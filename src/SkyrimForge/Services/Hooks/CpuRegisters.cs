using System.Runtime.InteropServices;

namespace SkyrimForge;

// Mirrors SafetyHook's Context64.
[StructLayout(LayoutKind.Sequential)]
internal struct CpuRegisters
{
    public XmmRegisters Xmm;
    public ulong Rflags;
    public ulong R15;
    public ulong R14;
    public ulong R13;
    public ulong R12;
    public ulong R11;
    public ulong R10;
    public ulong R9;
    public ulong R8;
    public ulong Rdi;
    public ulong Rsi;
    public ulong Rdx;
    public ulong Rcx;
    public ulong Rbx;
    public ulong Rax;
    public ulong Rbp;
    public ulong Rsp;
    public ulong TrampolineRsp;
    public ulong Rip;
}
