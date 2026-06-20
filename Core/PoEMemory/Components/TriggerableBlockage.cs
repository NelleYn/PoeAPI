using SharpDX;

namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing the open/closed state and grid bounds of a triggerable blockage.
/// </summary>
public class TriggerableBlockage : Component
{
    /// <summary>Gets a value indicating whether the blockage is closed.</summary>
    public bool IsClosed => Address != 0 && M.Read<byte>(Address + 0x30) == 1;

    /// <summary>Gets a value indicating whether the blockage is open.</summary>
    public bool IsOpened => Address != 0 && M.Read<byte>(Address + 0x30) == 0;

    /// <summary>Gets the minimum (top-left) grid corner of the blockage area.</summary>
    public Point Min => new(M.Read<int>(Address + 0x50), M.Read<int>(Address + 0x54));

    /// <summary>Gets the maximum (bottom-right) grid corner of the blockage area.</summary>
    public Point Max => new(M.Read<int>(Address + 0x58), M.Read<int>(Address + 0x5C));

    /// <summary>Gets the raw passability/blockage data spanning the grid area.</summary>
    public byte[] Data
    {
        get
        {
            var start = M.Read<long>(Address + 0x38);
            var end = M.Read<long>(Address + 0x40);
            return M.ReadBytes(start, (int) (end - start));
        }
    }
}
