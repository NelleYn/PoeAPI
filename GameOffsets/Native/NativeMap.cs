using System.Runtime.InteropServices;

namespace GameOffsets.Native;

/// <summary>
/// Header of the game's native map/dictionary structure: a pointer to the contents and the element count.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct NativeMap
{
    /// <inheritdoc/>
    public override string ToString()
    {
        return string.Format("Head: 0x{0}, Size: 0x{1}", Head.ToString("X"), Size);
    }

    public readonly long Head;
    public readonly uint Size;
}
