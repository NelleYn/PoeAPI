using System;
using System.Runtime.InteropServices;

namespace ExileCore.Shared.SomeMagic;

/// <summary>
/// A fixed-size unmanaged memory block allocated from the process heap, freed on disposal.
/// </summary>
public class LocalAllocation : IDisposable
{
    /// <summary>
    /// Allocates an unmanaged memory block of the given size.
    /// </summary>
    /// <param name="size">The size of the allocation, in bytes.</param>
    public LocalAllocation(int size)
    {
        Size = size;
        AllocationBase = Marshal.AllocHGlobal(Size);
    }

    /// <summary>
    /// Gets the size of the allocation, in bytes.
    /// </summary>
    public int Size { get; }

    /// <summary>
    /// Gets the base address of the allocation, or <see cref="IntPtr.Zero" /> once disposed.
    /// </summary>
    public IntPtr AllocationBase { get; private set; }

    /// <summary>
    /// Frees the unmanaged memory block.
    /// </summary>
    public void Dispose()
    {
        Marshal.FreeHGlobal(AllocationBase);
        AllocationBase = IntPtr.Zero;
        GC.SuppressFinalize(this);
    }

    ~LocalAllocation()
    {
        Dispose();
    }

    /// <summary>
    /// Reads the entire allocation into a managed byte array.
    /// </summary>
    /// <returns>The bytes of the allocation.</returns>
    public byte[] Read()
    {
        var bytes = new byte[Size];
        Marshal.Copy(AllocationBase, bytes, 0, Size);
        return bytes;
    }

    /// <summary>
    /// Marshals the allocation contents into a managed structure.
    /// </summary>
    /// <typeparam name="T">The structure type to read.</typeparam>
    /// <returns>The marshaled structure.</returns>
    public T Read<T>()
    {
        return (T) Marshal.PtrToStructure(AllocationBase, typeof(T));
    }

    /// <summary>
    /// Writes the given bytes into the allocation.
    /// </summary>
    /// <param name="bytes">The bytes to write.</param>
    public void Write(byte[] bytes)
    {
        Marshal.Copy(bytes, 0, AllocationBase, bytes.Length);
    }

    /// <summary>
    /// Marshals a managed structure into the allocation.
    /// </summary>
    /// <typeparam name="T">The structure type to write.</typeparam>
    /// <param name="generic">The structure to write.</param>
    public void Write<T>(T generic)
    {
        Marshal.StructureToPtr(generic, AllocationBase, false);
    }
}
