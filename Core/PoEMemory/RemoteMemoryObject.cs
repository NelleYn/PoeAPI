using System;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Interfaces;

namespace ExileCore.PoEMemory;

/// <summary>
/// Base class for every object backed by a location in the game process's memory.
/// Wraps a single <see cref="Address"/> and exposes helpers for materializing
/// related objects from pointers stored relative to that address.
/// </summary>
public abstract class RemoteMemoryObject
{
    private long _address;

    /// <summary>
    /// Gets the memory address this object is anchored at. Setting a new value
    /// raises <see cref="OnAddressChange"/>.
    /// </summary>
    public long Address
    {
        get => _address;
        protected set
        {
            if (_address != value)
            {
                _address = value;
                OnAddressChange();
            }
        }
    }

    /// <summary>Gets the shared object cache.</summary>
    public static Cache Cache => pCache;

    /// <summary>Gets the memory reader used to access the game process.</summary>
    public IMemory M => pM;

    /// <summary>Gets the current game context.</summary>
    public TheGame TheGame => pTheGame;

    /// <summary>Gets or sets the shared game context backing <see cref="TheGame"/>.</summary>
    public static TheGame pTheGame { get; protected set; }

    /// <summary>Gets or sets the shared cache backing <see cref="Cache"/>.</summary>
    protected static Cache pCache { get; set; }

    /// <summary>Gets or sets the shared memory reader backing <see cref="M"/>.</summary>
    protected static IMemory pM { get; set; }

    /// <summary>Invoked whenever <see cref="Address"/> changes. Override to refresh cached state.</summary>
    protected virtual void OnAddressChange()
    {
    }

    /// <summary>Dereferences the pointer stored at <see cref="Address"/> + <paramref name="offset"/> and materializes a <typeparamref name="T"/>.</summary>
    public T ReadObjectAt<T>(int offset) where T : RemoteMemoryObject, new()
    {
        return ReadObject<T>(Address + offset);
    }

    /// <summary>Reads the pointer stored at <paramref name="addressPointer"/> and materializes a <typeparamref name="T"/> at the dereferenced address.</summary>
    public T ReadObject<T>(long addressPointer) where T : RemoteMemoryObject, new()
    {
        var pointer = M.Read<long>(addressPointer);
        var t = new T {Address = pointer};
        return t;
    }

    /// <summary>Materializes a <typeparamref name="T"/> located at <see cref="Address"/> + <paramref name="offset"/>.</summary>
    public T GetObjectAt<T>(int offset) where T : RemoteMemoryObject, new()
    {
        return GetObject<T>(Address + offset);
    }

    /// <summary>Materializes a <typeparamref name="T"/> located at <see cref="Address"/> + <paramref name="offset"/>.</summary>
    public T GetObjectAt<T>(long offset) where T : RemoteMemoryObject, new()
    {
        return GetObject<T>(Address + offset);
    }

    /// <summary>Materializes a <typeparamref name="T"/> anchored directly at <paramref name="address"/>.</summary>
    public T GetObject<T>(long address) where T : RemoteMemoryObject, new()
    {
        var t = new T {Address = address};
        return t;
    }

    /// <summary>Materializes a <typeparamref name="T"/> anchored at <paramref name="address"/>.</summary>
    public T GetObject<T>(IntPtr address) where T : RemoteMemoryObject, new()
    {
        return GetObject<T>(address.ToInt64());
    }

    /// <summary>Materializes a new <typeparamref name="T"/> anchored at this object's own <see cref="Address"/>, reinterpreting it as a different type.</summary>
    public T AsObject<T>() where T : RemoteMemoryObject, new()
    {
        var t = new T {Address = Address};
        return t;
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        return obj is RemoteMemoryObject remoteMemoryObject && remoteMemoryObject.Address == Address;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return (int) Address + GetType().Name.GetHashCode();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Address:X}";
    }
}
