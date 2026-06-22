using System.Runtime.InteropServices;

namespace GameOffsets.Native;

/// <summary>
/// Layout of the game's native UTF-16 string (32 bytes). Verified against client 328.8 via an
/// in-process Marshal.OffsetOf dump. Identical layout to <see cref="NativeUnicodeText"/>; kept under
/// this name because reconstructed types reference it directly.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct NativeUtf16Text
{
    /// <summary>Pointer to the UTF-16 character buffer (or inline storage for short strings).</summary>
    public long Buffer;

    /// <summary>Reserved 8 bytes, also used for inline storage of short strings.</summary>
    public long Reserved8Bytes;

    /// <summary>String length (multiply by 2 for the UTF-16 byte length).</summary>
    public long Length;

    /// <summary>String length including the null terminator.</summary>
    public long LengthWithNullTerminator;
}
