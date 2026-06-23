// EXPERIMENTAL candidate ported from exApiTools/IconsBuilder — see proposals/IconsBuilder/README.md. Not part of the build.

using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Abstract;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using SharpDX;

namespace ExileCore.IconsBuilder;

/// <summary>
/// Icon for Legion (frozen-in-time) monsters and Legion monster-chests. Reads the
/// <see cref="GameStat.MonsterMinimapIcon"/> stat to pick the matching game sprite and only shows
/// the icon while the monster is frozen-in-time and alive.
/// </summary>
public class LegionIcon : BaseIcon
{
    /// <summary>Builds a Legion icon for the given entity.</summary>
    public LegionIcon(Entity entity, GameController gameController, IconsBuilderSettings settings, Dictionary<string, Size2> modIcons)
        : base(entity, settings)
    {
        Update(entity, settings, modIcons);
    }

    /// <inheritdoc/>
    public override string ToString() => $"{Entity.Path} : {Entity.Type} ({Entity.Address}) T: {Text}";

    private void Update(Entity entity, IconsBuilderSettings settings, Dictionary<string, Size2> modIcons)
    {
        MainTexture = new HudTexture("Icons.png");

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
                // Unknown rarity falls back to unique sizing rather than throwing. (Original threw.)
                MainTexture.Size = settings.SizeEntityUniqueIcon;
                Text = entity.RenderName;
                break;
        }

        if (entity.Path.StartsWith("Metadata/Monsters/LegionLeague/MonsterChest", StringComparison.Ordinal) || Rarity == MonsterRarity.Unique)
        {
            MainTexture.UV = SpriteHelper.GetUV(MapIconsIndex.LootFilterLargeGreenSquare);
            MainTexture.Color = Color.LimeGreen;
            Hidden = () => false;
            Text = entity.RenderName;

            Show = () =>
            {
                if (Entity.IsValid)
                    return Entity.GetComponent<Life>()?.HPPercentage > 0.02;

                return Entity.IsAlive;
            };

            return;
        }

        string modName = null;

        if (entity.HasComponent<ObjectMagicProperties>())
        {
            var objectMagicProperties = entity.GetComponent<ObjectMagicProperties>();
            if (objectMagicProperties == null) return;

            var mods = objectMagicProperties.Mods;
            if (mods == null) return;

            if (mods.Contains("MonsterConvertsOnDeath_")) Show = () => entity.IsValid && entity.IsAlive && entity.IsHostile;

            modName = mods.FirstOrDefault(modIcons.ContainsKey);

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
                        break;
                    case MonsterRarity.Magic:
                        MainTexture.UV = SpriteHelper.GetUV(MapIconsIndex.LootFilterLargeBlueCircle);
                        break;
                    case MonsterRarity.Rare:
                        MainTexture.UV = SpriteHelper.GetUV(MapIconsIndex.LootFilterLargeYellowCircle);
                        break;
                    case MonsterRarity.Unique:
                    default:
                        // Original used LootFilterLargeWhiteHexagon (absent in this fork). See README.
                        // Unknown rarity also lands here rather than throwing.
                        MainTexture.UV = SpriteHelper.GetUV(MapIconsIndex.LootFilterLargeYellowHexagon);
                        MainTexture.Color = Color.DarkOrange;
                        break;
                }
            }
        }

        var statDictionary = Entity.Stats;

        if (statDictionary.Count == 0)
        {
            statDictionary = entity.GetComponentFromMemory<Stats>()?.ParseStats();
            if (statDictionary == null || statDictionary.Count == 0) Text = "Error";
        }

        if (statDictionary != null && statDictionary.TryGetValue(GameStat.MonsterMinimapIcon, out var indexMinimapIcon))
        {
            var name = (MapIconsIndex)indexMinimapIcon;
            Text = name.ToString().Replace("Legion", "");
            Priority = IconPriority.Critical;

            var frozenCheck = new TimeCache<bool>(() =>
            {
                var stats = Entity.Stats;
                if (stats.Count == 0) return false;
                stats.TryGetValue(GameStat.FrozenInTime, out var frozenInTime);
                stats.TryGetValue(GameStat.MonsterHideMinimapIcon, out var hideMinimapIcon);
                return frozenInTime == 1 && hideMinimapIcon == 1 || frozenInTime == 0 && hideMinimapIcon == 0;
            }, 75);

            Show = () => Entity.IsAlive && frozenCheck.Value;
        }
        else
        {
            Show = () => !Hidden() && Entity.GetComponent<Life>()?.HPPercentage > 0.02;
        }
    }
}
