#pragma once

#include <cstddef>
#include <cstdint>
#include <mutex>
#include <optional>
#include <span>
#include <string_view>
#include <vector>

inline constexpr std::uint8_t jumpOpcode = 0xE9;
inline constexpr std::uint8_t callOpcode = 0xE8;
inline constexpr std::size_t branchLength = 5;

// jmp qword ptr [rip], which reads its destination from the eight bytes after it.
inline constexpr std::uint8_t absoluteJump[] = { 0xFF, 0x25, 0x00, 0x00, 0x00, 0x00 };

// The instruction and its destination.
inline constexpr std::size_t absoluteJumpLength = sizeof(absoluteJump) + sizeof(std::uintptr_t);

// PatchFault.cs mirrors the values.
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
	HookNotRemoved,
	// The address contains no call, or the call's target is neither code in the same section nor an absolute jump.
	NotCall,
	// A virtual method table entry addresses no executable section of a loaded module.
	NotCode
};

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

// Called with g_codeWritesLock held. Tests for a write that overlaps the range and does not begin at the range's first byte, since a hook can be chained to one that does.
// SF_Hook_ConflictOwner then returns the overlapping write's owner, or an empty string if this returns false.
bool SF_Code_OverlapsWriteNotAt(std::uintptr_t begin, std::size_t length);

// Called with g_codeWritesLock held. SF_Hook_ConflictOwner then returns the owner of the newest write beginning at the address, or an empty string if this returns false.
bool SF_Code_HasWriteAt(std::uintptr_t address);

// An instruction hook whose jump and slot a later hook at its address shares, and the bytes it moved, as they were before it was written.
struct SharedHook
{
	std::uint32_t slot;
	std::span<const std::uint8_t> movedBytes;
};

// Called with g_codeWritesLock held. Returns the newest write beginning at the address if it is an instruction hook a later hook there can share.
std::optional<SharedHook> SF_Code_FindSharedHook(std::uintptr_t address);

// Called with g_codeWritesLock held. SF_Hook_ConflictOwner then returns the owner of the write whose target lies in the range, or an empty string if this returns false.
bool SF_Code_ContainsWriteTarget(std::uintptr_t begin, std::size_t length);

// Called with g_codeWritesLock held. SF_Hook_ConflictOwner then returns the owner of the write whose guarded bytes contain the address, or an empty string if this returns false.
bool SF_Code_IsInsideWrite(std::uintptr_t address);

// Called with g_codeWritesLock held, once the write is done or the memory allocated.
// An instruction hook passes the bytes it moved, as they were before it was written, and its slot if a later hook at its address can share it.
void SF_Code_RecordWrite(std::uintptr_t begin, std::size_t length, const char* owner, std::string_view description, CodeWriteKind kind = CodeWriteKind::Write,
	std::uintptr_t target = 0, std::span<const std::uint8_t> movedBytes = {}, std::optional<std::uint32_t> slot = std::nullopt);

// Returns a jump or call at the site to the target, padded with NOPs to the length. Callers confirm the displacement fits.
std::vector<std::uint8_t> SF_Code_MakeBranch(std::uint8_t opcode, std::uintptr_t site, std::uintptr_t target, std::size_t length);

// Returns None if every byte of the range lies in one executable section of a loaded module SkyrimForge patches.
PatchFault SF_Code_CheckImage(std::uintptr_t begin, std::size_t length);

// Sets the bounds of the executable section of a loaded module containing the address, and returns false if none does.
bool SF_Code_FindCodeSection(std::uintptr_t address, std::uintptr_t* begin, std::uintptr_t* end);

// Called with g_codeWritesLock held, before a SafetyHook hook moves the instructions in the range to its trampoline.
PatchFault SF_Code_CheckMoved(std::uintptr_t begin, std::size_t movedLength);

// Called with g_codeWritesLock held. Returns an absolute jump to the destination within reach of a branch at the address, or 0 if no memory lies within reach.
std::uintptr_t SF_Code_AllocateJump(std::uintptr_t branch, std::uintptr_t destination);
