#include "Shared.h"

#include <Windows.h>

#define ZYCORE_STATIC_BUILD
#define ZYDIS_STATIC_BUILD
#include <Zydis/Zydis.h>

// A leaf function that allocates no stack has no exception directory entry. A split function has an entry for each part.
extern "C" __declspec(dllexport) bool SF_Code_FunctionBounds(std::uintptr_t address, std::uintptr_t* begin, std::uintptr_t* end)
{
	DWORD64 imageBase = 0;
	const RUNTIME_FUNCTION* function = RtlLookupFunctionEntry(address, &imageBase, nullptr);
	if (function == nullptr)
	{
		return false;
	}
	*begin = static_cast<std::uintptr_t>(imageBase + function->BeginAddress);
	*end = static_cast<std::uintptr_t>(imageBase + function->EndAddress);
	return true;
}

// False if an instruction spans address or a byte sequence does not decode. Nothing at or past address is read.
extern "C" __declspec(dllexport) bool SF_Code_IsInstructionStart(std::uintptr_t begin, std::uintptr_t address)
{
	ZydisDecoder decoder{};
	if (begin > address || !ZYAN_SUCCESS(ZydisDecoderInit(&decoder, ZYDIS_MACHINE_MODE_LONG_64, ZYDIS_STACK_WIDTH_64)))
	{
		return false;
	}
	for (std::uintptr_t instructionAddress = begin; instructionAddress < address;)
	{
		ZydisDecodedInstruction instruction{};
		if (!ZYAN_SUCCESS(ZydisDecoderDecodeInstruction(&decoder, nullptr, reinterpret_cast<const void*>(instructionAddress), address - instructionAddress, &instruction)))
		{
			return false;
		}
		instructionAddress += instruction.length;
	}
	return true;
}

// A byte that does not decode is data inside the function part and is skipped.
extern "C" bool SF_Code_BranchesInto(std::uintptr_t begin, std::uintptr_t end)
{
	std::uintptr_t partBegin = 0;
	std::uintptr_t partEnd = 0;
	ZydisDecoder decoder{};
	if (!SF_Code_FunctionBounds(begin, &partBegin, &partEnd) || !ZYAN_SUCCESS(ZydisDecoderInit(&decoder, ZYDIS_MACHINE_MODE_LONG_64, ZYDIS_STACK_WIDTH_64)))
	{
		return false;
	}
	for (std::uintptr_t instructionAddress = partBegin; instructionAddress < partEnd;)
	{
		ZydisDecodedInstruction instruction{};
		if (!ZYAN_SUCCESS(ZydisDecoderDecodeInstruction(&decoder, nullptr, reinterpret_cast<const void*>(instructionAddress), partEnd - instructionAddress, &instruction)))
		{
			++instructionAddress;
			continue;
		}
		if ((instructionAddress < begin || instructionAddress >= end) && instruction.raw.imm[0].is_relative)
		{
			const std::uintptr_t target = instructionAddress + instruction.length + static_cast<std::uintptr_t>(instruction.raw.imm[0].value.s);
			if (target > begin && target < end)
			{
				return true;
			}
		}
		instructionAddress += instruction.length;
	}
	return false;
}

// ZYDIS_ATTRIB_IS_RELATIVE marks both RIP-relative memory operands and relative branches.
extern "C" bool SF_Code_HasRelativeOperand(std::uintptr_t begin, std::uintptr_t end)
{
	ZydisDecoder decoder{};
	if (!ZYAN_SUCCESS(ZydisDecoderInit(&decoder, ZYDIS_MACHINE_MODE_LONG_64, ZYDIS_STACK_WIDTH_64)))
	{
		return true;
	}
	for (std::uintptr_t instructionAddress = begin; instructionAddress < end;)
	{
		ZydisDecodedInstruction instruction{};
		if (!ZYAN_SUCCESS(ZydisDecoderDecodeInstruction(&decoder, nullptr, reinterpret_cast<const void*>(instructionAddress), end - instructionAddress, &instruction)) ||
			(instruction.attributes & ZYDIS_ATTRIB_IS_RELATIVE) != 0)
		{
			return true;
		}
		instructionAddress += instruction.length;
	}
	return false;
}

// A byte sequence that does not decode counts as such a call.
extern "C" bool SF_Code_CallBeforeEnd(std::uintptr_t begin, std::uintptr_t end)
{
	ZydisDecoder decoder{};
	if (!ZYAN_SUCCESS(ZydisDecoderInit(&decoder, ZYDIS_MACHINE_MODE_LONG_64, ZYDIS_STACK_WIDTH_64)))
	{
		return true;
	}
	for (std::uintptr_t instructionAddress = begin; instructionAddress < end;)
	{
		ZydisDecodedInstruction instruction{};
		if (!ZYAN_SUCCESS(ZydisDecoderDecodeInstruction(&decoder, nullptr, reinterpret_cast<const void*>(instructionAddress), end - instructionAddress, &instruction)))
		{
			return true;
		}
		instructionAddress += instruction.length;
		if (instruction.mnemonic == ZYDIS_MNEMONIC_CALL && instructionAddress < end)
		{
			return true;
		}
	}
	return false;
}

// A byte sequence that does not decode counts as such a branch.
extern "C" bool SF_Code_BranchesWithin(std::uintptr_t begin, std::uintptr_t end)
{
	ZydisDecoder decoder{};
	if (!ZYAN_SUCCESS(ZydisDecoderInit(&decoder, ZYDIS_MACHINE_MODE_LONG_64, ZYDIS_STACK_WIDTH_64)))
	{
		return true;
	}
	for (std::uintptr_t instructionAddress = begin; instructionAddress < end;)
	{
		ZydisDecodedInstruction instruction{};
		if (!ZYAN_SUCCESS(ZydisDecoderDecodeInstruction(&decoder, nullptr, reinterpret_cast<const void*>(instructionAddress), end - instructionAddress, &instruction)))
		{
			return true;
		}
		const std::uintptr_t nextAddress = instructionAddress + instruction.length;
		if (instruction.raw.imm[0].is_relative)
		{
			const std::uintptr_t target = nextAddress + static_cast<std::uintptr_t>(instruction.raw.imm[0].value.s);
			if (target >= begin && target < end)
			{
				return true;
			}
		}
		instructionAddress = nextAddress;
	}
	return false;
}
