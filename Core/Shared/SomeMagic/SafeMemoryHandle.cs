using System;
using System.Runtime.ConstrainedExecution;
using System.Security;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

namespace ExileCore.Shared.SomeMagic;

/// <summary>
/// A safe wrapper around a native process handle that closes the handle when released.
/// </summary>
[HostProtection(MayLeakOnAbort = true)]
[SuppressUnmanagedCodeSecurity]
public sealed class SafeMemoryHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    /// <summary>
    /// Initializes a new instance that owns the underlying handle.
    /// </summary>
    public SafeMemoryHandle() : base(true)
    {
    }

    /// <summary>
    /// Initializes a new instance wrapping an existing native handle.
    /// </summary>
    /// <param name="handle">The native handle to wrap.</param>
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public SafeMemoryHandle(IntPtr handle) : base(true)
    {
        SetHandle(handle);
    }

    /// <summary>
    /// Closes the underlying native handle.
    /// </summary>
    /// <returns><see langword="true" /> if the handle was released successfully; otherwise, <see langword="false" />.</returns>
    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
    protected override bool ReleaseHandle()
    {
        return handle != IntPtr.Zero && Imports.CloseHandle(handle);
    }
}
