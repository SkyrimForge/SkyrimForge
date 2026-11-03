namespace SkyrimForge.Infrastructure;

/// <summary>Mirrors PatchFault in CodeWrites.h: the reason a patch or hook was not written.</summary>
internal enum PatchFault
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
    NotCall,
    NotCode
}
