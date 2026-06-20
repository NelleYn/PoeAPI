using ExileCore.Shared.Interfaces;

namespace ExileCore.PoEMemory.MemoryObjects;

/// <summary>
/// Helper for reading native (std::string-style) strings from process memory, accounting for
/// the small-string optimization where short strings are stored inline.
/// </summary>
public class NativeStringReader
{
    /// <summary>
    /// Reads a native string at <paramref name="address"/>, dereferencing the pointer when the
    /// capacity indicates the string is heap-allocated rather than stored inline.
    /// </summary>
    /// <param name="address">Address of the native string struct.</param>
    /// <param name="M">Memory reader to use.</param>
    /// <returns>The decoded string.</returns>
    public static string ReadString(long address, IMemory M)
    {
        var Size = M.Read<uint>(address + 0x10);
        var Capacity = M.Read<uint>(address + 0x18);

        if ( /*8 <= size ||*/ 8 <= Capacity) //Have no idea how to deal with this
        {
            var readAddr = M.Read<long>(address);
            return M.ReadStringU(readAddr);
        }

        return M.ReadStringU(address);
    }
}
