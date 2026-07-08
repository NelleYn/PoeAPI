// EXPERIMENTAL candidate ported from exApiTools/ItemFilter — see proposals/ItemFilterLibrary/README.md. Not part of the build.

using System.Collections.Generic;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;

namespace ExileCore.ItemFilterLibrary;

/// <summary>
/// Nested projections referenced by <see cref="ItemData"/>. Ported from
/// <c>exApiTools/ItemFilter/ItemData.Nested.cs</c>. Every field is populated only from members that
/// exist on this fork's Core (see README). <see cref="MapData"/>, <see cref="ChargeData"/>,
/// <see cref="AttributeRequirementsData"/>, <see cref="SkillGemData"/>, <see cref="WeaponData"/> and
/// <see cref="ArmourData"/> are backed by this fork's
/// <c>Map</c>/<c>Charges</c>/<c>AttributeRequirements</c>/<c>SkillGem</c>/<c>Weapon</c>/<c>Armour</c>
/// components and are populated only when the underlying entity carries that component (otherwise the corresponding
/// <c>ItemData</c> property is <c>null</c> — guard with <c>!= null</c> in rules). Upstream IFL
/// additionally exposes per-category mod lists (<c>ExplicitMods</c>/<c>ImplicitMods</c>/
/// <c>EnchantedMods</c>/…) and influence/price/scourge data; those are still omitted here because the
/// backing members are absent from this fork (see README).
/// </summary>
public partial class ItemData
{
    /// <summary>Mod-related projection. This fork's <c>Mods</c> exposes only the combined
    /// <see cref="ItemMods"/> list (implicit + explicit), so no per-affix-category lists are provided.</summary>
    public class ModsData
    {
        public ModsData(
            List<ItemMod> itemMods,
            ItemRarity itemRarity,
            bool identified,
            int itemLevel,
            int requiredLevel,
            string uniqueName,
            int countFractured,
            bool synthesised,
            bool isMirrored)
        {
            ItemMods = itemMods ?? new List<ItemMod>();
            ItemRarity = itemRarity;
            Identified = identified;
            ItemLevel = itemLevel;
            RequiredLevel = requiredLevel;
            UniqueName = uniqueName ?? string.Empty;
            CountFractured = countFractured;
            Synthesised = synthesised;
            IsMirrored = isMirrored;
        }

        /// <summary>All mods (implicit + explicit) — each <see cref="ItemMod"/> exposes <c>Name</c>/<c>DisplayName</c>.</summary>
        public List<ItemMod> ItemMods { get; }
        public ItemRarity ItemRarity { get; }
        public bool Identified { get; }
        public int ItemLevel { get; }
        public int RequiredLevel { get; }
        public string UniqueName { get; }
        public int CountFractured { get; }
        public bool Synthesised { get; }
        public bool IsMirrored { get; }
    }

    /// <summary>Socket / link projection.</summary>
    public class SocketData
    {
        public SocketData(
            int largestLinkSize,
            int numberOfSockets,
            List<int[]> links,
            List<string> socketGroups,
            List<string> socketedGems)
        {
            LargestLinkSize = largestLinkSize;
            NumberOfSockets = numberOfSockets;
            Links = links ?? new List<int[]>();
            SocketGroups = socketGroups ?? new List<string>();
            SocketedGems = socketedGems ?? new List<string>();
        }

        public int LargestLinkSize { get; }

        /// <summary>Number of sockets. Exposed under both names; <c>SocketNumber</c> matches upstream IFL rules.</summary>
        public int NumberOfSockets { get; }
        public int SocketNumber => NumberOfSockets;

        /// <summary>Each entry is a linked group as socket-colour codes (1..6) — from <c>Sockets.Links</c>.</summary>
        public List<int[]> Links { get; }

        /// <summary>Colour-letter groups per link ("RGB", "RRW", …) — from <c>Sockets.SocketGroup</c>.</summary>
        public List<string> SocketGroups { get; }

        /// <summary>Metadata paths of socketed gems — from <c>Sockets.SocketedGems[].GemEntity.Path</c>.</summary>
        public List<string> SocketedGems { get; }
    }

    /// <summary>Stack-size projection.</summary>
    public class StackData
    {
        public StackData(int size, int maxStackSize)
        {
            Size = size;
            MaxStackSize = maxStackSize;
        }

        /// <summary>Current stack size — from <c>Stack.Size</c>. Exposed as <see cref="Count"/> too (upstream IFL name).</summary>
        public int Size { get; }
        public int Count => Size;

        /// <summary>Maximum stack size — from <c>Stack.Info.MaxStackSize</c> (<c>CurrencyInfo.MaxStackSize</c>).</summary>
        public int MaxStackSize { get; }
    }

    /// <summary>Map-item projection — populated only when the entity has a <c>Map</c> component
    /// (see <c>Core/PoEMemory/Components/Map.cs</c>).</summary>
    public class MapData
    {
        public MapData(int tier, string areaId, string areaName, int areaLevel, InventoryTabMapSeries mapSeries)
        {
            Tier = tier;
            AreaId = areaId ?? string.Empty;
            AreaName = areaName ?? string.Empty;
            AreaLevel = areaLevel;
            MapSeries = mapSeries;
        }

        /// <summary>Map tier — from <c>Map.Tier</c>.</summary>
        public int Tier { get; }

        /// <summary>Metadata id of the map's <c>WorldArea</c> — from <c>Map.Area.Id</c>.</summary>
        public string AreaId { get; }

        /// <summary>Display name of the map's <c>WorldArea</c> — from <c>Map.Area.Name</c>.</summary>
        public string AreaName { get; }

        /// <summary>Monster level of the map's <c>WorldArea</c> — from <c>Map.Area.AreaLevel</c>.</summary>
        public int AreaLevel { get; }

        /// <summary>Map series (Atlas season) — from <c>Map.MapSeries</c>.</summary>
        public InventoryTabMapSeries MapSeries { get; }
    }

    /// <summary>Charge-item projection (flasks and other charge-using items) — populated only when the
    /// entity has a <c>Charges</c> component (see <c>Core/PoEMemory/Components/Charges.cs</c>).</summary>
    public class ChargeData
    {
        public ChargeData(int numCharges, int chargesPerUse, int chargesMax)
        {
            NumCharges = numCharges;
            ChargesPerUse = chargesPerUse;
            ChargesMax = chargesMax;
        }

        /// <summary>Current number of charges — from <c>Charges.NumCharges</c>.</summary>
        public int NumCharges { get; }

        /// <summary>Charges consumed per use — from <c>Charges.ChargesPerUse</c>.</summary>
        public int ChargesPerUse { get; }

        /// <summary>Maximum charges the item can hold — from <c>Charges.ChargesMax</c>.</summary>
        public int ChargesMax { get; }
    }

    /// <summary>Attribute-requirement projection — populated only when the entity has an
    /// <c>AttributeRequirements</c> component (see
    /// <c>Core/PoEMemory/Components/AttributeRequirements.cs</c>).</summary>
    public class AttributeRequirementsData
    {
        public AttributeRequirementsData(int strength, int dexterity, int intelligence)
        {
            Strength = strength;
            Dexterity = dexterity;
            Intelligence = intelligence;
        }

        /// <summary>Required strength — from <c>AttributeRequirements.strength</c>.</summary>
        public int Strength { get; }

        /// <summary>Required dexterity — from <c>AttributeRequirements.dexterity</c>.</summary>
        public int Dexterity { get; }

        /// <summary>Required intelligence — from <c>AttributeRequirements.intelligence</c>.</summary>
        public int Intelligence { get; }
    }

    /// <summary>Skill-gem projection — populated only when the entity has a <c>SkillGem</c> component
    /// (see <c>Core/PoEMemory/Components/SkillGem.cs</c>).</summary>
    public class SkillGemData
    {
        public SkillGemData(int level, int maxLevel, uint totalExpGained, uint experienceMaxLevel, uint experienceToNextLevel, int socketColor)
        {
            Level = level;
            MaxLevel = maxLevel;
            TotalExpGained = totalExpGained;
            ExperienceMaxLevel = experienceMaxLevel;
            ExperienceToNextLevel = experienceToNextLevel;
            SocketColor = socketColor;
        }

        /// <summary>Current gem level — from <c>SkillGem.Level</c>.</summary>
        public int Level { get; }

        /// <summary>Maximum level the gem can reach — from <c>SkillGem.MaxLevel</c>.</summary>
        public int MaxLevel { get; }

        /// <summary>Total experience gained — from <c>SkillGem.TotalExpGained</c>.</summary>
        public uint TotalExpGained { get; }

        /// <summary>Experience required to reach the maximum level — from <c>SkillGem.ExperienceMaxLevel</c>.</summary>
        public uint ExperienceMaxLevel { get; }

        /// <summary>Experience remaining until the next level — from <c>SkillGem.ExperienceToNextLevel</c>.</summary>
        public uint ExperienceToNextLevel { get; }

        /// <summary>Socket colour required by the gem — from <c>SkillGem.SocketColor</c>.</summary>
        public int SocketColor { get; }
    }

    /// <summary>Weapon stat projection — populated only when the entity has a <c>Weapon</c> component
    /// (see <c>Core/PoEMemory/Components/Weapon.cs</c>).</summary>
    public class WeaponData
    {
        public WeaponData(int damageMin, int damageMax, int attackTime, int critChance)
        {
            DamageMin = damageMin;
            DamageMax = damageMax;
            AttackTime = attackTime;
            CritChance = critChance;
        }

        /// <summary>Minimum physical damage — from <c>Weapon.DamageMin</c>.</summary>
        public int DamageMin { get; }

        /// <summary>Maximum physical damage — from <c>Weapon.DamageMax</c>.</summary>
        public int DamageMax { get; }

        /// <summary>Attack time in milliseconds — from <c>Weapon.AttackTime</c>.</summary>
        public int AttackTime { get; }

        /// <summary>Critical strike chance — from <c>Weapon.CritChance</c>.</summary>
        public int CritChance { get; }
    }

    /// <summary>Armour/defence stat projection — populated only when the entity has an <c>Armour</c>
    /// component (see <c>Core/PoEMemory/Components/Armour.cs</c>).</summary>
    public class ArmourData
    {
        public ArmourData(int armourScore, int evasionScore, int energyShieldScore)
        {
            ArmourScore = armourScore;
            EvasionScore = evasionScore;
            EnergyShieldScore = energyShieldScore;
        }

        /// <summary>Armour rating — from <c>Armour.ArmourScore</c>.</summary>
        public int ArmourScore { get; }

        /// <summary>Evasion rating — from <c>Armour.EvasionScore</c>.</summary>
        public int EvasionScore { get; }

        /// <summary>Energy shield rating — from <c>Armour.EnergyShieldScore</c>.</summary>
        public int EnergyShieldScore { get; }
    }
}
