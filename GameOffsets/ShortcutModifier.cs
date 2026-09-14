namespace GameOffsets;

/// <summary>
/// Modifier key attached to an in-game shortcut binding. Values are virtual-key codes.
/// </summary>
public enum ShortcutModifier
{
    /// <summary>Alt key (VK_MENU).</summary>
    Alt = 18,

    /// <summary>Control key (VK_CONTROL).</summary>
    Ctrl = 17,

    /// <summary>No modifier key.</summary>
    None = 0,

    /// <summary>Shift key (VK_SHIFT).</summary>
    Shift = 16
}
