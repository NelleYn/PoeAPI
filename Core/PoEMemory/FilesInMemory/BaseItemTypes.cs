using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ExileCore.PoEMemory.Models;
using ExileCore.Shared.Interfaces;

namespace ExileCore.PoEMemory.FilesInMemory;

/// <summary>
/// Reads the BaseItemTypes.dat table, exposing each base item type indexed by its
/// metadata path and by record address.
/// </summary>
public class BaseItemTypes : FileInMemory
{
    private int tries = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseItemTypes"/> class and loads all item types.
    /// </summary>
    /// <param name="m">The memory accessor used to read the game process.</param>
    /// <param name="address">A delegate returning the table's base address.</param>
    public BaseItemTypes(IMemory m, Func<long> address) : base(m, address)
    {
        LoadItemTypes();
    }

    /// <summary>Base item types keyed by their metadata path.</summary>
    public Dictionary<string, BaseItemType> Contents { get; } = new Dictionary<string, BaseItemType>();

    /// <summary>Base item types keyed by their record memory address.</summary>
    public Dictionary<long, BaseItemType> ContentsAddr { get; } = new Dictionary<long, BaseItemType>();

    /// <summary>Gets the base item type at the given record address, or <c>null</c> if not found.</summary>
    /// <param name="address">The record's memory address.</param>
    public BaseItemType GetFromAddress(long address)
    {
        ContentsAddr.TryGetValue(address, out var type);
        return type;
    }

    /// <summary>Resolves a base item type from its metadata path, or <c>null</c> if unknown.</summary>
    /// <param name="metadata">The item's metadata path.</param>
    public BaseItemType Translate(string metadata)
    {
        if (Contents.Count == 0) LoadItemTypes();

        if (metadata == null) return null;

        if (!Contents.TryGetValue(metadata, out var type))
        {
            Console.WriteLine("Key not found in BaseItemTypes: " + metadata);
            return null;
        }

        return type;
    }

    private void LoadItemTypes()
    {
        foreach (var i in RecordAddresses())
        {
            var key = M.ReadStringU(M.Read<long>(i));

            var baseItemType = new BaseItemType
            {
                Metadata = key,
                ClassName = M.ReadStringU(M.Read<long>(i + 0x10, 0)),
                Width = M.Read<int>(i + 0x18),
                Height = M.Read<int>(i + 0x1C),
                BaseName = M.ReadStringU(M.Read<long>(i + 0x20)),
                DropLevel = M.Read<int>(i + 0x30),
                Tags = new string[M.Read<long>(i + 0xA8)]
            };

            var ta = M.Read<long>(i + 0xB0);

            for (var k = 0; k < baseItemType.Tags.Length; k++)
            {
                var ii = ta + 0x8 + 0x10 * k;
                baseItemType.Tags[k] = M.ReadStringU(M.Read<long>(ii, 0), 255);
            }

            var tmpTags = key.Split('/');
            string tmpKey;

            if (tmpTags.Length > 3)
            {
                baseItemType.MoreTagsFromPath = new string[tmpTags.Length - 3];

                for (var k = 2; k < tmpTags.Length - 1; k++)
                {
                    // This Regex and if condition change Item Path Category e.g. TwoHandWeapons
                    // To tag strings type e.g. two_hand_weapon
                    tmpKey = Regex.Replace(tmpTags[k], @"(?<!_)([A-Z])", "_$1").ToLower().Remove(0, 1);

                    if (tmpKey[tmpKey.Length - 1] == 's')
                        tmpKey = tmpKey.Remove(tmpKey.Length - 1);

                    baseItemType.MoreTagsFromPath[k - 2] = tmpKey;
                }
            }
            else
            {
                baseItemType.MoreTagsFromPath = new string[1];
                baseItemType.MoreTagsFromPath[0] = "";
            }

            ContentsAddr.Add(i, baseItemType);

            if (!Contents.ContainsKey(key)) Contents.Add(key, baseItemType);
        }
    }
}
