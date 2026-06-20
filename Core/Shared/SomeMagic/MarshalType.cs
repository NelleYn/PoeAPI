using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace ExileCore.Shared.SomeMagic;

/// <summary>
/// Caches marshalling metadata (size, type code, pinning delegate) for a value type <typeparamref name="T" />.
/// </summary>
/// <typeparam name="T">The struct type whose marshalling information is cached.</typeparam>
public static class MarshalType<T> where T : struct
{
    internal static readonly GetPointerDelegate GetPointer;

    static MarshalType()
    {
        TypeCode = Type.GetTypeCode(typeof(T));

        // Bools = 1 char.
        if (typeof(T) == typeof(bool))
        {
            Size = 1;
            Type = typeof(T);
        }
        else if (typeof(T).IsEnum)
        {
            var underlying = typeof(T).GetEnumUnderlyingType();
            Size = Marshal.SizeOf(underlying);
            Type = underlying;
            TypeCode = Type.GetTypeCode(underlying);
        }
        else
        {
            Size = Marshal.SizeOf(typeof(T));
            Type = typeof(T);
        }

        IsIntPtr = Type == typeof(IntPtr);

        HasUnmanagedTypes = Type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Any(m => m.GetCustomAttributes(typeof(MarshalAsAttribute), true).Any());

        var method = new DynamicMethod($"GetPinnedPointer<{Type.FullName.Replace(".", "<>")}>", typeof(void*),
            new[] {Type.MakeByRefType()}, typeof(MarshalType<>).Module);

        var gen = method.GetILGenerator();
        gen.Emit(OpCodes.Ldarg_0);
        gen.Emit(OpCodes.Conv_U);
        gen.Emit(OpCodes.Ret);
        GetPointer = (GetPointerDelegate) method.CreateDelegate(typeof(GetPointerDelegate));
    }

    /// <summary>
    /// Gets the effective marshalled type (the underlying type for enums).
    /// </summary>
    public static Type Type { get; }

    /// <summary>
    /// Gets the type code of the effective marshalled type.
    /// </summary>
    public static TypeCode TypeCode { get; }

    /// <summary>
    /// Gets the marshalled size of the type, in bytes.
    /// </summary>
    public static int Size { get; }

    /// <summary>
    /// Gets a value indicating whether the type is <see cref="IntPtr" />.
    /// </summary>
    public static bool IsIntPtr { get; }

    /// <summary>
    /// Gets a value indicating whether the type has fields decorated with <see cref="MarshalAsAttribute" />.
    /// </summary>
    public static bool HasUnmanagedTypes { get; }

    internal unsafe delegate void* GetPointerDelegate(ref T generic);
}
