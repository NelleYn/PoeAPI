using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Interfaces;

namespace ExileCore.PoEMemory.FilesInMemory;

/// <summary>
/// Reads the LabyrinthTrials.dat table, exposing each trial and lookups by area, id, and area id.
/// </summary>
public class LabyrinthTrials : UniversalFileWrapper<LabyrinthTrial>
{
    /// <summary>The known world area ids that correspond to labyrinth trials.</summary>
    public static string[] LabyrinthTrialAreaIds = new string[18]
    {
        "1_1_7_1", "1_2_5_1", "1_2_6_2", "1_3_3_1", "1_3_6", "1_3_15", "2_6_7_1", "2_7_4", "2_7_5_2", "2_8_5", "2_9_7", "2_10_9",
        "EndGame_Labyrinth_trials_spikes", "EndGame_Labyrinth_trials_spinners", "EndGame_Labyrinth_trials_sawblades_#",
        "EndGame_Labyrinth_trials_lava_#", "EndGame_Labyrinth_trials_roombas", "EndGame_Labyrinth_trials_arrows"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="LabyrinthTrials"/> class.
    /// </summary>
    /// <param name="m">The memory accessor used to read the game process.</param>
    /// <param name="address">A delegate returning the table's base address.</param>
    public LabyrinthTrials(IMemory m, Func<long> address) : base(m, address)
    {
    }

    /// <summary>Gets all labyrinth trials in the table.</summary>
    public IList<LabyrinthTrial> EntriesList => base.EntriesList.ToList();

    /// <summary>Gets the first trial whose area id matches <paramref name="id"/>, or <c>null</c>.</summary>
    /// <param name="id">The trial area's string id.</param>
    public LabyrinthTrial GetLabyrinthTrialByAreaId(string id)
    {
        return EntriesList.FirstOrDefault(x => x.Area.Id == id);
    }

    /// <summary>Gets the first trial whose id matches <paramref name="index"/>, or <c>null</c>.</summary>
    /// <param name="index">The trial id.</param>
    public LabyrinthTrial GetLabyrinthTrialById(int index)
    {
        return EntriesList.FirstOrDefault(x => x.Id == index);
    }

    /// <summary>Gets the first trial associated with the given world area, or <c>null</c>.</summary>
    /// <param name="area">The world area to match.</param>
    public LabyrinthTrial GetLabyrinthTrialByArea(WorldArea area)
    {
        return EntriesList.FirstOrDefault(x => x.Area == area);
    }

    /// <summary>Gets the labyrinth trial located at the given memory address, or <c>null</c> if not found.</summary>
    /// <param name="address">The record's memory address.</param>
    public LabyrinthTrial GetByAddress(long address)
    {
        return base.GetByAddress(address);
    }
}
