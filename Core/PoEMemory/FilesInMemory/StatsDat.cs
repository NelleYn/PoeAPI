using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Interfaces;

namespace ExileCore.PoEMemory.FilesInMemory;

/// <summary>
/// Reads the Stats.dat table, exposing each stat record indexed by key and by sequential id.
/// </summary>
public class StatsDat : FileInMemory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StatsDat"/> class and loads all stat records.
    /// </summary>
    /// <param name="m">The memory accessor used to read the game process.</param>
    /// <param name="address">A delegate returning the table's base address.</param>
    public StatsDat(IMemory m, Func<long> address) : base(m, address)
    {
        loadItems();
    }

    /// <summary>Stat records keyed by their (case-insensitive) key.</summary>
    public IDictionary<string, StatRecord> records { get; } = new Dictionary<string, StatRecord>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Stat records keyed by their sequential id.</summary>
    public IDictionary<int, StatRecord> recordsById { get; } = new Dictionary<int, StatRecord>();

    /// <summary>Gets the stat record located at the given memory address, or <c>null</c> if not found.</summary>
    /// <param name="address">The record's memory address.</param>
    public StatRecord GetStatByAddress(long address)
    {
        return records.Values.ToList().Find(x => x.Address == address);
    }

    private void loadItems()
    {
        var iCounter = 1;

        foreach (var addr in RecordAddresses())
        {
            var r = new StatRecord(M, addr, iCounter++);
            records[r.Key] = r;
            recordsById[r.ID] = r;
        }
    }

    /// <summary>A single stat parsed from the Stats.dat table.</summary>
    public class StatRecord
    {
        /// <summary>
        /// Reads a single stat record from memory at the given address.
        /// </summary>
        /// <param name="m">The memory accessor used to read the game process.</param>
        /// <param name="addr">The record's memory address.</param>
        /// <param name="iCounter">The sequential id assigned to this record.</param>
        public StatRecord(IMemory m, long addr, int iCounter)
        {
            Address = addr;

            Key = RemoteMemoryObject.Cache.StringCache.Read($"{nameof(StatsDat)}{addr + 0}",
                () => m.ReadStringU(m.Read<long>(addr + 0), 255));

            Unknown4 = m.Read<byte>(addr + 0x8) != 0;
            Unknown5 = m.Read<byte>(addr + 0x9) != 0;
            Unknown6 = m.Read<byte>(addr + 0xA) != 0;
            Type = Key.Contains("%") ? StatType.Percents : (StatType) m.Read<int>(addr + 0xB);
            UnknownB = m.Read<byte>(addr + 0xF) != 0;

            UserFriendlyName =
                RemoteMemoryObject.Cache.StringCache.Read($"{nameof(StatsDat)}{addr + 0x10}",
                    () => m.ReadStringU(m.Read<long>(addr + 0x10), 255));

            ID = iCounter;
        }

        /// <summary>The stat's unique key.</summary>
        public string Key { get; }

        /// <summary>The record's memory address.</summary>
        public long Address { get; }

        /// <summary>The stat's value type, used by <see cref="ValueToString"/>.</summary>
        public StatType Type { get; }

        /// <summary>An unknown flag field.</summary>
        public bool Unknown4 { get; }

        /// <summary>An unknown flag field.</summary>
        public bool Unknown5 { get; }

        /// <summary>An unknown flag field.</summary>
        public bool Unknown6 { get; }

        /// <summary>An unknown flag field.</summary>
        public bool UnknownB { get; }

        /// <summary>The user-friendly display name of the stat.</summary>
        public string UserFriendlyName { get; }

        // more fields can be added (see in visualGGPK)

        /// <summary>The sequential id assigned to this stat.</summary>
        public int ID { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(UserFriendlyName) ? Key : UserFriendlyName;
        }

        /// <summary>Formats a stat value according to the stat's <see cref="Type"/>.</summary>
        /// <param name="val">The raw stat value.</param>
        public string ValueToString(int val)
        {
            switch (Type)
            {
                case StatType.Boolean:
                    return val != 0 ? "True" : "False";

                case StatType.IntValue:
                case StatType.Value2:
                    return val.ToString("+#;-#");
                case StatType.Percents:
                case StatType.Precents5:
                    return val.ToString("+#;-#") + "%";
            }

            return "";
        }
    }
}
