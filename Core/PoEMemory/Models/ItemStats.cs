using System;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;

namespace ExileCore.PoEMemory.Models;

/// <summary>
/// Aggregates the derived statistics of an item entity by parsing its sockets,
/// explicit mods and (for weapons) its base weapon stats into a single indexable table.
/// </summary>
public sealed class ItemStats
{
    private static StatTranslator translate;
    private readonly Entity item;
    private readonly float[] stats;

    /// <summary>Computes the statistics for the given item entity.</summary>
    /// <param name="item">The item entity to analyze.</param>
    public ItemStats(Entity item)
    {
        this.item = item;
        if (translate == null) translate = new StatTranslator();
        stats = new float[Enum.GetValues(typeof(ItemStatEnum)).Length];
        ParseSockets();
        ParseExplicitMods();
        if (item.HasComponent<Weapon>()) ParseWeaponStats();
    }

    private void ParseWeaponStats()
    {
        var component = item.GetComponent<Weapon>();
        var num = (component.DamageMin + component.DamageMax) / 2f + GetStat(ItemStatEnum.LocalPhysicalDamage);
        num *= 1f + (GetStat(ItemStatEnum.LocalPhysicalDamagePercent) + item.GetComponent<Quality>().ItemQuality) / 100f;
        AddToMod(ItemStatEnum.AveragePhysicalDamage, num);
        var num2 = 1f / (component.AttackTime / 1000f);
        num2 *= 1f + GetStat(ItemStatEnum.LocalAttackSpeed) / 100f;
        AddToMod(ItemStatEnum.AttackPerSecond, num2);
        var num3 = component.CritChance / 100f;
        num3 *= 1f + GetStat(ItemStatEnum.LocalCritChance) / 100f;
        AddToMod(ItemStatEnum.WeaponCritChance, num3);

        var num4 = GetStat(ItemStatEnum.LocalAddedColdDamage) + GetStat(ItemStatEnum.LocalAddedFireDamage) +
                   GetStat(ItemStatEnum.LocalAddedLightningDamage);

        AddToMod(ItemStatEnum.AverageElementalDamage, num4);
        AddToMod(ItemStatEnum.DPS, (num + num4) * num2);
        AddToMod(ItemStatEnum.PhysicalDPS, num * num2);
    }

    private void ParseExplicitMods()
    {
        foreach (var current in item.GetComponent<Mods>().ItemMods)
        {
            translate.Translate(this, current);
        }

        AddToMod(ItemStatEnum.ElementalResistance,
            GetStat(ItemStatEnum.LightningResistance) + GetStat(ItemStatEnum.FireResistance) +
            GetStat(ItemStatEnum.ColdResistance));

        AddToMod(ItemStatEnum.TotalResistance, GetStat(ItemStatEnum.ElementalResistance) + GetStat(ItemStatEnum.TotalResistance));
    }

    private void ParseSockets()
    {
        //todo ParseSockets do nothing
    }

    /// <summary>Adds <paramref name="value"/> to the accumulated total for the given <paramref name="stat"/>.</summary>
    /// <param name="stat">The statistic to accumulate into.</param>
    /// <param name="value">The amount to add.</param>
    public void AddToMod(ItemStatEnum stat, float value)
    {
        stats[(int) stat] += value;
    }

    /// <summary>Gets the accumulated value for the given <paramref name="stat"/>.</summary>
    /// <param name="stat">The statistic to read.</param>
    /// <returns>The current accumulated value.</returns>
    public float GetStat(ItemStatEnum stat)
    {
        return stats[(int) stat];
    }

    /// <summary>Obsolete. Always returns <see cref="ItemType.All"/>.</summary>
    [Obsolete]
    public ItemType GetSlot()
    {
        return ItemType.All;
    }
}
