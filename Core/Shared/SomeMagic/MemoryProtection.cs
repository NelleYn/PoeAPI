using System;
using ExileCore.Shared.Enums;

namespace ExileCore.Shared.SomeMagic;

/// <summary>
/// Temporarily changes the protection of a memory region, restoring the original protection on disposal.
/// </summary>
public class MemoryProtection : IDisposable
{
    /// <summary>
    /// Changes the protection of the given memory region to the requested protection.
    /// </summary>
    /// <param name="processHandle">The handle of the target process.</param>
    /// <param name="address">The start of the region.</param>
    /// <param name="size">The size of the region, in bytes.</param>
    /// <param name="protection">The protection to apply.</param>
    public MemoryProtection(SafeMemoryHandle processHandle, IntPtr address, int size,
        MemoryProtectionType protection = MemoryProtectionType.PAGE_EXECUTE_READWRITE)
    {
        ProcessHandle = processHandle;
        Address = address;
        Size = size;
        NewProtection = protection;
        OldProtection = NativeMethods.ChangeMemoryProtection(ProcessHandle, Address, Size, NewProtection);
    }

    /// <summary>
    /// Gets the handle of the target process.
    /// </summary>
    public static SafeMemoryHandle ProcessHandle { get; private set; }

    /// <summary>
    /// Gets the start address of the protected region.
    /// </summary>
    public static IntPtr Address { get; private set; }

    /// <summary>
    /// Gets the size of the protected region, in bytes.
    /// </summary>
    public static int Size { get; private set; }

    /// <summary>
    /// Gets the protection that was in effect before the change.
    /// </summary>
    public static MemoryProtectionType OldProtection { get; private set; }

    /// <summary>
    /// Gets the protection that was applied.
    /// </summary>
    public static MemoryProtectionType NewProtection { get; private set; }

    /// <summary>
    /// Restores the original protection of the region.
    /// </summary>
    public void Dispose()
    {
        NativeMethods.ChangeMemoryProtection(ProcessHandle, Address, Size, OldProtection);
        GC.SuppressFinalize(this);
    }

    ~MemoryProtection()
    {
        Dispose();
    }
}
