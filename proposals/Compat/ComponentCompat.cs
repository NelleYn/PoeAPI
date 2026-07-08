// EXPERIMENTAL candidate — see proposals/Compat/README.md. Not part of the build.

using System.Collections.Generic;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;

namespace ExileCore.Shared.Compat;

/// <summary>
/// Small per-component helpers that bridge fork member names to the shapes ExileApi-Compiled
/// plugins expect: <c>Stack.MaxSize</c>, the 2-argument <c>SoundController.PlaySound(file, volume)</c>,
/// an upstream-style <c>Life.GetBuffs()</c> accessor, and the per-affix-category <c>Mods.ImplicitMods</c>
/// / <c>Mods.ExplicitMods</c> accessors.
/// </summary>
public static class ComponentCompat
{
    /// <summary>
    /// Emulates upstream <c>Stack.MaxSize</c>: the maximum stack size for a stackable item.
    /// </summary>
    /// <param name="stack">The stack component.</param>
    /// <returns>
    /// The maximum stack size, or <c>0</c> when the currency info is unavailable. The fork has no
    /// <c>Stack.MaxSize</c>; it is read from <c>Stack.Info.MaxStackSize</c> (compatibility doc,
    /// "Components — items"). Builds on <c>Stack.Info</c> (<c>Stack.cs:12</c>) and
    /// <c>CurrencyInfo.MaxStackSize</c> (<c>CurrencyInfo.cs:9</c>).
    /// </returns>
    public static int MaxSize(this Stack stack)
    {
        return stack?.Info?.MaxStackSize ?? 0;
    }

    /// <summary>
    /// Emulates the upstream 2-argument <c>SoundController.PlaySound(string file, float volume)</c> overload.
    /// </summary>
    /// <param name="soundController">The sound controller.</param>
    /// <param name="name">The sound name (or file) to play.</param>
    /// <param name="volume">The master output volume (0..1) to apply before playback.</param>
    /// <remarks>
    /// The fork ships only the 1-argument <c>PlaySound(string)</c> (<c>Core/SoundController.cs:71</c>)
    /// and a separate <c>SetVolume(float)</c> (<c>Core/SoundController.cs:138</c>)
    /// (compatibility doc, "Graphics, fonts &amp; sound"). This helper composes the two:
    /// it sets the master volume, then plays. Note the volume is the <i>master</i> volume and persists
    /// for subsequent playbacks until changed again — matching how the fork's mastering voice behaves.
    /// </remarks>
    public static void PlaySound(this SoundController soundController, string name, float volume)
    {
        if (soundController == null)
            return;

        soundController.SetVolume(volume);
        soundController.PlaySound(name);
    }

    /// <summary>
    /// Emulates an upstream-style <c>GetBuffs()</c> accessor over the fork's <c>Life.Buffs</c> list.
    /// </summary>
    /// <param name="life">The life component.</param>
    /// <returns>
    /// The active buffs, or an empty list when unavailable. Builds on <c>Life.Buffs</c>
    /// (<c>Life.cs:79</c>). In ExileApi-Compiled buffs are exposed via a dedicated <c>Buffs</c>
    /// component; this fork reads them off <c>Life</c> / <c>Entity.Buffs</c> (compatibility doc,
    /// "Components — combat &amp; character").
    /// </returns>
    public static IReadOnlyList<Buff> GetBuffs(this Life life)
    {
        return life?.Buffs ?? new List<Buff>();
    }

    /// <summary>
    /// Null-safe buff lookup mirroring upstream buff queries. Named <c>HasBuffSafe</c> rather than
    /// <c>HasBuff</c> so it does not shadow the existing instance method <c>Life.HasBuff(string)</c>
    /// (<c>Life.cs:122</c>), which already handles a non-null receiver.
    /// </summary>
    /// <param name="life">The life component (may be <c>null</c>).</param>
    /// <param name="name">The buff name to search for.</param>
    /// <returns>
    /// <c>true</c> if a matching buff is present; otherwise <c>false</c>. Builds on
    /// <c>Life.HasBuff(string)</c> (<c>Life.cs:122</c>).
    /// </returns>
    public static bool HasBuffSafe(this Life life, string name)
    {
        return life?.HasBuff(name) ?? false;
    }

    /// <summary>
    /// Emulates upstream <c>Mods.ImplicitMods</c>: the item's implicit modifiers only (as opposed
    /// to the fork's combined <c>Mods.ItemMods</c>).
    /// </summary>
    /// <param name="mods">The mods component.</param>
    /// <returns>
    /// The implicit modifiers, or an empty list when <paramref name="mods"/> is <c>null</c>, has no
    /// address, or the range looks corrupt. The fork's public <c>Mods.ItemMods</c>
    /// (<c>Core/PoEMemory/Components/Mods.cs:47</c>) already builds this same list but concatenates
    /// it with the explicit mods via the *private* <c>Mods.GetMods(long,long)</c>
    /// (<c>Mods.cs:106-126</c>); this helper reproduces that exact walk (0x28-byte stride, capped at
    /// 12 entries) over just the implicit range, using the public
    /// <c>Mods.ModsStruct.implicitMods</c> field (<c>GameOffsets/ModsComponentOffsets.cs:13</c>) and
    /// the public <c>RemoteMemoryObject.GetObject&lt;T&gt;</c> (<c>Core/PoEMemory/RemoteMemoryObject.cs:84</c>).
    /// Compatibility doc, "Components — items".
    /// </returns>
    public static List<ItemMod> ImplicitMods(this Mods mods)
    {
        if (mods == null)
            return new List<ItemMod>();

        var range = mods.ModsStruct.implicitMods;
        return ParseModRange(mods, range.First, range.Last);
    }

    /// <summary>
    /// Emulates upstream <c>Mods.ExplicitMods</c>: the item's explicit modifiers only (as opposed
    /// to the fork's combined <c>Mods.ItemMods</c>).
    /// </summary>
    /// <param name="mods">The mods component.</param>
    /// <returns>
    /// The explicit modifiers, or an empty list when <paramref name="mods"/> is <c>null</c>, has no
    /// address, or the range looks corrupt. See <see cref="ImplicitMods"/> for how the range is
    /// walked; this uses <c>Mods.ModsStruct.explicitMods</c>
    /// (<c>GameOffsets/ModsComponentOffsets.cs:14</c>) instead. Compatibility doc, "Components — items".
    /// </returns>
    public static List<ItemMod> ExplicitMods(this Mods mods)
    {
        if (mods == null)
            return new List<ItemMod>();

        var range = mods.ModsStruct.explicitMods;
        return ParseModRange(mods, range.First, range.Last);
    }

    // Mirrors the fork's private Mods.GetMods(long,long) (Mods.cs:106-126): each ItemMod is
    // 0x28 bytes wide; bail out on a corrupt/oversized range exactly as the fork does (count > 12).
    private static List<ItemMod> ParseModRange(Mods mods, long begin, long end)
    {
        var list = new List<ItemMod>();

        if (mods.Address == 0)
            return list;

        var count = (end - begin) / 0x28;

        if (count > 12)
            return list;

        for (var i = begin; i < end; i += 0x28)
            list.Add(mods.GetObject<ItemMod>(i));

        return list;
    }
}
