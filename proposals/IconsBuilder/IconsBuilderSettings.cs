// EXPERIMENTAL candidate ported from exApiTools/IconsBuilder — see proposals/IconsBuilder/README.md. Not part of the build.

using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;

namespace ExileCore.IconsBuilder;

/// <summary>
/// Settings consumed by the ported IconsBuilder icon classes. This is a trimmed copy of the
/// original <c>IconsBuilderSettings</c> that keeps only the members the ported core actually reads
/// (per-rarity icon sizes, name toggles and a few behaviour switches).
/// </summary>
/// <remarks>
/// Implements <see cref="ISettings"/> so it can be hosted directly by a
/// <c>BaseSettingsPlugin&lt;IconsBuilderSettings&gt;</c>, or embedded in a host plugin's settings.
/// </remarks>
public class IconsBuilderSettings : ISettings
{
    /// <summary>Master enable switch required by <see cref="ISettings"/>.</summary>
    public ToggleNode Enable { get; set; } = new ToggleNode(true);

    /// <summary>When set, an out-of-range entity that has an in-game minimap icon still gets a built icon.</summary>
    public ToggleNode UseReplacementsForGameIconsWhenOutOfRange { get; set; } = new ToggleNode(true);

    [Menu("Default size")]
    public RangeNode<int> SizeDefaultIcon { get; set; } = new RangeNode<int>(16, 1, 50);

    [Menu("Size NPC icon")]
    public RangeNode<int> SizeNpcIcon { get; set; } = new RangeNode<int>(10, 1, 50);

    [Menu("Size Character icon")]
    public RangeNode<int> SizeSelf { get; set; } = new RangeNode<int>(10, 1, 50);

    [Menu("Size white monster icon")]
    public RangeNode<int> SizeEntityWhiteIcon { get; set; } = new RangeNode<int>(10, 1, 50);

    [Menu("Size magic monster icon")]
    public RangeNode<int> SizeEntityMagicIcon { get; set; } = new RangeNode<int>(10, 1, 50);

    [Menu("Size rare monster icon")]
    public RangeNode<int> SizeEntityRareIcon { get; set; } = new RangeNode<int>(10, 1, 50);

    [Menu("Size unique monster icon")]
    public RangeNode<int> SizeEntityUniqueIcon { get; set; } = new RangeNode<int>(10, 1, 50);

    [Menu("Size proximity monster icon")]
    public RangeNode<int> SizeEntityProximityMonsterIcon { get; set; } = new RangeNode<int>(10, 1, 50);

    [Menu("Size breach chest icon")]
    public RangeNode<int> SizeBreachChestIcon { get; set; } = new RangeNode<int>(10, 1, 50);

    [Menu("Size chests icon")]
    public RangeNode<int> SizeChestIcon { get; set; } = new RangeNode<int>(10, 1, 50);

    [Menu("Show small chests")]
    public ToggleNode ShowSmallChest { get; set; } = new ToggleNode(false);

    [Menu("Size small chests icon")]
    public RangeNode<int> SizeSmallChestIcon { get; set; } = new RangeNode<int>(10, 1, 50);

    [Menu("Size misc icon")]
    public RangeNode<int> SizeMiscIcon { get; set; } = new RangeNode<int>(10, 1, 50);

    [Menu("Size shrine icon")]
    public RangeNode<int> SizeShrineIcon { get; set; } = new RangeNode<int>(10, 1, 50);

    [Menu("Hidden monster icon size")]
    public RangeNode<float> HideSize { get; set; } = new RangeNode<float>(1, 0, 1);

    [Menu("Reparse entities")]
    public ButtonNode Reparse { get; set; } = new ButtonNode();

    public ToggleNode HideSelf { get; set; } = new ToggleNode(false);
    public ToggleNode HideOtherPlayers { get; set; } = new ToggleNode(false);
    public ToggleNode HideMinions { get; set; } = new ToggleNode(false);
    public ToggleNode HideBurriedMonsters { get; set; } = new ToggleNode(false);
    public ToggleNode ShowWhiteMonsterName { get; set; } = new ToggleNode(false);
    public ToggleNode ShowMagicMonsterName { get; set; } = new ToggleNode(false);
    public ToggleNode ShowRareMonsterName { get; set; } = new ToggleNode(false);
    public ToggleNode ShowUniqueMonsterName { get; set; } = new ToggleNode(false);
    public ToggleNode LogDebugInformation { get; set; } = new ToggleNode(false);
}
