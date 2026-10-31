#pragma once

#include <cstddef>
#include <cstdint>
#include <mutex>
#include <optional>
#include <string_view>

inline constexpr std::uint8_t jumpOpcode = 0xE9;
inline constexpr std::uint8_t callOpcode = 0xE8;
inline constexpr std::size_t branchLength = 5;

// jmp qword ptr [rip], which reads its destination from the eight bytes after it.
inline constexpr std::uint8_t absoluteJump[] = { 0xFF, 0x25, 0x00, 0x00, 0x00, 0x00 };

// The instruction and its destination.
inline constexpr std::size_t absoluteJumpLength = sizeof(absoluteJump) + sizeof(std::uintptr_t);

enum class CodeWriteKind
{
	Write,
	// Executable memory allocated for a plugin's code, which the plugin may change and enter at any address.
	Allocation
};

// Acquired across a write's overlap check, the write, and its entry.
extern std::mutex g_codeWritesLock;

void SF_Code_ClearConflictOwner();

// Called with g_codeWritesLock held. SF_Hook_ConflictOwner then returns the overlapping write's owner, or an empty string if this returns false.
bool SF_Code_OverlapsWrite(std::uintptr_t begin, std::size_t length);

// Called with g_codeWritesLock held. SF_Hook_ConflictOwner then returns the owner of the write whose target lies in the range, or an empty string if this returns false.
bool SF_Code_ContainsWriteTarget(std::uintptr_t begin, std::size_t length);

// Called with g_codeWritesLock held. SF_Hook_ConflictOwner then returns the owner of the write whose guarded bytes contain the address, or an empty string if this returns false.
bool SF_Code_IsInsideWrite(std::uintptr_t address);

// Called with g_codeWritesLock held, once the write is done or the memory allocated.
void SF_Code_RecordWrite(std::uintptr_t begin, std::size_t length, const char* owner, std::string_view description, CodeWriteKind kind = CodeWriteKind::Write,
	std::uintptr_t target = 0, std::optional<std::size_t> movedLength = std::nullopt);
