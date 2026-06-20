using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.Shared.Interfaces;

namespace ExileCore.PoEMemory.FilesInMemory;

/// <summary>
/// Reads the PassiveSkills.dat table, exposing each passive skill and lookups by passive id and string id.
/// </summary>
public class PassiveSkills : UniversalFileWrapper<PassiveSkill>
{
    private List<PassiveSkill> _EntriesList;
    private bool loaded;

    /// <summary>
    /// Initializes a new instance of the <see cref="PassiveSkills"/> class.
    /// </summary>
    /// <param name="m">The memory accessor used to read the game process.</param>
    /// <param name="address">A delegate returning the table's base address.</param>
    public PassiveSkills(IMemory m, Func<long> address) : base(m, address)
    {
    }

    /// <summary>Passive skills keyed by their numeric passive id.</summary>
    public Dictionary<int, PassiveSkill> PassiveSkillsDictionary { get; } = new Dictionary<int, PassiveSkill>();

    /// <summary>Gets all passive skills in the table, caching the materialized list on first access.</summary>
    public IList<PassiveSkill> EntriesList => _EntriesList ?? (_EntriesList = base.EntriesList.ToList());

    /// <summary>Gets the passive skill with the given numeric passive id, or <c>null</c> if not found.</summary>
    /// <param name="index">The numeric passive id.</param>
    public PassiveSkill GetPassiveSkillByPassiveId(int index)
    {
        CheckCache();

        if (!loaded)
        {
            foreach (var passiveSkill in EntriesList)
            {
                EntryAdded(passiveSkill.Address, passiveSkill);
            }

            loaded = true;
        }

        PassiveSkillsDictionary.TryGetValue(index, out var result);
        return result;
    }

    /// <summary>Gets the first passive skill whose string id matches <paramref name="id"/>, or <c>null</c>.</summary>
    /// <param name="id">The passive skill's string id.</param>
    public PassiveSkill GetPassiveSkillById(string id)
    {
        return EntriesList.FirstOrDefault(x => x.Id == id);
    }

    /// <summary>Indexes a passive skill by its numeric passive id as it is added to the cache.</summary>
    /// <param name="addr">The record's memory address.</param>
    /// <param name="entry">The record that was added.</param>
    protected void EntryAdded(long addr, PassiveSkill entry)
    {
        PassiveSkillsDictionary.Add(entry.PassiveId, entry);
    }

    /// <summary>Gets the passive skill located at the given memory address, or <c>null</c> if not found.</summary>
    /// <param name="address">The record's memory address.</param>
    public PassiveSkill GetByAddress(long address)
    {
        return base.GetByAddress(address);
    }
}
