namespace ExileCore.PoEMemory.MemoryObjects;

/// <summary>
/// Represents an object deployed by a skill (e.g. a totem, trap, or mine), identified by its object id and key.
/// </summary>
public class DeployedObject
{
    internal DeployedObject(uint objId, ushort objectKey)
    {
        ObjectId = objId;
        ObjectKey = objectKey;
    }

    /// <summary>Gets the unique identifier of the deployed object.</summary>
    public uint ObjectId { get; }

    /// <summary>Gets the skill/object key that identifies the deployed object's type.</summary>
    public ushort ObjectKey { get; }
}
