using System.IO;

namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing the identity and icon of a Blight tower from its data record.
/// </summary>
public class BlightTower : Component
{
    private string _id;
    private string _name;
    private string _icon;
    private string _iconFileName;
    private long? _datInfo;
    private long DatInfo => M.Read<long>(Address + 0x28);

    /// <summary>Gets the data id of the Blight tower (cached after the first read).</summary>
    public string Id => _id = _id ?? M.ReadStringU(M.Read<long>(DatInfo));

    /// <summary>Gets the display name of the Blight tower (cached after the first read).</summary>
    public string Name => _name = _name ?? M.ReadStringU(M.Read<long>(DatInfo + 0x8));

    /// <summary>Gets the icon path of the Blight tower (cached after the first read).</summary>
    public string Icon => _icon = _icon ?? M.ReadStringU(M.Read<long>(DatInfo + 0x18));

    /// <summary>Gets the icon file name without extension (cached after the first read).</summary>
    public string IconFileName => _iconFileName = _iconFileName ?? Path.GetFileNameWithoutExtension(Icon);
}
