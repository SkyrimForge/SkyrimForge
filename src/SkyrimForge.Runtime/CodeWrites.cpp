#include "Shared.h"
#include "CodeWrites.h"

#include <Windows.h>

#include <spdlog/spdlog.h>

#include <algorithm>
#include <atomic>
#include <cstring>
#include <format>
#include <span>
#include <string>
#include <utility>
#include <vector>

std::mutex g_codeWritesLock;

namespace
{
	// Entries are never removed or changed.
	struct CodeWrite
	{
		std::uintptr_t begin;
		std::size_t length;
		// The number of bytes from begin in which only the first is a valid jump target. An instruction hook's moved length, or length for any other write.
		std::size_t guardedLength;
		std::string owner;
		CodeWriteKind kind;
		std::uintptr_t target;
		std::vector<std::uint8_t> written;
		// An instruction hook's slot, set only where a later hook at its address can share it, and the bytes the hook moved, as they were before it was written.
		std::optional<std::uint32_t> slot;
		std::vector<std::uint8_t> movedBytes;
		const CodeWrite* previous;
	};

	// Each entry is complete before it is published here, so SF_Code_InteriorOwner reads the list without acquiring g_codeWritesLock.
	std::atomic<const CodeWrite*> g_lastCodeWrite{ nullptr };

	thread_local std::string t_conflictOwner;

	template <typename Predicate>
	const CodeWrite* FindWrite(Predicate predicate)
	{
		for (const CodeWrite* write = g_lastCodeWrite.load(std::memory_order_acquire); write != nullptr; write = write->previous)
		{
			if (predicate(*write))
			{
				return write;
			}
		}
		return nullptr;
	}

	// Called with g_codeWritesLock held.
	template <typename Predicate>
	bool FindConflict(Predicate predicate)
	{
		const CodeWrite* conflicting = FindWrite(predicate);
		t_conflictOwner = conflicting != nullptr ? conflicting->owner : std::string{};
		return conflicting != nullptr;
	}

	bool IsGuardedInterior(const CodeWrite& write, std::uintptr_t address)
	{
		return write.kind == CodeWriteKind::Write && write.begin < address && address < write.begin + write.guardedLength;
	}

	bool IsReadable(std::uintptr_t begin, std::size_t length)
	{
		for (std::uintptr_t address = begin; address < begin + length;)
		{
			MEMORY_BASIC_INFORMATION information{};
			if (VirtualQuery(reinterpret_cast<const void*>(address), &information, sizeof(information)) == 0 || information.State != MEM_COMMIT ||
				(information.Protect & (PAGE_NOACCESS | PAGE_EXECUTE | PAGE_GUARD)) != 0)
			{
				return false;
			}
			address = reinterpret_cast<std::uintptr_t>(information.BaseAddress) + information.RegionSize;
		}
		return true;
	}

	std::uintptr_t GetBranchDestination(std::uintptr_t address)
	{
		const auto* code = reinterpret_cast<const std::uint8_t*>(address);
		if (IsReadable(address, branchLength) && (code[0] == jumpOpcode || code[0] == callOpcode))
		{
			return address + branchLength + *reinterpret_cast<const std::int32_t*>(address + 1);
		}
		if (IsReadable(address, absoluteJumpLength) && std::memcmp(code, absoluteJump, sizeof(absoluteJump)) == 0)
		{
			return *reinterpret_cast<const std::uintptr_t*>(address + sizeof(absoluteJump));
		}
		return 0;
	}

	// Candidates are tried from the write's first byte, where another module's hook usually begins, so a displacement byte equal to E8 or E9 is not read as an opcode.
	std::string DescribeForeignBranch(std::uintptr_t begin, std::uintptr_t changed)
	{
		for (std::uintptr_t address = (std::max)(begin, changed - (branchLength - 1)); address <= changed; ++address)
		{
			std::uintptr_t destination = GetBranchDestination(address);
			if (destination == 0)
			{
				continue;
			}
			if (const std::uintptr_t next = GetBranchDestination(destination); SF_Code_ModuleBase(destination) == 0 && next != 0)
			{
				destination = next;
			}
			return SF_Code_ModuleBase(destination) != 0 ? std::format(" The changed bytes are a jump or call to {}.", SF_Faults_DescribeAddress(reinterpret_cast<const void*>(destination)))
				: std::format(" The changed bytes are a jump or call to 0x{:X}, which lies in no module.", destination);
		}
		return {};
	}

	std::string FormatBytes(std::span<const std::uint8_t> bytes)
	{
		std::string text;
		for (const std::uint8_t value : bytes)
		{
			text += std::format("{}{:02X}", text.empty() ? "" : " ", value);
		}
		return text;
	}
}

void SF_Code_ClearConflictOwner()
{
	t_conflictOwner.clear();
}

bool SF_Code_OverlapsWrite(std::uintptr_t begin, std::size_t length)
{
	return FindConflict([begin, length](const CodeWrite& write) { return write.begin < begin + length && begin < write.begin + write.length; });
}

bool SF_Code_OverlapsWriteNotAt(std::uintptr_t begin, std::size_t length)
{
	return FindConflict([begin, length](const CodeWrite& write) { return write.begin != begin && write.begin < begin + length && begin < write.begin + write.length; });
}

bool SF_Code_HasWriteAt(std::uintptr_t address)
{
	return FindConflict([address](const CodeWrite& write) { return write.kind == CodeWriteKind::Write && write.begin == address; });
}

std::optional<SharedHook> SF_Code_FindSharedHook(std::uintptr_t address)
{
	const CodeWrite* newest = FindWrite([address](const CodeWrite& write) { return write.kind == CodeWriteKind::Write && write.begin == address; });
	return newest != nullptr && newest->slot ? std::optional{ SharedHook{ *newest->slot, newest->movedBytes } } : std::nullopt;
}

bool SF_Code_ContainsWriteTarget(std::uintptr_t begin, std::size_t length)
{
	return FindConflict([begin, length](const CodeWrite& write) { return write.target != 0 && begin < write.target && write.target < begin + length; });
}

bool SF_Code_IsInsideWrite(std::uintptr_t address)
{
	return FindConflict([address](const CodeWrite& write) { return IsGuardedInterior(write, address); });
}

void SF_Code_RecordWrite(std::uintptr_t begin, std::size_t length, const char* owner, std::string_view description, CodeWriteKind kind,
	std::uintptr_t target, std::span<const std::uint8_t> movedBytes, std::optional<std::uint32_t> slot)
{
	// Never deleted, since SF_Code_InteriorOwner returns a pointer into its owner string.
	const auto* bytes = reinterpret_cast<const std::uint8_t*>(begin);
	std::vector<std::uint8_t> written = kind == CodeWriteKind::Write ? std::vector<std::uint8_t>{ bytes, bytes + length } : std::vector<std::uint8_t>{};
	const std::size_t guardedLength = movedBytes.empty() ? length : movedBytes.size();
	const auto* write = new CodeWrite{ begin, length, guardedLength, owner, kind, target, std::move(written), slot, { movedBytes.begin(), movedBytes.end() },
		g_lastCodeWrite.load(std::memory_order_relaxed) };
	g_lastCodeWrite.store(write, std::memory_order_release);
	spdlog::info("{} {} at {}.", owner, description, SF_Faults_DescribeAddress(reinterpret_cast<const void*>(begin)));
}

// Valid until this thread's next write.
extern "C" __declspec(dllexport) const char* SF_Hook_ConflictOwner()
{
	return t_conflictOwner.c_str();
}

// The string is valid until the process exits.
// Takes no lock, since an instruction handler calls it while another thread may have acquired g_codeWritesLock and SafetyHook may have suspended the calling thread.
extern "C" __declspec(dllexport) const char* SF_Code_InteriorOwner(std::uintptr_t address)
{
	const CodeWrite* write = FindWrite([address](const CodeWrite& entry) { return IsGuardedInterior(entry, address); });
	return write != nullptr ? write->owner.c_str() : nullptr;
}

// Takes no lock, since entries are complete before they are published and never change.
// A byte a later write covers is compared only against that write, since a hook chained to an earlier one overwrites the earlier one's bytes.
extern "C" __declspec(dllexport) std::uint32_t SF_Code_VerifyWrites()
{
	std::uint32_t writeCount = 0;
	std::uint32_t changedCount = 0;
	std::vector<std::pair<std::uintptr_t, std::uintptr_t>> laterWrites;
	for (const CodeWrite* write = g_lastCodeWrite.load(std::memory_order_acquire); write != nullptr; write = write->previous)
	{
		if (write->kind != CodeWriteKind::Write)
		{
			continue;
		}
		const auto* code = reinterpret_cast<const std::uint8_t*>(write->begin);
		bool compared = false;
		std::optional<std::size_t> changedIndex;
		for (std::size_t i = 0; i < write->length && !changedIndex; ++i)
		{
			const std::uintptr_t address = write->begin + i;
			if (std::ranges::any_of(laterWrites, [address](const auto& range) { return range.first <= address && address < range.second; }))
			{
				continue;
			}
			compared = true;
			if (code[i] != write->written[i])
			{
				changedIndex = i;
			}
		}
		laterWrites.emplace_back(write->begin, write->begin + write->length);
		if (!compared)
		{
			continue;
		}
		++writeCount;
		if (!changedIndex)
		{
			continue;
		}
		++changedCount;
		const std::uintptr_t changed = write->begin + *changedIndex;
		const std::span<const std::uint8_t> currentCode{ code, write->length };
		spdlog::warn("Another module changed the {} bytes at {} that {} patched or verified. They were {} after {}'s change and are now {}.{}", write->length,
			SF_Faults_DescribeAddress(reinterpret_cast<const void*>(write->begin)), write->owner, FormatBytes(write->written), write->owner, FormatBytes(currentCode),
			DescribeForeignBranch(write->begin, changed));
	}
	spdlog::info("Recorded writes compared with the code: {}. Changed by another module: {}.", writeCount, changedCount);
	return changedCount;
}
