namespace ExileCore.PoEMemory.Elements;

/// <summary>
/// Kind of an entry displayed in the in-game invites panel.
/// </summary>
public enum InvitesPanelItemKind
{
    /// <summary>No entry kind.</summary>
    None = 0,

    /// <summary>Party invite.</summary>
    Party = 1,

    /// <summary>Trade request.</summary>
    Trade = 2,

    /// <summary>Friend request.</summary>
    Friend = 3,

    /// <summary>Guild invite.</summary>
    Guild = 4,

    /// <summary>Any other entry kind.</summary>
    Other = 5
}
