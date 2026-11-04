#include "Shared.h"
#include "CodeWrites.h"

#include <Windows.h>

#define ZYCORE_STATIC_BUILD
#define ZYDIS_STATIC_BUILD
#include <safetyhook.hpp>

#include <spdlog/spdlog.h>

#include <algorithm>
#include <cstring>
#include <mutex>
#include <span>

namespace
{
	// The reason this thread's last hook was not written. SF_Hook_Fault returns it.
	thread_local PatchFault t_hookFault = PatchFault::None;

	std::uintptr_t Fail(PatchFault fault)
	{
		t_hookFault = fault;
		return 0;
	}

	// An absolute jump stub, as SKSE's trampoline and SF_Code_AllocateJump write one: FF 25 with a zero displacement, then the destination.
	bool IsTrampolineStub(std::uintptr_t address)
	{
		constexpr DWORD readableExecutable = PAGE_EXECUTE_READ | PAGE_EXECUTE_READWRITE | PAGE_EXECUTE_WRITECOPY;
		MEMORY_BASIC_INFORMATION information{};
		return VirtualQuery(reinterpret_cast<const void*>(address), &information, sizeof(information)) != 0 && information.State == MEM_COMMIT &&
			(information.Protect & readableExecutable) != 0 && (information.Protect & PAGE_GUARD) == 0 &&
			address + absoluteJumpLength <= reinterpret_cast<std::uintptr_t>(information.BaseAddress) + information.RegionSize &&
			std::memcmp(reinterpret_cast<const void*>(address), absoluteJump, sizeof(absoluteJump)) == 0;
	}

	// The target must lie in the site's own section or be an absolute jump stub, so a data byte equal to E8 at a wrong offset is rarely accepted as a call.
	// A call that a recorded hook or patch wrote at the site is accepted with any target.
	std::uintptr_t GetCallTarget(std::uintptr_t site, bool recorded)
	{
		std::uintptr_t sectionBegin = 0;
		std::uintptr_t sectionEnd = 0;
		if (!SF_Code_FindCodeSection(site, &sectionBegin, &sectionEnd) || site + branchLength > sectionEnd ||
			*reinterpret_cast<const std::uint8_t*>(site) != callOpcode)
		{
			return 0;
		}
		const std::uintptr_t callTarget = (site + branchLength) + *reinterpret_cast<const std::int32_t*>(site + 1);
		return recorded || (callTarget >= sectionBegin && callTarget < sectionEnd) || IsTrampolineStub(callTarget) ? callTarget : 0;
	}

	// An unconditional jump with a 32-bit displacement, or one through an address, which SafetyHook moves whole.
	bool IsJump(std::uintptr_t address)
	{
		const auto* code = reinterpret_cast<const std::uint8_t*>(address);
		return code[0] == jumpOpcode || (code[0] == absoluteJump[0] && code[1] == absoluteJump[1]);
	}

	// CommonLib's id2offset ends the process for an identifier the library does not contain.
	bool IsAddressLibraryIdentifier(std::uint64_t identifier)
	{
		return REL::IDDB::get().names(identifier);
	}
}

extern "C" __declspec(dllexport) std::uintptr_t SF_Hook_FunctionAddress(std::uint64_t se, std::uint64_t ae, std::uint64_t vr)
{
	const std::uint64_t identifier = REL::Relocate(se, ae, vr);
	if (!IsAddressLibraryIdentifier(identifier))
	{
		spdlog::error("The address library does not contain identifier {}.", identifier);
		return 0;
	}
	return REL::ID(identifier).address();
}

// CommonLib writes 0 if it states no identifier for this runtime.
extern "C" __declspec(dllexport) bool SF_AddressLibrary_Names(std::uint64_t se, std::uint64_t ae, std::uint64_t vr)
{
	const std::uint64_t identifier = REL::Relocate(se, ae, vr);
	return identifier != 0 && IsAddressLibraryIdentifier(identifier);
}

extern "C" __declspec(dllexport) std::uintptr_t SF_Hook_VTable(const char* name)
{
	return SF_VTable_Lookup(name);
}

// Valid until this thread's next hook.
extern "C" __declspec(dllexport) PatchFault SF_Hook_Fault()
{
	return t_hookFault;
}

// An entry an earlier hook replaced is replaced again, and the earlier replacement is returned.
// Otherwise the entry must address code, which an index past the table's end rarely does.
extern "C" __declspec(dllexport) std::uintptr_t SF_Hook_WriteVirtual(std::uintptr_t vtable, std::uint32_t index, std::uintptr_t replacement, const char* owner)
{
	SF_Code_ClearConflictOwner();
	t_hookFault = PatchFault::None;
	const std::uintptr_t slot = vtable + sizeof(void*) * index;
	std::scoped_lock lock{ g_codeWritesLock };
	if (SF_Code_OverlapsWriteNotAt(slot, sizeof(void*)))
	{
		return Fail(PatchFault::Overlap);
	}
	const bool replaced = SF_Code_HasWriteAt(slot);
	SF_Code_ClearConflictOwner();
	const std::uintptr_t existing = *reinterpret_cast<std::uintptr_t*>(slot);
	std::uintptr_t sectionBegin = 0;
	std::uintptr_t sectionEnd = 0;
	if (!replaced && !SF_Code_FindCodeSection(existing, &sectionBegin, &sectionEnd))
	{
		return Fail(PatchFault::NotCode);
	}
	REL::safe_write(slot, replacement);
	SF_Code_RecordWrite(slot, sizeof(void*), owner, "replaced a virtual method table entry");
	return existing;
}

// A data byte equal to E8 is decoded as a call too, and is rejected unless its computed target is the callee.
extern "C" __declspec(dllexport) std::uintptr_t SF_Hook_FindCall(std::uintptr_t caller, std::uintptr_t callee, std::uint32_t occurrence, std::uint32_t searchLength)
{
	if (caller == 0 || callee == 0)
	{
		return 0;
	}
	std::uint32_t matchCount = 0;
	for (std::size_t offset = 0; offset < searchLength; ++offset)
	{
		const std::uintptr_t candidate = caller + offset;
		if (GetCallTarget(candidate, false) == callee && matchCount++ == occurrence)
		{
			return candidate;
		}
	}
	return 0;
}

// Finds a call another SKSE plugin replaced, which SF_Hook_FindCall does not find.
extern "C" __declspec(dllexport) std::uintptr_t SF_Hook_CallAt(std::uintptr_t function, std::uint32_t seOffset, std::uint32_t aeOffset, std::uint32_t vrOffset)
{
	const std::uint32_t offset = REL::Relocate(seOffset, aeOffset, vrOffset);
	const std::uintptr_t site = function + offset;
	return GetCallTarget(site, false) != 0 ? site : 0;
}

// A call an earlier hook replaced is replaced again, and the earlier hook's target is returned.
// The call reaches the replacement through an absolute jump stub, written whole before the call is changed.
extern "C" __declspec(dllexport) std::uintptr_t SF_Hook_WriteCall(std::uintptr_t site, std::uintptr_t replacement, const char* owner)
{
	SF_Code_ClearConflictOwner();
	t_hookFault = PatchFault::None;
	std::scoped_lock lock{ g_codeWritesLock };
	if (const PatchFault fault = SF_Code_CheckImage(site, branchLength); fault != PatchFault::None)
	{
		return Fail(fault);
	}
	if (SF_Code_OverlapsWriteNotAt(site, branchLength))
	{
		return Fail(PatchFault::Overlap);
	}
	// HasWriteAt names the recorded write's owner, which the Overlap below reports.
	const bool replaced = SF_Code_HasWriteAt(site);
	const std::uintptr_t displaced = GetCallTarget(site, replaced);
	if (displaced == 0)
	{
		return Fail(replaced ? PatchFault::Overlap : PatchFault::NotCall);
	}
	SF_Code_ClearConflictOwner();
	const std::uintptr_t stub = SF_Code_AllocateJump(site, replacement);
	if (stub == 0)
	{
		return Fail(PatchFault::NoNearMemory);
	}
	const std::vector<std::uint8_t> call = SF_Code_MakeBranch(callOpcode, site, stub, branchLength);
	REL::safe_write(site, call.data(), call.size());
	FlushInstructionCache(GetCurrentProcess(), reinterpret_cast<void*>(site), call.size());
	SF_Code_RecordWrite(site, branchLength, owner, "replaced the call");
	return displaced;
}

// SafetyHook moves the instructions its jump overwrites to the trampoline returned. A jump an earlier hook or another module wrote at the entry is moved the same way,
// so the trampoline jumps to that hook. A recorded write at the entry is accepted only if it begins with such a jump, since SafetyHook then moves the jump alone.
extern "C" __declspec(dllexport) std::uintptr_t SF_Hook_WriteEntry(std::uintptr_t target, std::uintptr_t replacement, const char* owner)
{
	SF_Code_ClearConflictOwner();
	t_hookFault = PatchFault::None;
	std::scoped_lock lock{ g_codeWritesLock };
	// Tested over the jump's five bytes before SafetyHook decodes them, so an earlier write's owner is reported rather than a decoding failure.
	if (const PatchFault fault = SF_Code_CheckImage(target, branchLength); fault != PatchFault::None)
	{
		return Fail(fault);
	}
	if (SF_Code_OverlapsWriteNotAt(target, branchLength) || (SF_Code_HasWriteAt(target) && !IsJump(target)))
	{
		return Fail(PatchFault::Overlap);
	}
	SF_Code_ClearConflictOwner();

	auto created = safetyhook::InlineHook::create(reinterpret_cast<void*>(target), reinterpret_cast<void*>(replacement), safetyhook::InlineHook::StartDisabled);
	if (!created)
	{
		// SafetyHook writes a fourteen-byte jump when no trampoline lies within reach, which cannot move an instruction relative to its own address.
		const auto errorType = created.error().type;
		const bool allocationFailed = errorType == safetyhook::InlineHook::Error::BAD_ALLOCATION || errorType == safetyhook::InlineHook::Error::IP_RELATIVE_INSTRUCTION_OUT_OF_RANGE;
		return Fail(allocationFailed ? PatchFault::NoNearMemory : PatchFault::NotRelocatable);
	}
	const std::span<const std::uint8_t> moved = created->original_bytes();
	if (const PatchFault fault = SF_Code_CheckImage(target, moved.size()); fault != PatchFault::None)
	{
		return Fail(fault);
	}
	if (SF_Code_OverlapsWriteNotAt(target, moved.size()))
	{
		return Fail(PatchFault::Overlap);
	}
	if (const PatchFault fault = SF_Code_CheckMoved(target, moved.size()); fault != PatchFault::None)
	{
		return Fail(fault);
	}
	// SafetyHook can report a skipped write as done, so the entry is compared with the moved bytes.
	if (!created->enable() || std::ranges::equal(moved, std::span{ reinterpret_cast<const std::uint8_t*>(target), moved.size() }))
	{
		return Fail(PatchFault::HookNotWritten);
	}
	SF_Code_RecordWrite(target, moved.size(), owner, "detoured the function");
	const std::uintptr_t trampoline = created->trampoline().address();
	// Never destroyed, since destroying it writes the original bytes back while game threads may run them.
	new safetyhook::InlineHook{ std::move(*created) };
	return trampoline;
}
