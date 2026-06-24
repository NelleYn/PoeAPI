using System.Collections.Generic;
using ExileCore.PoEMemory.MemoryObjects;

namespace ExileCore.PoEMemory.Elements;

/// <summary>
/// UI element exposing the labels and items currently shown on the ground in the game world.
/// </summary>
public partial class ItemsOnGroundLabelElement : Element
{
    /// <summary>
    /// Gets the label element currently under the cursor, or <c>null</c> when none.
    /// </summary>
    public Element LabelOnHover
    {
        get
        {
            var readObjectAt = ReadObjectAt<Element>(0x248);
            return readObjectAt.Address == 0 ? null : readObjectAt;
        }
    }

    /// <summary>
    /// Gets the item entity currently under the cursor, or <c>null</c> when none.
    /// </summary>
    public Entity ItemOnHover
    {
        get
        {
            var readObjectAt = ReadObjectAt<Entity>(0x250);
            return readObjectAt.Address == 0 ? null : readObjectAt;
        }
    }

    /// <summary>
    /// Gets the metadata path of the hovered item, or <c>"Null"</c> when none.
    /// </summary>
    public string ItemOnHoverPath => ItemOnHover != null ? ItemOnHover.Path : "Null";

    /// <summary>
    /// Gets the text of the hovered label, or <c>"Null"</c> when none.
    /// </summary>
    public string LabelOnHoverText => LabelOnHover != null ? LabelOnHover.Text : "Null";

    /// <summary>
    /// Gets a label count read from a fixed offset.
    /// </summary>
    public int CountLabels => M.Read<int>(Address + 0x268);

    /// <summary>
    /// Gets a second label count read from a fixed offset.
    /// </summary>
    public int CountLabels2 => M.Read<int>(Address + 0x2A8);

    /// <summary>
    /// Gets the list of visible ground labels, or <c>null</c> when the list is unavailable or malformed.
    /// </summary>
    public List<LabelOnGround> LabelsOnGround
    {
        get
        {
            var address = M.Read<long>(Address + 0x2A0);

            var result = new List<LabelOnGround>();

            if (address <= 0)
                return null;

            var limit = 0;

            for (var nextAddress = M.Read<long>(address); nextAddress != address; nextAddress = M.Read<long>(nextAddress))
            {
                var labelOnGround = GetObject<LabelOnGround>(nextAddress);

                if (labelOnGround.Label.IsValid)
                    result.Add(labelOnGround);

                limit++;

                if (limit > 1000)
                    return null;
            }

            return result;
        }
    }
}
