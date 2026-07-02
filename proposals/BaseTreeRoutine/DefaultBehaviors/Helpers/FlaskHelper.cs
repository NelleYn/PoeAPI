// EXPERIMENTAL candidate ported from exApiTools/BasicFlaskRoutine — see proposals/BaseTreeRoutine/README.md. Not part of the build.

using System.Collections.Generic;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;

namespace ExileCore.TreeRoutine.DefaultBehaviors.Helpers;

/// <summary>
/// Flask reader ported from upstream <c>TreeRoutine/DefaultBehaviors/Helpers/FlaskHelper.cs</c>,
/// rewritten against this fork. Enumerates the five flask slots and answers "does slot N have enough
/// charges to fire?". <see cref="Flask"/> is only a marker component on this fork; usable charges are
/// computed from the flask entity's <see cref="Charges"/> component
/// (<see cref="Charges.NumCharges"/> / <see cref="Charges.ChargesPerUse"/>).
/// </summary>
public sealed class FlaskHelper
{
    private readonly GameController _gameController;

    public FlaskHelper(GameController gameController)
    {
        _gameController = gameController;
    }

    /// <summary>A single occupied flask slot together with its charge component.</summary>
    public readonly struct FlaskStatus
    {
        public FlaskStatus(int slotIndex, Entity item, Charges charges)
        {
            SlotIndex = slotIndex;
            Item = item;
            Charges = charges;
        }

        /// <summary>0-based flask slot (from <see cref="ServerInventory.InventSlotItem.PosX"/>).</summary>
        public int SlotIndex { get; }

        /// <summary>The flask <see cref="Entity"/>.</summary>
        public Entity Item { get; }

        /// <summary>The flask's <see cref="Charges"/> component.</summary>
        public Charges Charges { get; }

        /// <summary>Enough stored charges to pay for one use.</summary>
        public bool CanBeUsed =>
            Charges != null && Charges.ChargesPerUse > 0 && Charges.NumCharges >= Charges.ChargesPerUse;

        /// <summary>How many times the flask can currently be fired.</summary>
        public int UsesAvailable =>
            Charges != null && Charges.ChargesPerUse > 0 ? Charges.NumCharges / Charges.ChargesPerUse : 0;
    }

    /// <summary>Enumerate every occupied flask slot with a readable <see cref="Charges"/> component.</summary>
    public IEnumerable<FlaskStatus> GetFlasks()
    {
        var inventory = _gameController?.IngameState?.ServerData?.GetPlayerInventoryByType(InventoryTypeE.Flask);
        var slots = inventory?.InventorySlotItems;
        if (slots == null)
            yield break;

        foreach (var slot in slots)
        {
            var item = slot?.Item;
            if (item?.GetComponent<Flask>() == null) // not a flask
                continue;

            var charges = item.GetComponent<Charges>();
            if (charges == null)
                continue;

            yield return new FlaskStatus(slot.PosX, item, charges);
        }
    }

    /// <summary>Find the flask in a specific 0-based slot, or a default (empty) <see cref="FlaskStatus"/>.</summary>
    public FlaskStatus GetFlaskInSlot(int slotIndex)
    {
        foreach (var flask in GetFlasks())
            if (flask.SlotIndex == slotIndex)
                return flask;

        return default;
    }

    /// <summary><c>true</c> when the flask in <paramref name="slotIndex"/> has enough charges to fire.</summary>
    public bool CanUseFlaskInSlot(int slotIndex) => GetFlaskInSlot(slotIndex).CanBeUsed;
}
