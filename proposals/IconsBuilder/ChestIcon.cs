// EXPERIMENTAL candidate ported from exApiTools/IconsBuilder — see proposals/IconsBuilder/README.md. Not part of the build.

using System;
using System.Linq;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Abstract;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ExileCore.Shared.Static;
using SharpDX;

namespace ExileCore.IconsBuilder;

/// <summary>
/// Icon for chests/strongboxes/league containers. Classifies the entity into a <see cref="ChestType"/>
/// from its path/league, then picks a sprite, size, colour and label for that type.
/// </summary>
/// <remarks>
/// The original library also handled Heist, Expedition and Sanctum chests. Those <c>ChestType</c>
/// values do not exist in this fork's <see cref="ChestType"/> enum, so those branches are omitted
/// (see README). Unclassified chests fall back to <see cref="ChestType.SmallChest"/>.
/// </remarks>
public class ChestIcon : BaseIcon
{
    /// <summary>Builds a chest icon for the given entity.</summary>
    public ChestIcon(Entity entity, GameController gameController, IconsBuilderSettings settings) : base(entity, settings)
    {
        Update(entity, settings);
    }

    /// <summary>The classified chest type.</summary>
    public ChestType CType { get; private set; }

    private void Update(Entity entity, IconsBuilderSettings settings)
    {
        if (Entity.Path.Contains("BreachChest"))
            CType = ChestType.Breach;
        else if (Entity.Path.Contains("Metadata/Chests/AbyssChest") ||
                 Entity.Path.Contains("Metadata/MiscellaneousObjects/Abyss/AbyssFinal") ||
                 Entity.Path.Contains("Metadata/Chests/Abyss/AbyssFinal"))
            CType = ChestType.Abyss;
        else if (Entity.Path.Contains("Metadata/Chests/Incursion"))
            CType = ChestType.Incursion;
        else if (Entity.Path.Contains("Fossil"))
            CType = ChestType.Fossil;
        else if (Entity.Path.Contains("Metadata/Chests/Delve"))
            CType = ChestType.Delve;
        else if (Entity.Path.Contains("Perandus"))
            CType = ChestType.Perandus;
        else if (Entity.Path.Contains("Metadata/Chests/StrongBoxes"))
            CType = ChestType.Strongbox;
        else if (Entity.Path.Contains("Metadata/Chests/Labyrinth/Labyrinth"))
            CType = ChestType.Labyrinth;
        else if (Entity.Path.Contains("Metadata/Chests/SynthesisChests/Synthesis"))
            CType = ChestType.Synthesis;
        else if (Entity.League == LeagueType.Legion)
            CType = ChestType.Legion;
        else
            CType = ChestType.SmallChest;

        Show = () => !Entity.IsOpened;

        if (!_HasIngameIcon)
        {
            MainTexture = new HudTexture { FileName = "sprites.png" };
        }
        else
        {
            MainTexture.Size = settings.SizeChestIcon;
            Text = Entity.GetComponent<Render>()?.Name;
            return;
        }

        MainTexture.Color = Rarity switch
        {
            MonsterRarity.White => Color.White,
            MonsterRarity.Magic => HudSkin.MagicColor,
            MonsterRarity.Rare => HudSkin.RareColor,
            MonsterRarity.Unique => HudSkin.UniqueColor,
            _ => Color.Purple
        };

        switch (CType)
        {
            case ChestType.Breach:
                MainTexture.Size = settings.SizeBreachChestIcon;
                if (Entity.Path.Contains("Large"))
                {
                    MainTexture.UV = SpriteHelper.GetUV(strongboxesUV["Metadata/Chests/StrongBoxes/StrongboxDivination"], new Size2F(7, 8));
                    MainTexture.Color = new ColorBGRA(240, 100, 255, 255);
                    Text = "Big Breach";
                }
                else
                {
                    MainTexture.UV = SpriteHelper.GetUV(strongboxesUV["Metadata/Chests/StrongBoxes/Large"], new Size2F(7, 8));
                    MainTexture.Color = new ColorBGRA(240, 100, 255, 255);
                    Text = "Breach chest";
                }

                break;
            case ChestType.Abyss:
                MainTexture.Size = settings.SizeChestIcon;
                if (Entity.Path.Contains("AbyssFinalChest"))
                {
                    MainTexture.UV = SpriteHelper.GetUV(strongboxesUV["Metadata/Chests/AbyssChest"], new Size2F(7, 8));
                    MainTexture.Color = Color.GreenYellow;
                    Text = Entity.GetComponent<Render>()?.Name;
                }
                else if (Entity.Path.Contains("AbyssChest"))
                {
                    MainTexture.UV = SpriteHelper.GetUV(strongboxesUV["Metadata/Chests/StrongBoxes/StrongboxDivination"], new Size2F(7, 8));
                    MainTexture.Color = Color.GreenYellow;
                }

                break;
            case ChestType.Incursion:
                MainTexture.Size = settings.SizeChestIcon;
                MainTexture.UV = SpriteHelper.GetUV(strongboxesUV["Metadata/Chests/StrongBoxes/StrongboxDivination"], new Size2F(7, 8));
                MainTexture.Color = Color.OrangeRed;
                Text = Entity.Path.Replace("Metadata/Chests/IncursionChest", "").Replace("s/IncursionChest", "");
                break;
            case ChestType.Delve:
                Priority = IconPriority.High;
                MainTexture.Size = settings.SizeChestIcon;
                MainTexture.Color = Color.GreenYellow;
                Text = Entity.Path.Replace("Metadata/Chests/DelveChests/", "");
                if (Text.EndsWith("NoDrops")) Text = "";

                if (PathCheck(entity, "Metadata/Chests/DelveChests/OffPathCurrency", "Metadata/Chests/DelveChests/PathCurrency"))
                {
                    MainTexture.UV = SpriteHelper.GetUV(MyMapIconsIndex.Arcanist);
                    MainTexture.Color = Color.Orange;
                    Text = "Currency";
                }
                else if (PathCheck(entity, "Metadata/Chests/DelveChests/OffPathTrinkets", "Metadata/Chests/DelveChests/PathTrinkets"))
                {
                    MainTexture.UV = SpriteHelper.GetUV(MyMapIconsIndex.Jeweller);
                    Text = "Jew";
                }
                else if (PathCheck(entity, "Metadata/Chests/DelveChests/OffPathArmour", "Metadata/Chests/DelveChests/PathArmour"))
                {
                    MainTexture.UV = SpriteHelper.GetUV(MyMapIconsIndex.Armoury);
                    Text = "Armour";
                }
                else if (PathCheck(entity, "Metadata/Chests/DelveChests/OffPathWeapon", "Metadata/Chests/DelveChests/PathWeapon"))
                {
                    MainTexture.UV = SpriteHelper.GetUV(MyMapIconsIndex.Artisan);
                    Text = "Weapon";
                }
                else if (PathCheck(entity, "Metadata/Chests/DelveChests/PathGeneric", "Metadata/Chests/DelveChests/OffPathGeneric"))
                {
                    MainTexture.UV = SpriteHelper.GetUV(MyMapIconsIndex.Strongbox);
                    Text = "Generic";
                }
                else if (Entity.Path.Contains("DelveChestGem"))
                {
                    MainTexture.UV = SpriteHelper.GetUV(MyMapIconsIndex.Gemcutter);
                }
                else if (Entity.Path.Contains("Resonator"))
                {
                    if (Entity.Path.EndsWith("3")) Priority = IconPriority.Critical;
                    MainTexture.UV = SpriteHelper.GetUV(MyMapIconsIndex.Divination);
                    MainTexture.Color = Color.Orange;
                }
                else
                {
                    MainTexture.UV = SpriteHelper.GetUV(strongboxesUV["Metadata/Chests/StrongBoxes/Large"], new Size2F(7, 8));
                }

                break;
            case ChestType.Strongbox:
                MainTexture.Size = settings.SizeChestIcon;
                if (strongboxesUV.TryGetValue(Entity.Path, out var result))
                    MainTexture.UV = SpriteHelper.GetUV(result, new Size2F(7, 8));
                else
                    MainTexture.UV = SpriteHelper.GetUV(MyMapIconsIndex.Strongbox);

                Text = Entity.GetComponent<Render>()?.Name;
                break;
            case ChestType.SmallChest:
                MainTexture.Size = settings.SizeSmallChestIcon;
                if (Entity.Path.Contains("VaultTreasurePile"))
                {
                    MainTexture.UV = SpriteHelper.GetUV(strongboxesUV["Metadata/Chests/StrongBoxes/Arcanist"], new Size2F(7, 8));
                    MainTexture.Color = Color.Yellow;
                }
                else if (Entity.Path.Contains("SideArea/SideAreaChest"))
                {
                    MainTexture.FileName = "Icons.png";
                    MainTexture.UV = SpriteHelper.GetUV(new Size2(4, 6), Constants.MapIconsSize);
                }
                else
                {
                    MainTexture.FileName = "Icons.png";
                    MainTexture.UV = SpriteHelper.GetUV(MapIconsIndex.LootFilterSmallCyanSquare);
                    Show = () => Entity.IsValid && settings.ShowSmallChest && !Entity.IsOpened;
                }

                break;
            case ChestType.Fossil:
                Priority = IconPriority.Critical;
                MainTexture.Size = settings.SizeChestIcon;
                MainTexture.UV = SpriteHelper.GetUV(MyMapIconsIndex.Divination);
                var fossilName = Entity.GetComponent<Render>()?.Name ?? "";
                Text = fossilName.Contains(' ') ? fossilName.Substring(0, fossilName.IndexOf(' ')) : fossilName;
                MainTexture.Color = Color.Pink;
                if (FossilRarity.TryGetValue(Text, out var clr)) MainTexture.Color = clr;
                break;
            case ChestType.Perandus:
                MainTexture.Size = settings.SizeChestIcon;
                MainTexture.UV = SpriteHelper.GetUV(strongboxesUV["Metadata/Chests/StrongBoxes/Large"], new Size2F(7, 8));
                MainTexture.Color = Color.LightGreen;
                Text = Entity.GetComponent<Render>()?.Name;
                break;
            case ChestType.Labyrinth:
                MainTexture.Size = settings.SizeChestIcon;
                MainTexture.UV = SpriteHelper.GetUV(MyMapIconsIndex.Divination);
                Text = Entity.Path.Replace("Metadata/Chests/Labyrinth/Labyrinth", "");
                MainTexture.Color = Color.ForestGreen;
                break;
            case ChestType.Synthesis:
                Priority = IconPriority.Critical;
                MainTexture.Size = settings.SizeChestIcon;
                MainTexture.UV = SpriteHelper.GetUV(MyMapIconsIndex.Divination);
                Text = Entity.Path.Split('/').Last();
                MainTexture.Color = Color.Aquamarine;
                break;
            case ChestType.Legion:
                MainTexture.FileName = "Icons.png";
                Priority = IconPriority.Critical;
                MainTexture.Size = settings.SizeChestIcon;
                if (Entity.GetComponent<Stats>()?.StatDictionary.TryGetValue(GameStat.MonsterMinimapIcon, out var minimapIconIndex) ?? false)
                {
                    MainTexture.UV = SpriteHelper.GetUV((MapIconsIndex)minimapIconIndex);
                    Text = ((MapIconsIndex)minimapIconIndex).ToString().Replace("Legion", "");
                }
                else
                {
                    MainTexture.UV = SpriteHelper.GetUV(MyMapIconsIndex.Divination);
                }

                MainTexture.Color = Color.White;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(CType), CType, "Chest type not found.");
        }

        if (settings.LogDebugInformation && Show())
        {
            Logger.Log.Information(
                $"Chest debug -> CType:{CType} Path: {Entity.Path} #\t\tText: {Text} #\t\tRender Name: {Entity.GetComponent<Render>()?.Name}");
        }
    }
}
