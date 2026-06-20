using System;
using System.Runtime.InteropServices;

namespace ExileCore.Shared.PInvoke;

/// <summary>
/// Native CLIENT_ID structure identifying a process and thread.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct ClientID
{
    public IntPtr UniqueProcess;
    public IntPtr UniqueThread;
}
