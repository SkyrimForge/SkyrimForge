#include "Shared.h"
#include "CodeWrites.h"

#include <Windows.h>

#define ZYCORE_STATIC_BUILD
#define ZYDIS_STATIC_BUILD
#include <safetyhook.hpp>

#include <algorithm>
#include <array>
#include <atomic>
#include <cstring>
#include <expected>
#include <format>
#include <memory>
#include <mutex>
#include <optional>
#include <span>
#include <string>
#include <string_view>
#include <utility>
#include <vector>

extern "C" IMAGE_DOS_HEADER __ImageBase;

namespace
{
	// CodePatcher mirrors the values.
	enum class PatchFault : std::int32_t
	{
		None,
		NoImage,
		ExcludedImage,
		NotExecutable,
		Overlap,
		NotInstructionStart,
		Mismatch,
		ProtectionUnchanged,
		NotInstructionEnd,
		BranchIntoPatch,
		DisplacedRelative,
		TargetNotInstructionStart,
		TargetInsideWrite,
		NoNearMemory,
		ContainsTarget,
		NotRelocatable,
		MovedPastExpected,
		MovedCallNotLast,
		BranchWithinMoved,
		HookNotWritten,
		HookNotRemoved
	};

	// CodePatcher mirrors the values.
	enum class PatchKind : std::int32_t
	{
		Bytes,
		Jump,
		Call,
		Insertion,
		Instruction
	};

	// The layout matches CodePatcher's declaration.
	struct CodePatch
	{
		PatchKind kind;
		std::uintptr_t address;
		const std::uint8_t* expected;
		const std::uint8_t* mask;
		std::uint32_t expectedLength;
		const std::uint8_t* replacement;
		std::uint32_t offset;
		std::uint32_t replacementLength;
		std::uintptr_t target;
		const std::uint8_t* code;
		std::uint32_t codeLength;
		std::uint32_t displacedLength;
		std::int32_t displacedAfter;
		// Below instructionHookCapacity, and used by no other hook.
		std::uint32_t slot;
	};

	using InstructionHandler = void (*)(std::uint32_t slot, safetyhook::Context* context);

	constexpr std::size_t instructionHookCapacity = 1024;

	std::atomic<InstructionHandler> g_instructionHandler{ nullptr };

	// SafetyHook passes a destination only the register context, so each slot has its own destination, which passes its slot to the one handler.
	template <std::size_t Slot>
	void InstructionDestination(safetyhook::Context& context)
	{
		g_instructionHandler.load()(static_cast<std::uint32_t>(Slot), &context);
	}

	template <std::size_t... Slots>
	constexpr std::array<safetyhook::MidHookFn, sizeof...(Slots)> MakeInstructionDestinations(std::index_sequence<Slots...>)
	{
		return { &InstructionDestination<Slots>... };
	}

	constexpr std::array<safetyhook::MidHookFn, instructionHookCapacity> instructionDestinations = MakeInstructionDestinations(std::make_index_sequence<instructionHookCapacity>{});

	constexpr std::uint8_t nopOpcode = 0x90;

	// call qword ptr [rip + displacement]
	constexpr std::uint8_t indirectCall[] = { 0xFF, 0x15 };
	constexpr std::size_t indirectCallLength = sizeof(indirectCall) + sizeof(std::int32_t);

	bool IsLoneCall(const CodePatch& patch)
	{
		return patch.displacedLength == branchLength && *reinterpret_cast<const std::uint8_t*>(patch.address) == callOpcode;
	}

	// Never destroyed, since destroying it releases memory code may still run from.
	safetyhook::Allocator& GetCodeAllocator()
	{
		static auto* const allocator = new std::shared_ptr<safetyhook::Allocator>{ safetyhook::Allocator::create() };
		return **allocator;
	}

	// SafetyHook bounds the distance of an allocation's first byte only, so reach covers the further bytes a branch to or from the address spans.
	std::expected<safetyhook::Allocation, safetyhook::Allocator::Error> AllocateNear(std::uintptr_t address, std::size_t length, std::size_t reach)
	{
		constexpr std::size_t maximumDistance = 0x7FFF'FFFF;
		if (reach >= maximumDistance)
		{
			return std::unexpected{ safetyhook::Allocator::Error::NO_MEMORY_IN_RANGE };
		}
		return GetCodeAllocator().allocate_near({ reinterpret_cast<std::uint8_t*>(address) }, length, maximumDistance - reach);
	}

	// Callers confirm the displacement fits.
	std::vector<std::uint8_t> MakeBranch(std::uint8_t opcode, std::uintptr_t site, std::uintptr_t target, std::size_t length)
	{
		std::vector<std::uint8_t> bytes(length, nopOpcode);
		bytes[0] = opcode;
		const auto displacement = static_cast<std::int32_t>(target - (site + branchLength));
		std::memcpy(bytes.data() + 1, &displacement, sizeof(displacement));
		return bytes;
	}

	std::vector<std::uint8_t> MakeCave(const CodePatch& patch, std::uintptr_t cave)
	{
		const bool loneCall = IsLoneCall(patch);
		const std::size_t displacedLength = loneCall ? indirectCallLength : patch.displacedLength;
		const std::size_t displacedAt = patch.displacedAfter != 0 ? patch.codeLength : 0;
		const std::size_t codeAt = patch.displacedAfter != 0 ? 0 : displacedLength;
		const std::size_t returnAt = patch.codeLength + displacedLength;
		std::vector<std::uint8_t> bytes(returnAt + branchLength + (loneCall ? sizeof(std::uintptr_t) : 0));
		std::memcpy(bytes.data() + codeAt, patch.code, patch.codeLength);
		if (loneCall)
		{
			const std::size_t slotAt = returnAt + branchLength;
			const std::uintptr_t destination = patch.address + branchLength + *reinterpret_cast<const std::int32_t*>(patch.address + 1);
			const auto slotDisplacement = static_cast<std::int32_t>(slotAt - (displacedAt + indirectCallLength));
			std::memcpy(bytes.data() + displacedAt, indirectCall, sizeof(indirectCall));
			std::memcpy(bytes.data() + displacedAt + sizeof(indirectCall), &slotDisplacement, sizeof(slotDisplacement));
			std::memcpy(bytes.data() + slotAt, &destination, sizeof(destination));
		}
		else
		{
			std::memcpy(bytes.data() + displacedAt, reinterpret_cast<const void*>(patch.address), patch.displacedLength);
		}
		const std::vector<std::uint8_t> jump = MakeBranch(jumpOpcode, cave + returnAt, patch.address + patch.expectedLength, branchLength);
		std::ranges::copy(jump, bytes.begin() + static_cast<std::ptrdiff_t>(returnAt));
		return bytes;
	}

	std::size_t GetCaveLength(const CodePatch& patch)
	{
		return IsLoneCall(patch) ? patch.codeLength + indirectCallLength + branchLength + sizeof(std::uintptr_t) : patch.codeLength + patch.displacedLength + branchLength;
	}

	std::vector<std::uint8_t> MakeSiteBytes(const CodePatch& patch, std::uintptr_t cave)
	{
		switch (patch.kind)
		{
		case PatchKind::Bytes:
			return { patch.replacement, patch.replacement + patch.replacementLength };
		case PatchKind::Call:
			return MakeBranch(callOpcode, patch.address, patch.target, patch.expectedLength);
		case PatchKind::Jump:
			return MakeBranch(jumpOpcode, patch.address, patch.target, patch.expectedLength);
		case PatchKind::Insertion:
			return MakeBranch(jumpOpcode, patch.address, cave, patch.expectedLength);
		default:
			// SafetyHook writes an instruction hook.
			return {};
		}
	}

	std::string DescribePatch(const CodePatch& patch, std::size_t movedLength)
	{
		switch (patch.kind)
		{
		case PatchKind::Bytes:
			return std::format("patched {} of the {} bytes it verified", patch.replacementLength, patch.expectedLength);
		case PatchKind::Call:
			return std::format("replaced {} bytes with a call to {}", patch.expectedLength, SF_Faults_DescribeAddress(reinterpret_cast<const void*>(patch.target)));
		case PatchKind::Jump:
			return std::format("replaced {} bytes with a jump to {}", patch.expectedLength, SF_Faults_DescribeAddress(reinterpret_cast<const void*>(patch.target)));
		case PatchKind::Insertion:
			return std::format("inserted {} bytes of code {} the {} bytes it displaced, replacing {} bytes", patch.codeLength, patch.displacedAfter != 0 ? "before" : "after",
				patch.displacedLength, patch.expectedLength);
		default:
			return std::format("hooked the instruction in slot {}, moving {} of the {} bytes it verified", patch.slot, movedLength, patch.expectedLength);
		}
	}

	// Empty for a null module, for which GetModuleFileNameW returns the executable's path.
	std::wstring GetModuleDirectory(HMODULE module)
	{
		wchar_t path[MAX_PATH]{};
		const std::wstring_view loadedPath{ path, module != nullptr ? GetModuleFileNameW(module, path, MAX_PATH) : 0 };
		return std::wstring{ loadedPath.substr(0, loadedPath.rfind(L'\\') + 1) };
	}

	bool IsUnder(std::wstring_view path, std::wstring_view directory)
	{
		return !directory.empty() && path.size() > directory.size() &&
			CompareStringOrdinal(path.data(), static_cast<int>(directory.size()), directory.data(), static_cast<int>(directory.size()), TRUE) == CSTR_EQUAL;
	}

	bool IsExcludedImage(HMODULE module, const IMAGE_NT_HEADERS64* headers)
	{
		if (module == reinterpret_cast<HMODULE>(&__ImageBase) || headers->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR].VirtualAddress != 0)
		{
			return true;
		}
		wchar_t path[MAX_PATH]{};
		const std::wstring_view modulePath{ path, GetModuleFileNameW(module, path, MAX_PATH) };
		wchar_t windowsDirectory[MAX_PATH]{};
		const UINT windowsDirectoryLength = GetSystemWindowsDirectoryW(windowsDirectory, MAX_PATH);
		// hostfxr.dll is in a different directory from coreclr.dll.
		return IsUnder(modulePath, std::wstring{ windowsDirectory, windowsDirectoryLength } + L'\\') ||
			IsUnder(modulePath, GetModuleDirectory(GetModuleHandleW(L"coreclr.dll"))) || IsUnder(modulePath, GetModuleDirectory(GetModuleHandleW(L"hostfxr.dll")));
	}

	PatchFault CheckImage(std::uintptr_t begin, std::size_t length)
	{
		HMODULE module = nullptr;
		if (!GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT, reinterpret_cast<LPCWSTR>(begin), &module))
		{
			return PatchFault::NoImage;
		}
		const auto imageBase = reinterpret_cast<std::uintptr_t>(module);
		const auto* headers = reinterpret_cast<const IMAGE_NT_HEADERS64*>(imageBase + reinterpret_cast<const IMAGE_DOS_HEADER*>(imageBase)->e_lfanew);
		if (IsExcludedImage(module, headers))
		{
			return PatchFault::ExcludedImage;
		}
		const std::uintptr_t rva = begin - imageBase;
		const std::span sections{ IMAGE_FIRST_SECTION(headers), headers->FileHeader.NumberOfSections };
		const bool executable = std::ranges::any_of(sections, [rva, length](const IMAGE_SECTION_HEADER& section) {
			return (section.Characteristics & IMAGE_SCN_MEM_EXECUTE) != 0 && rva >= section.VirtualAddress && rva + length <= section.VirtualAddress + section.Misc.VirtualSize;
		});
		return executable ? PatchFault::None : PatchFault::NotExecutable;
	}

	// Called with g_codeWritesLock held, once the expected bytes match. Execution resumes at the end of the expected bytes, so they must end on an instruction.
	PatchFault CheckRedirect(const CodePatch& patch)
	{
		const std::uintptr_t end = patch.address + patch.expectedLength;
		const bool displacedEndsInstruction = patch.kind != PatchKind::Insertion || SF_Code_IsInstructionStart(patch.address, patch.address + patch.displacedLength);
		if (!SF_Code_IsInstructionStart(patch.address, end) || !displacedEndsInstruction)
		{
			return PatchFault::NotInstructionEnd;
		}
		if (SF_Code_BranchesInto(patch.address, end))
		{
			return PatchFault::BranchIntoPatch;
		}
		if (patch.kind == PatchKind::Insertion)
		{
			// A moved relative operand would resolve to a different address. The lone call is rewritten as an absolute one.
			const bool displacedRelative = !IsLoneCall(patch) && SF_Code_HasRelativeOperand(patch.address, patch.address + patch.displacedLength);
			return displacedRelative ? PatchFault::DisplacedRelative : PatchFault::None;
		}
		// Tested first, so the conflicting write's owner is reported.
		if (SF_Code_IsInsideWrite(patch.target))
		{
			return PatchFault::TargetInsideWrite;
		}
		std::uintptr_t functionBegin = 0;
		std::uintptr_t functionEnd = 0;
		const bool targetBeginsInstruction = !SF_Code_FunctionBounds(patch.target, &functionBegin, &functionEnd) || SF_Code_IsInstructionStart(functionBegin, patch.target);
		return targetBeginsInstruction ? PatchFault::None : PatchFault::TargetNotInstructionStart;
	}

	// Called with g_codeWritesLock held. The overlap is tested before the bytes, so an earlier write's owner is reported instead of a mismatch.
	PatchFault CheckPatch(const CodePatch& patch)
	{
		if (const PatchFault fault = CheckImage(patch.address, patch.expectedLength); fault != PatchFault::None)
		{
			return fault;
		}
		if (SF_Code_OverlapsWrite(patch.address, patch.expectedLength))
		{
			return PatchFault::Overlap;
		}
		// PrepareInstructionHook tests an instruction hook's moved bytes.
		if (patch.kind != PatchKind::Instruction && SF_Code_ContainsWriteTarget(patch.address, patch.expectedLength))
		{
			return PatchFault::ContainsTarget;
		}
		// A function the exception directory does not list is confirmed by its expected bytes alone.
		std::uintptr_t functionBegin = 0;
		std::uintptr_t functionEnd = 0;
		if (SF_Code_FunctionBounds(patch.address, &functionBegin, &functionEnd) && !SF_Code_IsInstructionStart(functionBegin, patch.address))
		{
			return PatchFault::NotInstructionStart;
		}
		const auto* code = reinterpret_cast<const std::uint8_t*>(patch.address);
		for (std::uint32_t i = 0; i < patch.expectedLength; ++i)
		{
			if (patch.mask[i] != 0 && code[i] != patch.expected[i])
			{
				return PatchFault::Mismatch;
			}
		}
		return patch.kind == PatchKind::Bytes || patch.kind == PatchKind::Instruction ? PatchFault::None : CheckRedirect(patch);
	}

	// Called with g_codeWritesLock held, once CheckPatch has passed.
	// The moved bytes must lie within the expected ones, since SafetyHook writes a fourteen-byte jump when no trampoline lies within reach.
	// A moved call must be the last moved instruction, so a thread inside the callee during the write returns past the jump.
	// A moved branch must not target a moved byte, since SafetyHook aims it at the original address, where the hook's jump is written.
	PatchFault PrepareInstructionHook(const CodePatch& patch, std::optional<safetyhook::MidHook>& hook)
	{
		auto created = safetyhook::MidHook::create(reinterpret_cast<void*>(patch.address), instructionDestinations[patch.slot], safetyhook::MidHook::StartDisabled);
		if (!created)
		{
			const safetyhook::MidHook::Error& error = created.error();
			const bool allocationFailed = error.type != safetyhook::MidHook::Error::BAD_INLINE_HOOK ||
				error.inline_hook_error.type == safetyhook::InlineHook::Error::BAD_ALLOCATION;
			return allocationFailed ? PatchFault::NoNearMemory : PatchFault::NotRelocatable;
		}
		const std::size_t movedLength = created->original_bytes().size();
		const std::uintptr_t movedEnd = patch.address + movedLength;
		PatchFault fault = PatchFault::None;
		if (movedLength > patch.expectedLength)
		{
			fault = PatchFault::MovedPastExpected;
		}
		else if (SF_Code_ContainsWriteTarget(patch.address, movedLength))
		{
			fault = PatchFault::ContainsTarget;
		}
		else if (SF_Code_CallBeforeEnd(patch.address, movedEnd))
		{
			fault = PatchFault::MovedCallNotLast;
		}
		else if (SF_Code_BranchesWithin(patch.address, movedEnd))
		{
			fault = PatchFault::BranchWithinMoved;
		}
		else if (SF_Code_BranchesInto(patch.address, movedEnd))
		{
			fault = PatchFault::BranchIntoPatch;
		}
		if (fault == PatchFault::None)
		{
			hook = std::move(*created);
		}
		return fault;
	}

	// Each hook is leaked, since a thread that entered it before it was disabled may still be running in its trampoline.
	bool DisableInstructionHooks(std::span<const CodePatch> patches, std::span<std::optional<safetyhook::MidHook>> hooks, const char* owner)
	{
		bool disabled = true;
		for (std::size_t i = 0; i < hooks.size(); ++i)
		{
			std::optional<safetyhook::MidHook>& hook = hooks[i];
			if (hook && hook->enabled())
			{
				if (!hook->disable())
				{
					disabled = false;
					SF_Code_RecordWrite(patches[i].address, patches[i].expectedLength, owner,
						"hooked an instruction, and the hook could not be removed after a later hook in its group failed",
						CodeWriteKind::Write, 0, hook->original_bytes().size());
				}
				new safetyhook::MidHook{ std::move(*hook) };
			}
		}
		return disabled;
	}

	constexpr std::uintptr_t pageSize = 0x1000;

	void RestoreProtections(std::span<const std::uintptr_t> pages, std::span<const DWORD> protections)
	{
		for (std::size_t i = 0; i < pages.size(); ++i)
		{
			DWORD replacedProtection = 0;
			VirtualProtect(reinterpret_cast<void*>(pages[i]), pageSize, protections[i], &replacedProtection);
		}
	}
}

extern "C" __declspec(dllexport) std::uintptr_t SF_Code_Allocate(std::uintptr_t address, std::uint32_t length, const char* owner)
{
	SF_Code_ClearConflictOwner();
	std::scoped_lock lock{ g_codeWritesLock };
	auto allocation = AllocateNear(address, length, std::size_t{ length } + branchLength);
	if (!allocation)
	{
		return 0;
	}
	const std::uintptr_t begin = allocation->address();
	SF_Code_RecordWrite(begin, length, owner, std::format("allocated {} bytes of executable memory", length), CodeWriteKind::Allocation);
	// Never destroyed, since destroying it releases its memory.
	new safetyhook::Allocation{ std::move(*allocation) };
	return begin;
}

// Called before the first instruction hook is written.
extern "C" __declspec(dllexport) void SF_Hook_SetInstructionHandler(InstructionHandler handler)
{
	g_instructionHandler.store(handler);
}

// Writes every patch or none. The exception is HookNotRemoved, where an enabled hook could not be disabled after a later hook failed, and remains written and recorded.
extern "C" __declspec(dllexport) PatchFault SF_Code_WritePatches(const CodePatch* patches, std::uint32_t count, const char* owner, std::uint32_t* failedIndex)
{
	SF_Code_ClearConflictOwner();
	const std::span patchSpan{ patches, count };
	std::scoped_lock lock{ g_codeWritesLock };
	std::vector<std::optional<safetyhook::MidHook>> hooks(count);
	for (std::uint32_t i = 0; i < count; ++i)
	{
		PatchFault fault = CheckPatch(patchSpan[i]);
		if (fault == PatchFault::None && patchSpan[i].kind == PatchKind::Instruction)
		{
			fault = PrepareInstructionHook(patchSpan[i], hooks[i]);
		}
		if (fault != PatchFault::None)
		{
			*failedIndex = i;
			return fault;
		}
	}

	// The reach covers the site's jump to the cave and the cave's jump back to the end of the expected bytes.
	std::vector<safetyhook::Allocation> caves(count);
	for (std::uint32_t i = 0; i < count; ++i)
	{
		const CodePatch& patch = patchSpan[i];
		if (patch.kind != PatchKind::Insertion)
		{
			continue;
		}
		const std::size_t caveLength = GetCaveLength(patch);
		auto cave = AllocateNear(patch.address, caveLength, caveLength + patch.expectedLength + branchLength);
		if (!cave)
		{
			*failedIndex = i;
			return PatchFault::NoNearMemory;
		}
		caves[i] = std::move(*cave);
	}

	std::vector<std::uintptr_t> writeBegins;
	std::vector<std::vector<std::uint8_t>> writes;
	for (std::uint32_t i = 0; i < count; ++i)
	{
		writeBegins.push_back(patchSpan[i].address + (patchSpan[i].kind == PatchKind::Bytes ? patchSpan[i].offset : 0));
		writes.push_back(MakeSiteBytes(patchSpan[i], caves[i].address()));
	}

	// Each page is changed separately, since VirtualProtect returns the protection of a range's first page only.
	std::vector<std::uintptr_t> pages;
	std::vector<DWORD> protections;
	for (std::uint32_t i = 0; i < count; ++i)
	{
		const std::uintptr_t begin = writeBegins[i];
		for (std::uintptr_t page = begin & ~(pageSize - 1); !writes[i].empty() && page < begin + writes[i].size(); page += pageSize)
		{
			if (std::ranges::contains(pages, page))
			{
				continue;
			}
			DWORD protection = 0;
			if (!VirtualProtect(reinterpret_cast<void*>(page), pageSize, PAGE_EXECUTE_READWRITE, &protection))
			{
				RestoreProtections(pages, protections);
				*failedIndex = i;
				return PatchFault::ProtectionUnchanged;
			}
			pages.push_back(page);
			protections.push_back(protection);
		}
	}
	// The hooks are written first, since only a hook's write can still fail. SafetyHook can report a skipped write as done, so each site is compared with the moved bytes.
	for (std::uint32_t i = 0; i < count; ++i)
	{
		std::optional<safetyhook::MidHook>& hook = hooks[i];
		if (!hook)
		{
			continue;
		}
		const std::span<const std::uint8_t> moved = hook->original_bytes();
		if (!hook->enable() || std::ranges::equal(moved, std::span{ reinterpret_cast<const std::uint8_t*>(patchSpan[i].address), moved.size() }))
		{
			const bool disabled = DisableInstructionHooks(patchSpan.first(i + 1), std::span{ hooks }.first(i + 1), owner);
			RestoreProtections(pages, protections);
			*failedIndex = i;
			return disabled ? PatchFault::HookNotWritten : PatchFault::HookNotRemoved;
		}
	}
	// Each cave is written and flushed before any site jumps to it.
	for (std::uint32_t i = 0; i < count; ++i)
	{
		if (caves[i])
		{
			const std::vector<std::uint8_t> caveBytes = MakeCave(patchSpan[i], caves[i].address());
			std::ranges::copy(caveBytes, caves[i].data());
			FlushInstructionCache(GetCurrentProcess(), caves[i].data(), caveBytes.size());
		}
	}
	for (std::uint32_t i = 0; i < count; ++i)
	{
		std::ranges::copy(writes[i], reinterpret_cast<std::uint8_t*>(writeBegins[i]));
		FlushInstructionCache(GetCurrentProcess(), reinterpret_cast<void*>(writeBegins[i]), writes[i].size());
	}
	RestoreProtections(pages, protections);
	for (std::uint32_t i = 0; i < count; ++i)
	{
		const bool redirects = patchSpan[i].kind == PatchKind::Jump || patchSpan[i].kind == PatchKind::Call;
		const std::optional<std::size_t> movedLength = hooks[i] ? std::optional{ hooks[i]->original_bytes().size() } : std::nullopt;
		SF_Code_RecordWrite(patchSpan[i].address, patchSpan[i].expectedLength, owner, DescribePatch(patchSpan[i], movedLength.value_or(0)), CodeWriteKind::Write,
			redirects ? patchSpan[i].target : 0, movedLength);
		if (hooks[i])
		{
			// Never destroyed, since destroying it writes the original bytes back while game threads may run them.
			new safetyhook::MidHook{ std::move(*hooks[i]) };
		}
		if (caves[i])
		{
			SF_Code_RecordWrite(caves[i].address(), caves[i].size(), owner, "allocated the code it inserted", CodeWriteKind::Allocation);
			// Never destroyed, since destroying it releases its memory.
			new safetyhook::Allocation{ std::move(caves[i]) };
		}
	}
	return PatchFault::None;
}
