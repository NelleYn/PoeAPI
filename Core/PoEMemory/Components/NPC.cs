namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing NPC indicator state such as overhead icons and minimap label visibility.
/// </summary>
public class NPC : Component
{
    /// <summary>Gets a value indicating whether the NPC has an icon shown overhead.</summary>
    public bool HasIconOverhead => M.Read<long>(Address + 0x48) != 0;

    /// <summary>Gets a value indicating whether the NPC is flagged to be ignored when hidden.</summary>
    public bool IsIgnoreHidden => M.Read<byte>(Address + 0x20) == 1;

    /// <summary>Gets a value indicating whether the NPC's minimap label is visible.</summary>
    public bool IsMinMapLabelVisible => M.Read<byte>(Address + 0x21) == 1;
}
