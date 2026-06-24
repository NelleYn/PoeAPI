using System;
using System.Collections;
using System.Collections.Generic;
using ExileCore.PoEMemory.FilesInMemory;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Enums;
using SharpDX;

namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing player-specific information such as name, attributes, level, prophecies,
/// pantheon choices, hideout, and labyrinth trial completion.
/// </summary>
public class Player : Component
{
    // Offsets verified against client 328.8 via an in-process Marshal.OffsetOf dump
    // (PlayerName 0x168, Xp 0x18C, Attributes 0x19C, Level 0x1AC).

    /// <summary>Gets the player's character name.</summary>
    public string PlayerName => NativeStringReader.ReadString(Address + 0x168, M);

    /// <summary>Gets the player's total experience.</summary>
    public uint XP => Address != 0 ? M.Read<uint>(Address + 0x18C) : 0;

    /// <summary>Gets the player's strength attribute.</summary>
    public int Strength => Address != 0 ? M.Read<int>(Address + 0x19C) : 0;

    /// <summary>Gets the player's dexterity attribute.</summary>
    public int Dexterity => Address != 0 ? M.Read<int>(Address + 0x1A0) : 0;

    /// <summary>Gets the player's intelligence attribute.</summary>
    public int Intelligence => Address != 0 ? M.Read<int>(Address + 0x1A4) : 0;

    /// <summary>Gets the player's level.</summary>
    public int Level => Address != 0 ? M.Read<byte>(Address + 0x1AC) : 1;

    /// <summary>Gets the allocated loot id of the player.</summary>
    public int AllocatedLootId => Address != 0 ? M.Read<byte>(Address + 0x7C) : 1;

    /// <summary>Gets the player's hideout level.</summary>
    public int HideoutLevel => M.Read<byte>(Address + 0x28E);

    /// <summary>Gets the number of prophecies the player currently has.</summary>
    public byte PropheciesCount => M.Read<byte>(Address + 0x112);

    /// <summary>Gets the prophecies the player currently has sealed in their inventory.</summary>
    public IList<ProphecyDat> Prophecies
    {
        get
        {
            var result = new List<ProphecyDat>();
            var readAddr = Address + 0x114;

            for (var i = 0; i < 7; i++)
            {
                var prophecyId = M.Read<ushort>(readAddr);

                //if(prophacyId > 0)//Dunno why it will never be 0 even if there is no prophecy
                {
                    var prophecy = TheGame.Files.Prophecies.GetProphecyById(prophecyId);

                    // if (prophecy != null)
                    result.Add(prophecy);
                }

                readAddr += 4; //prophecy prophecyId(UShort), Skip index(byte), Skip unknown(byte)
            }

            return result;
        }
    }

    /// <summary>Gets the player's hideout wrapper.</summary>
    public HideoutWrapper Hideout => ReadObject<HideoutWrapper>(Address + 0x268);

    /// <summary>Gets the player's chosen minor pantheon god.</summary>
    public PantheonGod PantheonMinor => (PantheonGod) M.Read<byte>(Address + 0x93);

    /// <summary>Gets the player's chosen major pantheon god.</summary>
    public PantheonGod PantheonMajor => (PantheonGod) M.Read<byte>(Address + 0x94);

    private IList<PassiveSkill> AllocatedPassivesM()
    {
        var result = new List<PassiveSkill>();
        var passiveIds = TheGame.IngameState.ServerData.PassiveSkillIds;

        foreach (var id in passiveIds)
        {
            var passive = TheGame.Files.PassiveSkills.GetPassiveSkillByPassiveId(id);

            if (passive == null)
            {
                DebugWindow.LogMsg($"Can't find passive with id: {id}", 10, Color.Red);
                continue;
            }

            result.Add(passive);
        }

        return result;
    }

    #region Trials

    /// <summary>Determines whether the labyrinth trial with the given area id has been completed.</summary>
    /// <param name="trialId">The trial area id (see <c>WorldArea.Id</c> or <c>LabyrinthTrials.LabyrinthTrialAreaIds</c>).</param>
    /// <returns><c>true</c> if the trial is completed; otherwise <c>false</c>.</returns>
    public bool IsTrialCompleted(string trialId)
    {
        var trialWrapper = TheGame.Files.LabyrinthTrials.GetLabyrinthTrialByAreaId(trialId);

        if (trialWrapper == null)
        {
            throw new ArgumentException(
                $"Trial with id '{trialId}' is not found. Use WorldArea.Id or LabyrinthTrials.LabyrinthTrialAreaIds[]");
        }

        return TrialPassStates.Get(trialWrapper.Id - 1);
    }

    /// <summary>Determines whether the given labyrinth trial has been completed.</summary>
    /// <param name="trialWrapper">The trial to check.</param>
    /// <returns><c>true</c> if the trial is completed; otherwise <c>false</c>.</returns>
    public bool IsTrialCompleted(LabyrinthTrial trialWrapper)
    {
        if (trialWrapper == null)
            throw new ArgumentException($"Argument {nameof(trialWrapper)} should not be null");

        return TrialPassStates.Get(trialWrapper.Id - 1);
    }

    /// <summary>Determines whether the labyrinth trial in the given area has been completed.</summary>
    /// <param name="area">The world area containing the trial.</param>
    /// <returns><c>true</c> if the trial is completed; otherwise <c>false</c>.</returns>
    public bool IsTrialCompleted(WorldArea area)
    {
        if (area == null)
            throw new ArgumentException($"Argument {nameof(area)} should not be null");

        var trialWrapper = TheGame.Files.LabyrinthTrials.GetLabyrinthTrialByArea(area);

        if (trialWrapper == null)
            throw new ArgumentException($"Can't find trial wrapper for area '{area.Name}' (seems not a trial area).");

        return TrialPassStates.Get(trialWrapper.Id - 1);
    }

    [HideInReflection]
    private BitArray TrialPassStates
    {
        get
        {
            var stateBuff = M.ReadBytes(Address + 0x1B4, 36); // (286+) bytes of info.
            return new BitArray(stateBuff);
        }
    }

    #region Debug things

    /// <summary>Gets the completion state of every known labyrinth trial (for debugging).</summary>
    public IList<TrialState> TrialStates
    {
        get
        {
            var result = new List<TrialState>();
            var passStates = TrialPassStates;

            foreach (var trialAreaId in LabyrinthTrials.LabyrinthTrialAreaIds)
            {
                var wrapper = TheGame.Files.LabyrinthTrials.GetLabyrinthTrialByAreaId(trialAreaId);

                result.Add(
                    new TrialState {TrialAreaId = trialAreaId, TrialArea = wrapper, IsCompleted = passStates.Get(wrapper.Id - 1)});
            }

            return result;
        }
    }

    /// <summary>Describes the completion state of a single labyrinth trial.</summary>
    public class TrialState
    {
        /// <summary>Gets the labyrinth trial this state describes.</summary>
        public LabyrinthTrial TrialArea { get; internal set; }

        /// <summary>Gets the area id of the trial.</summary>
        public string TrialAreaId { get; internal set; }

        /// <summary>Gets a value indicating whether the trial has been completed.</summary>
        public bool IsCompleted { get; internal set; }

        /// <summary>Gets the hexadecimal address of the trial area.</summary>
        public string AreaAddr => TrialArea.Address.ToString("x");

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Completed: {IsCompleted}, Trial {TrialArea.Area.Name}, AreaId: {TrialArea.Id}";
        }
    }

    #endregion

    #endregion
}
