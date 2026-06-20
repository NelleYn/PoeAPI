using System;
using System.Collections.Generic;

namespace ExileCore.Shared.SomeMagic;

/// <summary>
/// Describes a multi-level pointer: a base address followed by a chain of offsets to dereference.
/// </summary>
public class Pointer : IDisposable
{
    /// <summary>
    /// Initializes a new pointer with the given base address and offset chain.
    /// </summary>
    /// <param name="baseAddress">The base address.</param>
    /// <param name="offsets">The offsets to dereference in order.</param>
    public Pointer(IntPtr baseAddress, params int[] offsets)
    {
        BaseAddress = baseAddress;

        foreach (var i in offsets)
        {
            Offsets.Add(i);
        }
    }

    /// <summary>
    /// Gets the base address of the pointer chain.
    /// </summary>
    public IntPtr BaseAddress { get; }

    /// <summary>
    /// Gets the ordered list of offsets to dereference from the base address.
    /// </summary>
    public List<int> Offsets { get; } = new List<int>();

    /// <summary>
    /// Releases the pointer.
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    ~Pointer()
    {
        Dispose();
    }
}
