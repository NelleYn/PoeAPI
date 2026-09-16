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
    // Built by a constructor that only fills a dictionary of delegates — no memory is read and no
    // game state is needed — so a static field initializer is safe here. It replaces a lazy
    // `if (translate == null) translate = new StatTranslator()` that ran on every plugin thread
    // that touched an item, with nothing ordering the write against the reads.
    private static readonly StatTranslator translate = new StatTranslator();

    private readonly Entity item;
    private readonly float[] stats;

    /// <summary>Computes the statistics for the given item entity.</summary>
    /// <param name="item">The item entity to analyze.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is null.</exception>
    /// <remarks>
    /// EVERY PARSE STEP GUARDS ITSELF, and that is the change. The component reads below became
    /// REACHABLE on 2026-09-16: until the item components entered the vtable table nothing here
    /// resolved and the null dereferences could not fire, so they had never been seen. Each step now
    /// asks for the component it is about to use and returns if it is not there, instead of being
    /// gated on a different component (ParseWeaponStats read Quality under a Weapon gate) or on no
    /// gate at all (ParseExplicitMods dereferenced Mods unconditionally).
    /// </remarks>
    public ItemStats(Entity item)
    {
        this.item = item ?? throw new ArgumentNullException(nameof(item));
        stats = new float[Enum.GetValues(typeof(ItemStatEnum)).Length];
        ParseSockets();
        ParseExplicitMods();
        ParseWeaponStats();
    }

    private void ParseWeaponStats()
    {
        // GUARDED BY THE COMPONENT IT USES, not by a HasComponent<Weapon>() call at the call site.
        // The two go through CacheComp separately and an address change between them empties it, so
        // a true answer from HasComponent is not a promise that GetComponent returns anything —
        // Entity's own hidden-check cache carries that same note for the same reason.
        var component = item.GetComponent<Weapon>();

        if (component == null) return;

        // Quality IS A DIFFERENT COMPONENT AND IS NOT IMPLIED BY Weapon. This read used to sit under
        // the Weapon gate and dereference whatever GetComponent<Quality>() returned; an item without
        // a Quality component contributes no quality, which is what the 0 means.
        var quality = item.GetComponent<Quality>()?.ItemQuality ?? 0;

        var num = (component.DamageMin + component.DamageMax) / 2f + GetStat(ItemStatEnum.LocalPhysicalDamage);
        num *= 1f + (GetStat(ItemStatEnum.LocalPhysicalDamagePercent) + quality) / 100f;
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
        // NULL IS A NORMAL ANSWER, not an exceptional one. Of the item components measured on
        // 2026-09-16 only Base and RenderItem were recorded as present on every dropped item; Mods
        // carries no such note, so nothing here may assume it. GetComponent also returns null for
        // any entity whose component map could not be read and for one inside its scan cooldown.
        // This ran unconditionally from the constructor.
        var mods = item.GetComponent<Mods>();

        if (mods == null) return;

        foreach (var current in mods.ItemMods)
        {
            translate.Translate(this, current);
        }

        // Skipped along with the loop on purpose, and it changes no result: with no mods read every
        // term summed below is zero, so both accumulations would add zero to zero.
        AddToMod(ItemStatEnum.ElementalResistance,
            GetStat(ItemStatEnum.LightningResistance) + GetStat(ItemStatEnum.FireResistance) +
            GetStat(ItemStatEnum.ColdResistance));

        AddToMod(ItemStatEnum.TotalResistance, GetStat(ItemStatEnum.ElementalResistance) + GetStat(ItemStatEnum.TotalResistance));
    }

    private void ParseSockets()
    {
        // One lookup where there used to be two. HasComponent followed by GetComponent reads
        // CacheComp twice, and the second read can come back empty even after the first said yes.
        var component = item.GetComponent<Sockets>();

        if (component == null) return;

        AddToMod(ItemStatEnum.Sockets, component.NumberOfSockets);
        AddToMod(ItemStatEnum.LinkedSockets, component.LargestLinkSize);
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
}
