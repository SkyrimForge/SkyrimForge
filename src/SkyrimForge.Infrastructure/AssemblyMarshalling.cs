using System.Runtime.CompilerServices;

// A bool return still needs [MarshalAs(UnmanagedType.U1)].
[assembly: DisableRuntimeMarshalling]
