using System.Runtime.CompilerServices;

namespace SkyrimForge.Hooks;

// Matches the order of xmm0 to xmm15 in SafetyHook's Context64.
[InlineArray(16)]
internal struct XmmRegisters
{
    private Xmm element;
}
