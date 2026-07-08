using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using ExileCore.PoEMemory;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.SomeMagic;
using GameOffsets.Native;
using ProcessMemoryUtilities.Memory;

namespace ExileCore;

/// <summary>
/// Reads the Path of Exile process memory: opens a read handle to the process, exposes typed
/// <c>Read&lt;T&gt;</c> and string/array readers, and performs the initial pattern scans to locate
/// the base offsets. All reads are best-effort and return defaults/empty on invalid addresses.
/// </summary>
public class Memory : IMemory
{
    private bool closed;
    private readonly Stopwatch sw = Stopwatch.StartNew();

    /// <summary>Opens a read handle to the supplied game process and runs the base pattern scans.</summary>
    public Memory((Process, Offsets) tuple)
    {
        Process = tuple.Item1;
        AddressOfProcess = Process.MainModule.BaseAddress.ToInt64();
        MainWindowHandle = Process.MainWindowHandle;

        OpenProcessHandle = ProcessMemory.OpenProcess(ProcessAccessFlags.VirtualMemoryRead, Process.Id);

        BaseOffsets = tuple.Item2.DoPatternScans(this);
    }

    /// <summary>The game's main window handle.</summary>
    public IntPtr MainWindowHandle { get; }

    /// <summary>The open read handle to the game process.</summary>
    public IntPtr OpenProcessHandle { get; }

    /// <summary>The base address of the game's main module.</summary>
    public long AddressOfProcess { get; }

    /// <summary>The base offsets resolved by the initial pattern scan.</summary>
    public Dictionary<OffsetsName, long> BaseOffsets { get; }

    /// <summary>The game process.</summary>
    public Process Process { get; }

    /// <summary>Reads an ASCII string at the given address.</summary>
    public string ReadString(long addr, int length = 256, bool replaceNull = true)
    {
        if (addr <= 65536 && addr >= -1) return string.Empty;
        var @string = Encoding.ASCII.GetString(ReadMem(new IntPtr(addr), length));
        return replaceNull ? RTrimNull(@string) : @string;
    }

    /// <summary>Reads a native (length-prefixed) string structure at the given address.</summary>
    public string ReadNativeString(long address)
    {
        var Size = Read<uint>(address + 0x8);
        var Reserved = Read<uint>(address + 0x10);

        if (Reserved == 0)
            return string.Empty;

        if ( /*8 <= size ||*/ 8 <= Reserved) //Have no idea how to deal with this
        {
            var readAddr = Read<long>(address);
            return ReadStringU(readAddr);
        }

        return ReadStringU(address);
    }

    /// <summary>Reads a Unicode (UTF-16) string at the given address.</summary>
    public string ReadStringU(long addr, int length = 256, bool replaceNull = true)
    {
        if (length > 5120 || length < 0)
            return string.Empty;

        if (addr == 0)
            return string.Empty;

        var mem = ReadMem(new IntPtr(addr), length);

        if (mem.Length == 0)
            return string.Empty;

        if (mem[0] == 0 && mem[1] == 0)
            return string.Empty;

        var @string = Encoding.Unicode.GetString(mem);
        return replaceNull ? RTrimNull(@string) : @string;
    }

    /// <summary>Reads <paramref name="size"/> raw bytes from the given address.</summary>
    public byte[] ReadMem(long addr, int size)
    {
        return ReadMem(new IntPtr(addr), size);
    }

    /// <summary>Reads <paramref name="size"/> raw bytes from the given address. Returns an empty array on invalid input.</summary>
    public byte[] ReadMem(IntPtr address, int size)
    {
        try
        {
            if (size <= 0 || address.ToInt64() <= 0 /*|| !AddressIsValid(address)*/) return new byte[0];
            var buffer = new byte[size];
            ProcessMemory.ReadProcessMemoryArray(OpenProcessHandle, address, buffer, 0, size);

            return buffer;
        }
        catch (Exception e)
        {
            DebugWindow.LogError($"Readmem-> A: {address} Size: {size}. {e}");
            throw;
        }
    }

    /// <summary>Reads <paramref name="size"/> raw bytes from the given address.</summary>
    public byte[] ReadBytes(long addr, int size)
    {
        return ReadMem(addr, size);
    }

    /// <summary>Reads <paramref name="size"/> raw bytes from the given address.</summary>
    public byte[] ReadBytes(long addr, long size)
    {
        return ReadMem(addr, (int) size);
    }

    /// <summary>Reads a contiguous array of remote-memory objects between two addresses, guarding against overflow.</summary>
    public List<T> ReadStructsArray<T>(long startAddress, long endAddress, int structSize, RemoteMemoryObject game)
        where T : RemoteMemoryObject, new()
    {
        var result = new List<T>();
        var i = 0;
        var size = (endAddress - startAddress) / structSize;

        if (size < 0 || size > 100000)
        {
            DebugWindow.LogError($"Maybe overflow memory in {nameof(ReadStructsArray)} for reading structures of type: {typeof(T).Name}", 3);
            //throw new OverflowException($"Maybe overflow memory in {nameof(ReadStructsArray)} so much structs {size}");
            return result;
        }

        for (var address = startAddress; address < endAddress; address += structSize)
        {
            result.Add(game.GetObject<T>(address));
            i++;

            if (i > 100000)
            {
                //throw new OverflowException($"Maybe overflow memory in {nameof(ReadStructsArray)} so much structs {size}");
                DebugWindow.LogError($"Maybe overflow memory in {nameof(ReadStructsArray)} for reading structures of type: {typeof(T).Name}", 3);
            }
        }

        return result;
    }

    /// <summary>Reads a vector of pointers to remote-memory objects (start/last pointers stored at the given address).</summary>
    public IList<T> ReadDoublePtrVectorClasses<T>(long address, RemoteMemoryObject game, bool noNullPointers = false)
        where T : RemoteMemoryObject, new()
    {
        var start = Read<long>(address);

        //var end = ReadLong(address + 0x8);
        var last = Read<long>(address + 0x10);

        var length = (int) (last - start);
        var bytes = ReadMem(new IntPtr(start), length);
        var result = new List<T>();
        var sw = Stopwatch.StartNew();

        for (var readOffset = 0; readOffset < length; readOffset += 16)
        {
            if (sw.ElapsedMilliseconds > 2000)
            {
                DebugWindow.LogError($"ReadDoublePtrVectorClasses error result count: {result.Count}");
                return new List<T>();
            }

            var pointer = BitConverter.ToInt64(bytes, readOffset);

            if (pointer == 0 && noNullPointers)
                continue;

            result.Add(game.GetObject<T>(pointer));
        }

        return result;
    }

    /// <summary>Reads an array of 64-bit pointers between two addresses, stepping by <paramref name="offset"/> bytes.</summary>
    public IList<long> ReadPointersArray(long startAddress, long endAddress, int offset = 8)
    {
        var result = new List<long>();

        var length = endAddress - startAddress;

        if (length <= 0 || length > 20000 * 8)
            return result;

        sw.Restart();
        result = new List<long>((int) (length / offset) + 1);
        var bytes = ReadMem(startAddress, (int) length);

        for (var i = 0; i < length; i += offset)
        {
            if (sw.ElapsedMilliseconds > 2000)
            {
                DebugWindow.LogError($"ReadPointersArray error result count: {result.Count}");
                return new List<long>();
            }

            result.Add(BitConverter.ToInt64(bytes, i));
        }

        return result;
    }

    /// <summary>Reads <paramref name="count"/> consecutive 64-bit pointers starting at the given address.</summary>
    public IList<long> ReadSecondPointerArray_Count(long startAddress, int count)
    {
        var result = new List<long>();

        if (count <= 0 || count > 20000)
            return result;

        var length = count * 8;

        sw.Restart();
        result = new List<long>(count);
        var bytes = ReadMem(startAddress, length);

        for (var i = 0; i < length; i += 8)
        {
            if (sw.ElapsedMilliseconds > 2000)
            {
                DebugWindow.LogError($"ReadSecondPointerArray_Count error result count: {result.Count}");
                return new List<long>();
            }

            result.Add(i + 8 <= bytes.Length ? BitConverter.ToInt64(bytes, i) : 0L);
        }

        return result;
    }

    /// <summary>Reads a value of type <typeparamref name="T"/> by following the pointer's base address and offset chain.</summary>
    public T Read<T>(Pointer addr, params int[] offsets) where T : struct
    {
        if (addr == null) return default;

        var combined = new int[addr.Offsets.Count + offsets.Length];
        addr.Offsets.CopyTo(combined, 0);
        offsets.CopyTo(combined, addr.Offsets.Count);

        return combined.Length == 0 ? Read<T>(addr.BaseAddress) : Read<T>(addr.BaseAddress, combined);
    }

    /// <summary>Reads a value of type <typeparamref name="T"/> by following a chain of pointer offsets from the given address.</summary>
    public T Read<T>(IntPtr addr, params int[] offsets) where T : struct
    {
        if (addr == IntPtr.Zero) return default;
        var num = Read<long>(addr);
        var result = num;

        for (var index = 0; index < offsets.Length - 1; index++)
        {
            if (result == 0)
                return default;

            var offset = offsets[index];
            result = Read<long>(result + offset);
        }

        if (result == 0)
            return default;

        return Read<T>(result + offsets[offsets.Length - 1]);
    }

    /// <summary>Reads a value of type <typeparamref name="T"/> by following a chain of pointer offsets from the given address.</summary>
    public T Read<T>(long addr, params int[] offsets) where T : struct
    {
        return Read<T>(new IntPtr(addr), offsets);
    }

    /// <summary>Reads a value of type <typeparamref name="T"/> by following the pointer's base address and offset chain.</summary>
    public T Read<T>(Pointer addr) where T : struct
    {
        return Read<T>(addr, Array.Empty<int>());
    }

    /// <summary>Reads a value of type <typeparamref name="T"/> directly from the given address.</summary>
    public T Read<T>(IntPtr addr) where T : struct
    {
        if (addr == IntPtr.Zero /*|| !AddressIsValid(addr)*/)
        {
            // throw new Exception($"Invalid address ({addr}) for {typeof(T)}");
            return default;
        }

        var result = new T();
        ProcessMemory.ReadProcessMemory(OpenProcessHandle, addr, ref result);
        return result;
    }

    /// <summary>Reads a value of type <typeparamref name="T"/> directly from the given address.</summary>
    public T Read<T>(long addr) where T : struct
    {
        var ptr = new IntPtr(addr);
        return Read<T>(ptr);
    }

    /// <summary>Walks a native linked list of (key, value) pairs starting from a double pointer at the given address.</summary>
    public IList<Tuple<long, int>> ReadDoublePointerIntList(long address)
    {
        var result = new List<Tuple<long, int>>();
        var head = Read<long>(address);
        var node = Read<NativeListNode>(head);
        result.Add(new Tuple<long, int>(node.Ptr2_Key, node.Value));

        var sw = Stopwatch.StartNew();

        while (head != node.Next)
        {
            if (sw.ElapsedMilliseconds > 2000)
            {
                Core.Logger.Error($"ReadDoublePointerIntList error result count: {result.Count}");
                return new List<Tuple<long, int>>();
            }

            node = Read<NativeListNode>(node.Next);
            result.Add(new Tuple<long, int>(node.Ptr2_Key, node.Value));
        }

        if (result.Count > 0)
            result.RemoveAt(result.Count - 1);

        return result;
    }

    /// <summary>Walks a native linked list from <paramref name="head"/>, reading each node value as type <typeparamref name="T"/>.</summary>
    public IList<T> ReadList<T>(IntPtr head) where T : struct
    {
        var result = new List<T>();

        var node = Read<NativeListNode>(head);

        var sw = Stopwatch.StartNew();
        var headLong = head.ToInt64();

        while (headLong != node.Next)
        {
            if (sw.ElapsedMilliseconds > 2000)
            {
                Core.Logger.Error($"Readlist error result count: {result.Count}");
                return new List<T>();
            }

            result.Add(Read<T>(node.Next));
            node = Read<NativeListNode>(node.Next);
        }

        return result;
    }

    /// <summary>Walks a native linked list from <paramref name="head"/>, collecting each node pointer.</summary>
    public IList<long> ReadListPointer(IntPtr head)
    {
        var result = new List<long>();

        var node = Read<NativeListNode>(head);

        var sw = Stopwatch.StartNew();
        var headLong = head.ToInt64();

        while (headLong != node.Next)
        {
            if (sw.ElapsedMilliseconds > 2000)
            {
                Core.Logger.Error($"ReadListPointer error result count: {result.Count}");
                return new List<long>();
            }

            result.Add(node.Next);
            node = Read<NativeListNode>(node.Next);
        }

        return result;
    }

    /// <summary>Scans the game's executable image for each supplied byte pattern and returns the matched offsets.</summary>
    public long[] FindPatterns(params IPattern[] patterns)
    {
        var exeImage = ReadMem(new IntPtr(AddressOfProcess), Process.MainModule.ModuleMemorySize); //33mb
        var address = new long[patterns.Length];

        bool CompareData(IPattern pattern, IReadOnlyList<byte> data, int offset)
        {
            if (pattern.Bytes[0] != data[offset] || pattern.Bytes[pattern.Bytes.Length - 1] != data[offset + pattern.Bytes.Length - 1])
                return false;

            for (var i = 0; i < pattern.Bytes.Length; i++)
            {
                if (pattern.Mask[i] == 'x' && pattern.Bytes[i] != data[offset + i])
                    return false;
            }

            return true;
        }

        void FindPattern(long iPattern)
        {
            var pattern = patterns[iPattern];
            var patternData = pattern.Bytes;
            var patternLength = patternData.Length;

            var found = false;

            using (new PerformanceTimer($"Pattern: {pattern.Name} -> ", 0,
                (s, span) => DebugWindow.LogMsg(
                    $"{s}: Time: {span.TotalMilliseconds} ms. Offset:[{address[iPattern]}] Started searching offset with:{pattern.StartOffset}"),
                false))
            {
                for (var offset = pattern.StartOffset; offset < exeImage.Length - patternLength; offset++)
                {
                    if (!CompareData(pattern, exeImage, offset)) continue;
                    found = true;
                    address[iPattern] = offset;
                    break;
                }

                if (found) return;

                for (var offset = 0; offset < pattern.StartOffset; offset++)
                {
                    if (!CompareData(pattern, exeImage, offset)) continue;
                    found = true;
                    address[iPattern] = offset;
                    break;
                }
            }
        }

        var MultiThreading = true;

        //Little faster start hud
        //For me ~1400 vs ~800ms

        if (MultiThreading)
            Parallel.For((long) 0, patterns.Length, FindPattern);
        else
        {
            for (var index = 0; index < patterns.Length; index++)
            {
                FindPattern(index);
            }
        }

        exeImage = null;
        return address;
    }

    /// <summary>Returns whether the process has exited or the memory handle has been closed.</summary>
    public bool IsInvalid()
    {
        return Process.HasExited || closed;
    }

    /// <summary>Reads an array of values of type <typeparamref name="T"/> from a native pointer array structure.</summary>
    public IList<T> ReadNativeArray<T>(INativePtrArray ptrArray, int offset = 8) where T : struct
    {
        if (ptrArray.First == IntPtr.Zero)
            return new List<T>();

        var Length = (int) (ptrArray.Last.ToInt64() - ptrArray.First.ToInt64()) / offset;
        var result = new List<T>(Length);
        var pointers = ReadPointersArray(ptrArray.First.ToInt64(), ptrArray.Last.ToInt64(), offset);

        foreach (var pointer in pointers)
        {
            result.Add(Read<T>(pointer));
        }

        return result;
    }

    /// <summary>Closes the process handle and marks the instance as invalid.</summary>
    public void Dispose()
    {
        if (!closed)
        {
            closed = true;

            try
            {
                ProcessMemory.CloseHandle(OpenProcessHandle);
            }
            catch (Exception ex)
            {
                Logger.Log.Error($"Error when dispose memory: {ex.Message}");
            }
        }
    }

    private static string RTrimNull(string text)
    {
        var num = text.IndexOf('\0');
        return num > 0 ? text.Substring(0, num) : text;
    }
}
