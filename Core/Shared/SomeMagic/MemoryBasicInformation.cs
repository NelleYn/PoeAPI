using System;
using System.Runtime.InteropServices;
using ExileCore.Shared.Enums;

namespace ExileCore.Shared.SomeMagic;

/// <summary>
/// Managed equivalent of the native MEMORY_BASIC_INFORMATION structure returned by VirtualQuery.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct MemoryBasicInformation
{
    public IntPtr BaseAddress;
    public IntPtr AllocationBase;
    public MemoryProtectionType AllocationProtect;
    public IntPtr RegionSize;
    public MemoryAllocationState State;
    public MemoryProtectionType Protect;
    public MemoryAllocationType Type;
}
