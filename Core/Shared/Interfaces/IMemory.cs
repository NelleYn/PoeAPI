using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using ExileCore.PoEMemory;
using ExileCore.Shared.Enums;

namespace ExileCore.Shared.Interfaces;

/// <summary>
/// Abstraction over reading the target game's process memory, including strings, structs, pointer arrays and pattern scans.
/// </summary>
public interface IMemory : IDisposable
{
    IntPtr MainWindowHandle { get; }
    IntPtr OpenProcessHandle { get; }
    long AddressOfProcess { get; }
    Dictionary<OffsetsName, long> BaseOffsets { get; }
    Process Process { get; }

    /// <summary>
    /// Read string as ASCII
    /// </summary>
    /// <param name="addr">Address of the string in process memory.</param>
    /// <param name="length">Maximum number of characters to read.</param>
    /// <param name="replaceNull">Whether to strip trailing null characters.</param>
    /// <returns>The decoded ASCII string.</returns>
    string ReadString(long addr, int length = 256, bool replaceNull = true);

    string ReadNativeString(long addr);

    /// <summary>
    /// Read string as Unicode
    /// </summary>
    /// <param name="addr">Address of the string in process memory.</param>
    /// <param name="length">Maximum number of characters to read.</param>
    /// <param name="replaceNull">Whether to strip trailing null characters.</param>
    /// <returns>The decoded Unicode string.</returns>
    string ReadStringU(long addr, int length = 256, bool replaceNull = true);

    byte[] ReadMem(long addr, int size);
    byte[] ReadMem(IntPtr addr, int size);
    byte[] ReadBytes(long addr, int size);
    byte[] ReadBytes(long addr, long size);

    List<T> ReadStructsArray<T>(long startAddress, long endAddress, int structSize, RemoteMemoryObject game)
        where T : RemoteMemoryObject, new();

    IList<T> ReadDoublePtrVectorClasses<T>(long address, RemoteMemoryObject game, bool noNullPointers = false)
        where T : RemoteMemoryObject, new();

    IList<long> ReadPointersArray(long startAddress, long endAddress, int offset = 8);
    IList<long> ReadSecondPointerArray_Count(long startAddress, int count);
    T Read<T>(Pointer addr, params int[] offsets) where T : struct;
    T Read<T>(IntPtr addr, params int[] offsets) where T : struct;
    T Read<T>(long addr, params int[] offsets) where T : struct;
    T Read<T>(Pointer addr) where T : struct;
    T Read<T>(IntPtr addr) where T : struct;
    T Read<T>(long addr) where T : struct;
    IList<T> ReadNativeArray<T>(INativePtrArray ptrArray, int offset = 8) where T : struct;
    IList<Tuple<long, int>> ReadDoublePointerIntList(long address);
    IList<T> ReadList<T>(IntPtr head) where T : struct;
    IList<long> ReadListPointer(IntPtr head);
    long[] FindPatterns(params IPattern[] patterns);
    bool IsInvalid();
}
