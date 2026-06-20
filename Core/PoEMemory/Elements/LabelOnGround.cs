using System;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;

namespace ExileCore.PoEMemory.Elements;

/// <summary>
/// Memory object pairing a ground item entity with its on-screen label, exposing pick-up state and timing.
/// </summary>
public class LabelOnGround : RemoteMemoryObject
{
    private readonly Lazy<string> debug;
    private readonly Lazy<long> labelInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="LabelOnGround"/> class.
    /// </summary>
    public LabelOnGround()
    {
        labelInfo = new Lazy<long>(GetLabelInfo);

        debug = new Lazy<string>(() =>
        {
            return ItemOnGround.HasComponent<WorldItem>()
                ? ItemOnGround.GetComponent<WorldItem>().ItemEntity?.GetComponent<Base>()?.Name
                : ItemOnGround.Path;
        });
    }

    /// <summary>
    /// Gets a value indicating whether the label is currently visible.
    /// </summary>
    public bool IsVisible => Label?.IsVisible ?? false;

    /// <summary>
    /// Gets the ground item entity associated with this label, or <c>null</c> when none.
    /// </summary>
    public Entity ItemOnGround
    {
        get
        {
            var readObjectAt = ReadObjectAt<Entity>(0x10);
            return readObjectAt.Address == 0 ? null : readObjectAt;
        }
    }

    /// <summary>
    /// Gets the label element associated with this ground item, or <c>null</c> when none.
    /// </summary>
    public Element Label
    {
        get
        {
            var readObjectAt = ReadObjectAt<Element>(0x18);
            return readObjectAt.Address == 0 ? null : readObjectAt;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the item can currently be picked up.
    /// </summary>
    // Temp solution for pick it, need test PickTest and PickTest2.
    public bool CanPickUp
    {
        get
        {
            var label = Label;

            if (label != null)
                return M.Read<long>(label.Address + 0x420) == 0;

            return true;
        }
    }

    /// <summary>
    /// Gets the time remaining until the item can be picked up.
    /// </summary>
    public TimeSpan TimeLeft
    {
        get
        {
            if (CanPickUp) return new TimeSpan();
            if (labelInfo.Value == 0) return MaxTimeForPickUp;
            var futureTime = M.Read<int>(labelInfo.Value + 0x38);
            return TimeSpan.FromMilliseconds(futureTime - Environment.TickCount);
        }
    }

    /// <summary>
    /// Gets the maximum wait time before the item becomes pickable.
    /// </summary>
    // Temp solution for pick it.
    public TimeSpan MaxTimeForPickUp => TimeSpan.Zero;

    private long GetLabelInfo()
    {
        return Label != null ? Label.Address != 0 ? M.Read<long>(Label.Address + 0x3A8) : 0 : 0;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return debug.Value;
    }
}
