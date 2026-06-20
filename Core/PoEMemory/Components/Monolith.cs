namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing the open progress of a breach/essence monolith object.
/// </summary>
public class Monolith : Component
{
    //EssenceTypes: 0x28-0x20 is a range, then read double pointer struct (each second pointer)

    /// <summary>Gets the current open stage of the monolith.</summary>
    public int OpenStage => M.Read<byte>(Address + 0x70);

    /// <summary>Gets a value indicating whether the monolith has been fully opened.</summary>
    public bool IsOpened => OpenStage == 4; //After killing monsters (or on time) this objects disappear
}
