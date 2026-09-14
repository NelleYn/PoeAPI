namespace ExileCore.PoEMemory;

/// <summary>
/// Type tag stored in a UI element node, identifying which concrete element the
/// node represents. Values are taken as observed in the reference distribution.
/// </summary>
public enum ElementType : ushort
{
    /// <summary>A miscellaneous ground label element.</summary>
    MiscGroundLabel = 8720,

    /// <summary>A label attached to an item lying on the ground.</summary>
    GroundItemLabel = 16516,

    /// <summary>A passive skill tree point element.</summary>
    PassivePoint = 30773,

    /// <summary>An item inside an inventory element.</summary>
    InventoryItem = 49156,
}
