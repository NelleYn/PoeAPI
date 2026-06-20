using System;
using System.Collections.Generic;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ExileCore.Shared.Interfaces;
using GameOffsets;

namespace ExileCore.PoEMemory.FilesInMemory;

/// <summary>
/// Reads the Mods.dat table, exposing each mod record indexed by key, by record address,
/// and grouped into tiers by mod group and affix type.
/// </summary>
public class ModsDat : FileInMemory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModsDat"/> class and loads all mod records.
    /// </summary>
    /// <param name="m">The memory accessor used to read the game process.</param>
    /// <param name="address">A delegate returning the table's base address.</param>
    /// <param name="sDat">The stats table used to resolve mod stat names.</param>
    /// <param name="tagsDat">The tags table used to resolve mod tags.</param>
    public ModsDat(IMemory m, Func<long> address, StatsDat sDat, TagsDat tagsDat) : base(m, address)
    {
        loadItems(sDat, tagsDat);
    }

    /// <summary>Mod records keyed by their (case-insensitive) key.</summary>
    public IDictionary<string, ModRecord> records { get; } = new Dictionary<string, ModRecord>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Mod records keyed by their record memory address.</summary>
    public IDictionary<long, ModRecord> DictionaryRecords { get; } = new Dictionary<long, ModRecord>();

    /// <summary>Mod records grouped by (group, affix type) and sorted by minimum level.</summary>
    public IDictionary<Tuple<string, ModType>, List<ModRecord>> recordsByTier { get; } =
        new Dictionary<Tuple<string, ModType>, List<ModRecord>>();

    /// <summary>Gets the mod record at the given record address, or <c>null</c> if not found.</summary>
    /// <param name="address">The record's memory address.</param>
    public ModRecord GetModByAddress(long address)
    {
        DictionaryRecords.TryGetValue(address, out var result);
        return result;
    }

    private void loadItems(StatsDat sDat, TagsDat tagsDat)
    {
        foreach (var addr in RecordAddresses())
        {
            var r = new ModRecord(M, sDat, tagsDat, addr);

            if (records.ContainsKey(r.Key))
                continue;

            DictionaryRecords.Add(addr, r);
            records.Add(r.Key, r);
            var addToItemIiers = r.Domain != ModDomain.Monster;
            if (!addToItemIiers) continue;
            var byTierKey = Tuple.Create(r.Group, r.AffixType);

            if (!recordsByTier.TryGetValue(byTierKey, out var groupMembers))
            {
                groupMembers = new List<ModRecord>();
                recordsByTier[byTierKey] = groupMembers;
            }

            groupMembers.Add(r);
        }

        foreach (var list in recordsByTier.Values)
        {
            list.Sort(ModRecord.ByLevelComparer);
        }
    }

    /// <summary>A single mod parsed from the Mods.dat table.</summary>
    public class ModRecord
    {
        /// <summary>The maximum number of stats a mod can have.</summary>
        public const int NumberOfStats = 4;

        /// <summary>Comparer that orders mod records by descending minimum level.</summary>
        public static IComparer<ModRecord> ByLevelComparer = new LevelComparer();

        // more fields can be added (see in visualGGPK)

        /// <summary>
        /// Reads a single mod record from memory at the given address.
        /// </summary>
        /// <param name="m">The memory accessor used to read the game process.</param>
        /// <param name="sDat">The stats table used to resolve mod stat names.</param>
        /// <param name="tagsDat">The tags table used to resolve mod tags.</param>
        /// <param name="addr">The record's memory address.</param>
        public ModRecord(IMemory m, StatsDat sDat, TagsDat tagsDat, long addr)
        {
            Address = addr;
            var ModsRecord = m.Read<ModsRecordOffsets>(addr);

            Key = RemoteMemoryObject.Cache.StringCache.Read($"{nameof(ModsDat)}{ModsRecord.Key.buf}",
                () => ModsRecord.Key.ToString(m));

            Unknown8 = ModsRecord.Unknown8;
            MinLevel = ModsRecord.MinLevel;

            var read = m.Read<long>(ModsRecord.TypeName);
            TypeName = RemoteMemoryObject.Cache.StringCache.Read($"{nameof(ModsDat)}{read}", () => m.ReadStringU(read, 255));

            var s1 = ModsRecord.StatNames1 == 0 ? 0 : m.Read<long>(ModsRecord.StatNames1);
            var s2 = ModsRecord.StatNames2 == 0 ? 0 : m.Read<long>(ModsRecord.StatNames2);
            var s3 = ModsRecord.StatNames3 == 0 ? 0 : m.Read<long>(ModsRecord.StatNames3);
            var s4 = ModsRecord.StatName4 == 0 ? 0 : m.Read<long>(ModsRecord.StatName4);

            StatNames = new[]
            {
                ModsRecord.StatNames1 == 0
                    ? null
                    : sDat.records[RemoteMemoryObject.Cache.StringCache.Read($"{nameof(ModsDat)}{s1}", () => m.ReadStringU(s1))],
                ModsRecord.StatNames2 == 0
                    ? null
                    : sDat.records[RemoteMemoryObject.Cache.StringCache.Read($"{nameof(ModsDat)}{s2}", () => m.ReadStringU(s2))],
                ModsRecord.StatNames3 == 0
                    ? null
                    : sDat.records[RemoteMemoryObject.Cache.StringCache.Read($"{nameof(ModsDat)}{s3}", () => m.ReadStringU(s3))],
                ModsRecord.StatName4 == 0
                    ? null
                    : sDat.records[RemoteMemoryObject.Cache.StringCache.Read($"{nameof(ModsDat)}{s4}", () => m.ReadStringU(s4))]
            };

            Domain = (ModDomain) ModsRecord.Domain;

            UserFriendlyName = RemoteMemoryObject.Cache.StringCache.Read($"{nameof(ModsDat)}{ModsRecord.UserFriendlyName}",
                () => m.ReadStringU(ModsRecord.UserFriendlyName));

            AffixType = (ModType) ModsRecord.AffixType;

            Group = RemoteMemoryObject.Cache.StringCache.Read($"{nameof(ModsDat)}{ModsRecord.Group}",
                () => m.ReadStringU(ModsRecord.Group));

            StatRange = new[]
            {
                new IntRange(ModsRecord.StatRange1, ModsRecord.StatRange2), new IntRange(ModsRecord.StatRange3, ModsRecord.StatRange4),
                new IntRange(ModsRecord.StatRange5, ModsRecord.StatRange6), new IntRange(ModsRecord.StatRange7, ModsRecord.StatRange8)
            };

            Tags = new TagsDat.TagRecord[ModsRecord.Tags];
            var ta = ModsRecord.ta;

            for (var i = 0; i < Tags.Length; i++)
            {
                var ii = ta + 0x8 + 0x10 * i;
                var l = m.Read<long>(ii, 0);

                Tags[i] = tagsDat.Records[
                    RemoteMemoryObject.Cache.StringCache.Read($"{nameof(ModsDat)}{l}", () => m.ReadStringU(l, 255))];
            }

            TagChances = new Dictionary<string, int>(ModsRecord.TagChances);
            var tc = ModsRecord.tc;

            for (var i = 0; i < Tags.Length; i++)
            {
                TagChances[Tags[i].Key] = m.Read<int>(tc + 4 * i);
            }

            IsEssence = ModsRecord.IsEssence == 0x01;

            Tier = RemoteMemoryObject.Cache.StringCache.Read($"{nameof(ModsDat)}{ModsRecord.Tier}",
                () => m.ReadStringU(ModsRecord.Tier));
        }

        /// <summary>The record's memory address.</summary>
        public long Address { get; }

        /// <summary>The mod's unique key.</summary>
        public string Key { get; }

        /// <summary>Whether the mod is a prefix, suffix, or other affix type.</summary>
        public ModType AffixType { get; }

        /// <summary>The domain the mod applies to (item, monster, etc.).</summary>
        public ModDomain Domain { get; }

        /// <summary>The mod group used to determine mutual exclusivity and tiering.</summary>
        public string Group { get; }

        /// <summary>The minimum item/monster level required for the mod to roll.</summary>
        public int MinLevel { get; }

        /// <summary>The stats granted by the mod. Game refers to Stats.dat line.</summary>
        public StatsDat.StatRecord[] StatNames { get; }

        /// <summary>The value ranges for each stat granted by the mod.</summary>
        public IntRange[] StatRange { get; }

        /// <summary>The spawn weight chances keyed by tag.</summary>
        public IDictionary<string, int> TagChances { get; }

        /// <summary>The tags associated with the mod. Game refers to Tags.dat line.</summary>
        public TagsDat.TagRecord[] Tags { get; }

        /// <summary>An unknown pointer field.</summary>
        public long Unknown8 { get; }

        /// <summary>The user-friendly display name of the mod.</summary>
        public string UserFriendlyName { get; }

        /// <summary>Whether the mod is an essence mod.</summary>
        public bool IsEssence { get; }

        /// <summary>The mod's tier label.</summary>
        public string Tier { get; }

        /// <summary>The mod's type name.</summary>
        public string TypeName { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Name: {UserFriendlyName}, Key: {Key}, MinLevel: {MinLevel}";
        }

        private class LevelComparer : IComparer<ModRecord>
        {
            public int Compare(ModRecord x, ModRecord y)
            {
                return -x.MinLevel + y.MinLevel;
            }
        }
    }
}
