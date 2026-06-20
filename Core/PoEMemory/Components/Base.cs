using ExileCore.Shared.Helpers;
using GameOffsets.Native;

namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing base item information such as name, inventory size, and influence flags.
/// </summary>
public class Base : Component
{
    //x20 - some strings about item
    private string _name;

    /// <summary>Gets the base item name (cached after the first read).</summary>
    public string Name => _name ?? (_name = M.Read<NativeStringU>(Address + 0x10, 0x18).ToString(M));

    /// <summary>Gets the item width in inventory cells.</summary>
    public int ItemCellsSizeX => M.Read<int>(Address + 0x10, 0x10);

    /// <summary>Gets the item height in inventory cells.</summary>
    public int ItemCellsSizeY => M.Read<int>(Address + 0x10, 0x14);

    /// <summary>Gets a value indicating whether the item is corrupted.</summary>
    public bool isCorrupted => M.Read<byte>(Address + 0xD8) == 1;

    /// <summary>Gets a value indicating whether the item carries Shaper influence.</summary>
    public bool isShaper => M.Read<byte>(Address + 0xD9) == 1;

    /// <summary>Gets a value indicating whether the item carries Elder influence.</summary>
    public bool isElder => M.Read<byte>(Address + 0xDA) == 1;

    // public bool isFractured => M.Read<byte>(Address + 0x98) == 0;

    // 0x8 - link to base item
    // +0x10 - Name
    // +0x30 - Use hint
    // +0x50 - Link to Data/BaseItemTypes.dat

    // 0xC (+4) fileref to visual identity
}
