using System;
using System.Runtime.InteropServices;

namespace GameOffsets;

/// <summary>
/// A configured in-game shortcut: its main key, modifier and what it is used for. Offsets verified
/// against client 328.8 via an in-process Marshal.OffsetOf dump; enum values from the 328.8
/// GameOffsets assembly.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 12)]
public struct Shortcut
{
    /// <summary>The primary key of the shortcut.</summary>
    [FieldOffset(0x0)] public ConsoleKey MainKey;

    /// <summary>The modifier key (Shift/Ctrl/Alt) of the shortcut.</summary>
    [FieldOffset(0x4)] public ShortcutModifier Modifier;

    /// <summary>What the shortcut is used for.</summary>
    [FieldOffset(0x8)] public ShortcutUsage Usage;

    /// <summary>Gets the modifier prefix text (e.g. "Ctrl + "), or null when there is no modifier.</summary>
    public string ModifierText => (Modifier == ShortcutModifier.None) ? null : $"{Modifier} + ";

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{ModifierText}{MainKey} ({Usage})";
    }
}

/// <summary>Modifier key of a <see cref="Shortcut"/>.</summary>
public enum ShortcutModifier
{
    /// <summary>No modifier.</summary>
    None = 0,

    /// <summary>Shift modifier.</summary>
    Shift = 16,

    /// <summary>Control modifier.</summary>
    Ctrl = 17,

    /// <summary>Alt modifier.</summary>
    Alt = 18
}

/// <summary>What a <see cref="Shortcut"/> is bound to.</summary>
public enum ShortcutUsage
{
    Flask1 = 0,
    Flask2 = 1,
    Flask3 = 2,
    Flask4 = 3,
    Flask5 = 4,
    TempSkill1 = 5,
    TempSkill2 = 6,
    Skill1 = 7,
    Skill2 = 8,
    Skill3 = 9,
    Skill4 = 10,
    Skill5 = 11,
    Skill6 = 12,
    Skill7 = 13,
    Skill8 = 14,
    Skill9 = 15,
    Skill10 = 16,
    Skill11 = 17,
    Skill12 = 18,
    Skill13 = 19,
    OptionsPanel = 28,
    CharacterPanel = 29,
    SocialPanel = 30,
    InventoryPanel = 32,
    SkillTree = 39,
    Atlas = 40,
    AtlasTree = 41,
    LeagueInterface = 43,
    LeaguePanel = 44,
    ToggleDebug = 50,
    ItemPickup = 51,
    StalkerSentinel = 62,
    PandemoniumSentinel = 63,
    ApexSentinel = 64
}
