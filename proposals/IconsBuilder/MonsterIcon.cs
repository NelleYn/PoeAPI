// EXPERIMENTAL candidate ported from exApiTools/IconsBuilder — see proposals/IconsBuilder/README.md. Not part of the build.

using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Abstract;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using SharpDX;

namespace ExileCore.IconsBuilder;

/// <summary>
/// Icon for regular monsters. Picks a sprite and size from the monster rarity, and (optionally) a
/// special mod-icon sprite from the <c>modIcons</c> map when the monster carries a known mod.
/// </summary>
public class MonsterIcon : BaseIcon
{
    /// <summary>Builds a monster icon for the given entity.</summary>
    /// <param name="entity">The monster entity.</param>
    /// <param name="gameController">The host game controller (unused here; kept for call-site parity).</param>
    /// <param name="settings">Icon sizes and name toggles.</param>
    /// <param name="modIcons">Map of mod name to a (column,row) sprite cell on <c>sprites.png</c>.</param>
    public MonsterIcon(Entity entity, GameController gameController, IconsBuilderSettings settings, Dictionary<string, Size2> modIcons)
        : base(entity, settings)
    {
        Update(entity, settings, modIcons);
    }

    /// <summary>Entity id, exposed for de-duplication by hosts.</summary>
    public long ID { get; set; }

    private void Update(Entity entity, IconsBuilderSettings settings, Dictionary<string, Size2> modIcons)
    {
        Show = () => entity.IsAlive;
        if (entity.IsHidden && settings.HideBurriedMonsters)
        {
            Show = () => !entity.IsHidden && entity.IsAlive;
        }

        ID = entity.Id;

        if (!_HasIngameIcon) MainTexture = new HudTexture("Icons.png");

        switch (Rarity)
        {
            case MonsterRarity.White:
                MainTexture.Size = settings.SizeEntityWhiteIcon;
                break;
            case MonsterRarity.Magic:
                MainTexture.Size = settings.SizeEntityMagicIcon;
                break;
            case MonsterRarity.Rare:
                MainTexture.Size = settings.SizeEntityRareIcon;
                break;
            case MonsterRarity.Unique:
            default:
                // Unknown/out-of-range rarity reads fall back to unique sizing rather than throwing,
                // matching the graceful default in BaseIcon. (Original threw here.)
                MainTexture.Size = settings.SizeEntityUniqueIcon;
                break;
        }

        if (!entity.IsHostile)
        {
            if (!_HasIngameIcon)
            {
                MainTexture.UV = SpriteHelper.GetUV(MapIconsIndex.LootFilterSmallGreenCircle);
                Priority = IconPriority.Low;
                Show = () => !settings.HideMinions && entity.IsAlive;
            }
        }
        else if (Rarity == MonsterRarity.Unique && entity.Path.Contains("Metadata/Monsters/Spirit/"))
        {
            MainTexture.UV = SpriteHelper.GetUV(MapIconsIndex.LootFilterLargeGreenHexagon);
        }
        else
        {
            string modName = null;

            if (entity.HasComponent<ObjectMagicProperties>())
            {
                var objectMagicProperties = entity.GetComponent<ObjectMagicProperties>();
                var mods = objectMagicProperties?.Mods;

                if (mods != null)
                {
                    if (mods.Contains("MonsterConvertsOnDeath_")) Show = () => entity.IsAlive && entity.IsHostile;

                    modName = mods.FirstOrDefault(modIcons.ContainsKey);
                }
            }

            if (modName != null)
            {
                MainTexture = new HudTexture("sprites.png");
                MainTexture.UV = SpriteHelper.GetUV(modIcons[modName], new Size2F(7, 8));
                Priority = IconPriority.VeryHigh;
            }
            else
            {
                switch (Rarity)
                {
                    case MonsterRarity.White:
                        MainTexture.UV = SpriteHelper.GetUV(MapIconsIndex.LootFilterLargeRedCircle);
                        if (settings.ShowWhiteMonsterName)
                            Text = RenderName?.Split(',').FirstOrDefault();
                        break;
                    case MonsterRarity.Magic:
                        MainTexture.UV = SpriteHelper.GetUV(MapIconsIndex.LootFilterLargeBlueCircle);
                        if (settings.ShowMagicMonsterName)
                            Text = RenderName?.Split(',').FirstOrDefault();
                        break;
                    case MonsterRarity.Rare:
                        MainTexture.UV = SpriteHelper.GetUV(MapIconsIndex.LootFilterLargeYellowCircle);
                        if (settings.ShowRareMonsterName)
                            Text = RenderName?.Split(',').FirstOrDefault();
                        break;
                    case MonsterRarity.Unique:
                    default:
                        // Original used LootFilterLargeWhiteHexagon (absent in this fork); use the
                        // yellow hexagon tinted dark orange instead. See README. Unknown rarity also
                        // lands here rather than throwing.
                        MainTexture.UV = SpriteHelper.GetUV(MapIconsIndex.LootFilterLargeYellowHexagon);
                        MainTexture.Color = Color.DarkOrange;
                        if (settings.ShowUniqueMonsterName)
                            Text = RenderName?.Split(',').FirstOrDefault();
                        break;
                }
            }
        }
    }
}
