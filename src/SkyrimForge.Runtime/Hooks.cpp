#include "Shared.h"
#include "CodeWrites.h"

#include <Windows.h>

#include <spdlog/spdlog.h>

#include <algorithm>
#include <cstring>
#include <mutex>

namespace
{
	bool IsGameCode(std::uintptr_t address)
	{
		const REL::Segment text = REL::Module::get().segment(REL::Segment::textx);
		return address >= text.address() && address < text.address() + text.size();
	}

	template <typename T>
	T SelectForRuntime(T se, T ae, T vr)
	{
		return REL::Module::IsAE() ? ae : (REL::Module::IsVR() ? vr : se);
	}

	// SKSE's trampoline stub is FF 25 with a zero displacement, then the destination.
	bool IsTrampolineStub(std::uintptr_t address)
	{
		constexpr DWORD readableExecutable = PAGE_EXECUTE_READ | PAGE_EXECUTE_READWRITE | PAGE_EXECUTE_WRITECOPY;
		MEMORY_BASIC_INFORMATION information{};
		return VirtualQuery(reinterpret_cast<const void*>(address), &information, sizeof(information)) != 0 && information.State == MEM_COMMIT &&
			(information.Protect & readableExecutable) != 0 && (information.Protect & PAGE_GUARD) == 0 &&
			address + absoluteJumpLength <= reinterpret_cast<std::uintptr_t>(information.BaseAddress) + information.RegionSize &&
			std::memcmp(reinterpret_cast<const void*>(address), absoluteJump, sizeof(absoluteJump)) == 0;
	}

	// A call another SKSE plugin replaced targets SKSE's stub, so a hook written over it calls that plugin's replacement.
	std::uintptr_t GetCallTarget(std::uintptr_t site)
	{
		if (site == 0 || !IsGameCode(site) || !IsGameCode(site + branchLength - 1) || *reinterpret_cast<const std::uint8_t*>(site) != callOpcode)
		{
			return 0;
		}
		const std::uintptr_t callTarget = (site + branchLength) + *reinterpret_cast<const std::int32_t*>(site + 1);
		return IsGameCode(callTarget) || IsTrampolineStub(callTarget) ? callTarget : 0;
	}

	// CommonLib's id2offset ends the process for an identifier the library does not contain.
	bool IsAddressLibraryIdentifier(std::uint64_t identifier)
	{
		return REL::IDDB::get().names(identifier);
	}

	// The decoded forms address memory through registers and constants only, so a detour can run them from a new address.
	std::size_t GetInstructionLength(std::uintptr_t address)
	{
		const std::uint8_t* code = reinterpret_cast<const std::uint8_t*>(address);
		std::size_t length = 0;

		// REX prefix.
		if (code[length] >= 0x40 && code[length] <= 0x4F)
		{
			++length;
		}

		const std::uint8_t opcode = code[length++];

		// push r64
		if (opcode >= 0x50 && opcode <= 0x57)
		{
			return length;
		}

		std::size_t immediateLength = 0;
		switch (opcode)
		{
		case 0x89:  // mov r/m64, r64
		case 0x8B:  // mov r64, r/m64
			break;
		case 0x83:  // group 1 (add, sub, and others), imm8
			immediateLength = 1;
			break;
		case 0x81:  // group 1, imm32
		case 0xC7:  // mov r/m64, imm32
			immediateLength = 4;
			break;
		default:
			return 0;
		}

		const std::uint8_t modRM = code[length++];
		const std::uint8_t mode = modRM >> 6;
		const std::uint8_t rm = modRM & 0x07;

		// C7 is a mov only if the middle field is zero. XBEGIN shares the opcode and counts an address from itself.
		if (opcode == 0xC7 && ((modRM >> 3) & 0x07) != 0x00)
		{
			return 0;
		}
		if (mode != 0x03)
		{
			// RIP-relative.
			if (mode == 0x00 && rm == 0x05)
			{
				return 0;
			}
			const bool hasSib = rm == 0x04;
			std::uint8_t sibBase = 0;
			if (hasSib)
			{
				sibBase = code[length++] & 0x07;
			}
			if (mode == 0x01)
			{
				length += 1;
			}
			else if (mode == 0x02 || (mode == 0x00 && hasSib && sibBase == 0x05))
			{
				length += 4;
			}
		}
		return length + immediateLength;
	}

	std::size_t GetPrologueLength(std::uintptr_t target)
	{
		std::size_t length = 0;
		while (length < branchLength)
		{
			const std::size_t instructionLength = GetInstructionLength(target + length);
			if (instructionLength == 0)
			{
				return 0;
			}
			length += instructionLength;
		}
		return length;
	}
}

extern "C" __declspec(dllexport) std::uintptr_t SF_Hook_FunctionAddress(std::uint64_t se, std::uint64_t ae, std::uint64_t vr)
{
	const std::uint64_t identifier = SelectForRuntime(se, ae, vr);
	if (!IsAddressLibraryIdentifier(identifier))
	{
		spdlog::error("The address library does not contain identifier {}.", identifier);
		return 0;
	}
	return REL::Relocation<std::uintptr_t>{ REL::RelocationID(se, ae, vr) }.address();
}

// CommonLib writes 0 if it states no identifier for this runtime.
extern "C" __declspec(dllexport) bool SF_AddressLibrary_Names(std::uint64_t se, std::uint64_t ae, std::uint64_t vr)
{
	const std::uint64_t identifier = SelectForRuntime(se, ae, vr);
	return identifier != 0 && IsAddressLibraryIdentifier(identifier);
}

extern "C" __declspec(dllexport) std::uintptr_t SF_Hook_VTable(const char* name)
{
	return name == nullptr ? 0 : SF_VTable_Lookup(name);
}

extern "C" __declspec(dllexport) std::uintptr_t SF_Hook_WriteVirtual(std::uintptr_t vtable, std::uint32_t index, std::uintptr_t replacement, const char* owner)
{
	SF_Code_ClearConflictOwner();
	if (vtable == 0 || replacement == 0)
	{
		return 0;
	}
	const std::uintptr_t slot = vtable + sizeof(void*) * index;
	std::scoped_lock lock{ g_codeWritesLock };
	const std::uintptr_t existing = *reinterpret_cast<std::uintptr_t*>(slot);
	if (SF_Code_OverlapsWrite(slot, sizeof(void*)) || !IsGameCode(existing))
	{
		return 0;
	}
	REL::safe_write(slot, replacement);
	SF_Code_RecordWrite(slot, sizeof(void*), owner, "replaced a virtual method table entry");
	return existing;
}

// SKSE's create() resets its cursor, so a second allocation abandons the first and the hooks relocated into it.
extern "C" void SF_Hook_AllocateTrampoline()
{
	SKSE::AllocTrampoline(8192);
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
		if (GetCallTarget(candidate) == callee && matchCount++ == occurrence)
		{
			return candidate;
		}
	}
	return 0;
}

// Finds a call another SKSE plugin replaced, which SF_Hook_FindCall does not find.
extern "C" __declspec(dllexport) std::uintptr_t SF_Hook_CallAt(std::uintptr_t function, std::uint32_t seOffset, std::uint32_t aeOffset, std::uint32_t vrOffset)
{
	if (function == 0)
	{
		return 0;
	}
	const std::uint32_t offset = SelectForRuntime(seOffset, aeOffset, vrOffset);
	const std::uintptr_t site = function + offset;
	return GetCallTarget(site) != 0 ? site : 0;
}

extern "C" __declspec(dllexport) std::uintptr_t SF_Hook_WriteCall(std::uintptr_t site, std::uintptr_t replacement, const char* owner)
{
	SF_Code_ClearConflictOwner();
	std::scoped_lock lock{ g_codeWritesLock };
	if (replacement == 0 || SF_Code_OverlapsWrite(site, branchLength) || GetCallTarget(site) == 0)
	{
		return 0;
	}
	const std::uintptr_t displaced = SKSE::GetTrampoline().write_call<branchLength>(site, replacement);
	SF_Code_RecordWrite(site, branchLength, owner, "replaced the call");
	return displaced;
}

extern "C" __declspec(dllexport) std::uint32_t SF_Hook_EntryPrologue(std::uintptr_t target, std::uint8_t* destination, std::uint32_t capacity)
{
	if (target == 0 || !IsGameCode(target))
	{
		return 0;
	}
	if (destination != nullptr && capacity != 0)
	{
		std::memcpy(destination, reinterpret_cast<const void*>(target), capacity);
	}
	return static_cast<std::uint32_t>(GetPrologueLength(target));
}

extern "C" __declspec(dllexport) std::uintptr_t SF_Hook_WriteEntry(std::uintptr_t target, std::uintptr_t replacement, const char* owner)
{
	SF_Code_ClearConflictOwner();
	if (target == 0 || replacement == 0 || !IsGameCode(target))
	{
		return 0;
	}
	std::scoped_lock lock{ g_codeWritesLock };
	// The overlap is tested over at least five bytes, so an earlier write's owner is reported even if its jump no longer decodes as a prologue.
	const std::size_t prologueLength = GetPrologueLength(target);
	const std::size_t replacedLength = (std::max)(prologueLength, branchLength);
	if (SF_Code_OverlapsWrite(target, replacedLength) || SF_Code_ContainsWriteTarget(target, replacedLength) || prologueLength == 0)
	{
		return 0;
	}

	SKSE::Trampoline& trampoline = SKSE::GetTrampoline();

	// SKSE ends the process on a request the trampoline cannot meet. The request is two jumps, this stub's and write_branch's, beyond the prologue.
	if (trampoline.free_size() < prologueLength + absoluteJumpLength * 2)
	{
		return 0;
	}
	std::uint8_t* stub = static_cast<std::uint8_t*>(trampoline.allocate(prologueLength + absoluteJumpLength));
	const std::uintptr_t resumeAddress = target + prologueLength;
	std::memcpy(stub, reinterpret_cast<const void*>(target), prologueLength);
	std::memcpy(stub + prologueLength, absoluteJump, sizeof(absoluteJump));
	std::memcpy(stub + prologueLength + sizeof(absoluteJump), &resumeAddress, sizeof(resumeAddress));

	// Written once the stub is complete, so a thread arriving between the two steps runs the original code.
	trampoline.write_branch<branchLength>(target, replacement);
	SF_Code_RecordWrite(target, prologueLength, owner, "detoured the function");
	return reinterpret_cast<std::uintptr_t>(stub);
}
