using ExileCore.PoEMemory.MemoryObjects;

namespace ExileCore.PoEMemory.Elements;

/// <summary>
/// UI element representing a text label attached to an entity (for example, a chat message or floating name).
/// </summary>
public class EntityLabel : Element
{
    /// <summary>
    /// Gets the length of the label string, clamped to <c>0</c> when out of the expected range.
    /// </summary>
    public int Length
    {
        get
        {
            var num = (int) M.Read<long>(Address + 0xC60);
            return num <= 0 || num > 1024 ? 0 : num;
        }
    }

    /// <summary>
    /// Gets the capacity of the label string buffer, clamped to <c>0</c> when out of the expected range.
    /// </summary>
    public int Capacity
    {
        get
        {
            var num = (int) M.Read<long>(Address + 0xC68);
            return num <= 0 || num > 1024 ? 0 : num;
        }
    }

    /// <summary>
    /// Gets the label text, reading either inline or via an indirect pointer depending on capacity.
    /// </summary>
    public string Text
    {
        get
        {
            var LabelLen = Length;

            if (LabelLen <= 0 || LabelLen > 1024)
            {
                return string.Empty;
            }

            if (Capacity >= 8)
            {
                var read = M.Read<long>(Address + 0xC50);

                return M.ReadStringU(read, LabelLen * 2, false);
            }

            return M.ReadStringU(Address + 0xC50, LabelLen * 2, false);
        }
    }

    /// <summary>
    /// Gets an alternate label string read from a native string at a fixed offset.
    /// </summary>
    public string Text2 => NativeStringReader.ReadString(Address + 0x2E8, M);
}
