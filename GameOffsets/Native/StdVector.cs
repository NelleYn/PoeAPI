using System.Runtime.InteropServices;

namespace GameOffsets.Native;

/// <summary>
/// Mirror of the game's native std::vector (begin/end/capacity pointers). Layout verified against
/// client 328.8 via an in-process Marshal.OffsetOf dump. Equivalent to <see cref="NativePtrArray"/>;
/// both names are kept because different parts of the reconstructed codebase reference each.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct StdVector
{
    /// <summary>Pointer to the first element.</summary>
    public long First;

    /// <summary>Pointer to one past the last element.</summary>
    public long Last;

    /// <summary>Pointer to the end of the allocated capacity.</summary>
    public long End;

    /// <summary>The number of bytes between <see cref="First"/> and <see cref="Last"/>.</summary>
    public long Size => Last - First;
}
