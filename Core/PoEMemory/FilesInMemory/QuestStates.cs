using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Interfaces;

namespace ExileCore.PoEMemory.FilesInMemory;

/// <summary>
/// Reads the QuestStates.dat table, exposing quest states grouped by quest id and state id.
/// </summary>
public class QuestStates : UniversalFileWrapper<QuestState>
{
    private Dictionary<string, Dictionary<int, QuestState>> QuestStatesDictionary;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuestStates"/> class.
    /// </summary>
    /// <param name="m">The memory accessor used to read the game process.</param>
    /// <param name="address">A delegate returning the table's base address.</param>
    public QuestStates(IMemory m, Func<long> address) : base(m, address)
    {
    }

    /// <summary>Gets all quest states in the table.</summary>
    public IList<QuestState> EntriesList => base.EntriesList.ToList();

    /// <summary>
    /// Gets the quest state for the given quest and state id, building the lookup on first call.
    /// Returns <c>null</c> if no matching state exists.
    /// </summary>
    /// <param name="questId">The owning quest's id.</param>
    /// <param name="stateId">The quest state's id.</param>
    public QuestState GetQuestState(string questId, int stateId)
    {
        Dictionary<int, QuestState> dictionary;

        if (QuestStatesDictionary == null)
        {
            CheckCache();
            var qStates = EntriesList;
            QuestStatesDictionary = new Dictionary<string, Dictionary<int, QuestState>>();

            try
            {
                foreach (var item in qStates)
                {
                    if (!QuestStatesDictionary.TryGetValue(item.Quest.Id, out dictionary))
                    {
                        dictionary = new Dictionary<int, QuestState>();
                        QuestStatesDictionary.Add(item.Quest.Id.ToLowerInvariant(), dictionary);
                    }

                    dictionary.Add(item.QuestStateId, item);
                }
            }
            catch (Exception)
            {
                QuestStatesDictionary = null;
                throw;
            }
        }

        if (QuestStatesDictionary.TryGetValue(questId.ToLowerInvariant(), out dictionary) &&
            dictionary.TryGetValue(stateId, out var result)) return result;

        return null;
    }

    /// <summary>Gets the quest state located at the given memory address, or <c>null</c> if not found.</summary>
    /// <param name="address">The record's memory address.</param>
    public QuestState GetByAddress(long address)
    {
        return base.GetByAddress(address);
    }
}
