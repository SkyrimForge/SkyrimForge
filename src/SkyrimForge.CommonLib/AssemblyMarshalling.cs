using System.Runtime.CompilerServices;

// Permits a value struct such as NiPoint3 in a P/Invoke signature, which LibraryImport otherwise reports as SYSLIB1051.
[assembly: DisableRuntimeMarshalling]
