using System;
using System.Text;

namespace ExileCore.Shared.SomeMagic;

/// <summary>
/// Reads and writes memory in a remote process, following multi-level pointer chains and converting types.
/// </summary>
public class MemoryLiterate
{
    private readonly SafeMemoryHandle _safeMemoryHandle;

    /// <summary>
    /// Initializes a new instance bound to the given process handle.
    /// </summary>
    /// <param name="safeMemoryHandle">The handle of the process to read from and write to.</param>
    public MemoryLiterate(SafeMemoryHandle safeMemoryHandle)
    {
        _safeMemoryHandle = safeMemoryHandle;
    }

    /// <summary>
    /// Reads a block of raw bytes from the given address.
    /// </summary>
    /// <param name="address">The address to read from.</param>
    /// <param name="size">The number of bytes to read.</param>
    /// <returns>The bytes read.</returns>
    public byte[] Read(IntPtr address, int size)
    {
        var buffer = new byte[size];
        NativeMethods.ReadProcessMemory(_safeMemoryHandle, address, buffer, size);
        return buffer;
    }

    /// <summary>
    /// Reads a block of raw bytes by following the pointer's offset chain.
    /// </summary>
    /// <param name="pointer">The pointer describing the base address and offset chain.</param>
    /// <param name="size">The number of bytes to read at the final address.</param>
    /// <returns>The bytes read.</returns>
    public byte[] Read(Pointer pointer, int size)
    {
        byte[] buffer;

        if (pointer.Offsets.Count == 0)
        {
            buffer = new byte[size];
            NativeMethods.ReadProcessMemory(_safeMemoryHandle, pointer.BaseAddress, buffer, size);
            return buffer;
        }

        var addressSize = MarshalType<IntPtr>.Size;
        buffer = new byte[addressSize];
        NativeMethods.ReadProcessMemory(_safeMemoryHandle, pointer.BaseAddress, buffer, addressSize);
        var address = TypeConverter.BytesToGenericType<IntPtr>(buffer);
        var offsetsCount = pointer.Offsets.Count - 1;

        for (var i = 0; i < offsetsCount; ++i)
        {
            NativeMethods.ReadProcessMemory(_safeMemoryHandle, address + pointer.Offsets[i], buffer, addressSize);
            address = TypeConverter.BytesToGenericType<IntPtr>(buffer);
        }

        buffer = new byte[size];
        NativeMethods.ReadProcessMemory(_safeMemoryHandle, address + pointer.Offsets[offsetsCount], buffer, size);
        return buffer;
    }

    /// <summary>
    /// Reads a value of the given struct type from the address.
    /// </summary>
    /// <typeparam name="T">The struct type to read.</typeparam>
    /// <param name="address">The address to read from.</param>
    /// <returns>The value read.</returns>
    public T Read<T>(IntPtr address) where T : struct
    {
        return TypeConverter.BytesToGenericType<T>(Read(address, MarshalType<T>.Size));
    }

    /// <summary>
    /// Reads a value of the given struct type by following the pointer's offset chain.
    /// </summary>
    /// <typeparam name="T">The struct type to read.</typeparam>
    /// <param name="pointer">The pointer describing the base address and offset chain.</param>
    /// <returns>The value read.</returns>
    public T Read<T>(Pointer pointer) where T : struct
    {
        return TypeConverter.BytesToGenericType<T>(Read(pointer, MarshalType<T>.Size));
    }

    /// <summary>
    /// Reads an array of values of the given struct type starting at the address.
    /// </summary>
    /// <typeparam name="T">The struct type to read.</typeparam>
    /// <param name="address">The address of the first element.</param>
    /// <param name="count">The number of elements to read.</param>
    /// <returns>The array of values read.</returns>
    public T[] Read<T>(IntPtr address, int count) where T : struct
    {
        var el = new T[count];

        for (var i = 0; i < count; i++)
        {
            el[i] = Read<T>(address + i * MarshalType<T>.Size);
        }

        return el;
    }

    /// <summary>
    /// Reads a null-terminated string from the address using the given encoding.
    /// </summary>
    /// <param name="address">The address to read from.</param>
    /// <param name="size">The maximum number of bytes to read.</param>
    /// <param name="encoding">The encoding used to decode the bytes.</param>
    /// <returns>The decoded string, truncated at the first null character.</returns>
    public string Read(IntPtr address, int size, Encoding encoding)
    {
        var buffer = Read(address, size);
        var s = encoding.GetString(buffer);
        var i = s.IndexOf('\0');

        if (i != -1)
            s = s.Remove(i);

        return s;
    }

    /// <summary>
    /// Reads a null-terminated string by following the pointer's offset chain.
    /// </summary>
    /// <param name="pointer">The pointer describing the base address and offset chain.</param>
    /// <param name="size">The maximum number of bytes to read.</param>
    /// <param name="encoding">The encoding used to decode the bytes.</param>
    /// <returns>The decoded string, truncated at the first null character.</returns>
    public string Read(Pointer pointer, int size, Encoding encoding)
    {
        var buffer = Read(pointer, size);
        var s = encoding.GetString(buffer);
        var i = s.IndexOf('\0');

        if (i != -1)
            s = s.Remove(i);

        return s;
    }

    /// <summary>
    /// Writes raw bytes to the address, temporarily lifting memory protection.
    /// </summary>
    /// <param name="address">The address to write to.</param>
    /// <param name="bytes">The bytes to write.</param>
    /// <returns><see langword="true" /> if all bytes were written; otherwise, <see langword="false" />.</returns>
    public bool Write(IntPtr address, byte[] bytes)
    {
        using (new MemoryProtection(_safeMemoryHandle, address, bytes.Length))
        {
            return NativeMethods.WriteProcessMemory(_safeMemoryHandle, address, bytes, bytes.Length) == bytes.Length;
        }
    }

    /// <summary>
    /// Writes raw bytes by following the pointer's offset chain, temporarily lifting memory protection.
    /// </summary>
    /// <param name="pointer">The pointer describing the base address and offset chain.</param>
    /// <param name="bytes">The bytes to write at the final address.</param>
    /// <returns><see langword="true" /> if all bytes were written; otherwise, <see langword="false" />.</returns>
    public bool Write(Pointer pointer, byte[] bytes)
    {
        if (pointer.Offsets.Count == 0)
        {
            using (new MemoryProtection(_safeMemoryHandle, pointer.BaseAddress, bytes.Length))
            {
                return NativeMethods.WriteProcessMemory(_safeMemoryHandle, pointer.BaseAddress, bytes, bytes.Length) == bytes.Length;
            }
        }

        var addressSize = MarshalType<IntPtr>.Size;
        var address = TypeConverter.BytesToGenericType<IntPtr>(Read(pointer.BaseAddress, addressSize));
        var offsetsCount = pointer.Offsets.Count - 1;

        for (var i = 0; i < offsetsCount; ++i)
        {
            address = TypeConverter.BytesToGenericType<IntPtr>(Read(address + pointer.Offsets[i], addressSize));
        }

        address += pointer.Offsets[offsetsCount];

        using (new MemoryProtection(_safeMemoryHandle, address, bytes.Length))
        {
            return NativeMethods.WriteProcessMemory(_safeMemoryHandle, address, bytes, bytes.Length) == bytes.Length;
        }
    }

    /// <summary>
    /// Writes a value of the given struct type to the address.
    /// </summary>
    /// <typeparam name="T">The struct type to write.</typeparam>
    /// <param name="address">The address to write to.</param>
    /// <param name="value">The value to write.</param>
    /// <returns><see langword="true" /> if all bytes were written; otherwise, <see langword="false" />.</returns>
    public bool Write<T>(IntPtr address, T value) where T : struct
    {
        return Write(address, TypeConverter.GenericTypeToBytes(value));
    }

    /// <summary>
    /// Writes a value of the given struct type by following the pointer's offset chain.
    /// </summary>
    /// <typeparam name="T">The struct type to write.</typeparam>
    /// <param name="pointer">The pointer describing the base address and offset chain.</param>
    /// <param name="value">The value to write.</param>
    /// <returns><see langword="true" /> if all bytes were written; otherwise, <see langword="false" />.</returns>
    public bool Write<T>(Pointer pointer, T value) where T : struct
    {
        return Write(pointer, TypeConverter.GenericTypeToBytes(value));
    }

    /// <summary>
    /// Writes a null-terminated string to the address using the given encoding.
    /// </summary>
    /// <param name="address">The address to write to.</param>
    /// <param name="value">The string to write; a terminating null is appended if absent.</param>
    /// <param name="encoding">The encoding used to encode the string.</param>
    /// <returns><see langword="true" /> if all bytes were written; otherwise, <see langword="false" />.</returns>
    public bool Write(IntPtr address, string value, Encoding encoding)
    {
        if (value[value.Length - 1] != '\0')
            value += '\0';

        var bytes = encoding.GetBytes(value);
        return Write(address, bytes);
    }

    /// <summary>
    /// Writes a null-terminated string by following the pointer's offset chain using the given encoding.
    /// </summary>
    /// <param name="pointer">The pointer describing the base address and offset chain.</param>
    /// <param name="value">The string to write; a terminating null is appended if absent.</param>
    /// <param name="encoding">The encoding used to encode the string.</param>
    /// <returns><see langword="true" /> if all bytes were written; otherwise, <see langword="false" />.</returns>
    public bool Write(Pointer pointer, string value, Encoding encoding)
    {
        if (value[value.Length - 1] != '\0')
            value += '\0';

        var bytes = encoding.GetBytes(value);
        return Write(pointer, bytes);
    }
}
