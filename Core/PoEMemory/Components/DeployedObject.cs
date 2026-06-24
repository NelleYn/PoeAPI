using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Cache;
using GameOffsets;

namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Wraps an object deployed by an actor (e.g. a minion or mine), resolving it to its entity.
/// </summary>
public class DeployedObject : RemoteMemoryObject
{
    private readonly FrameCache<ActorDeployedObject> cacheValue;
    private Entity _entity;

    /// <summary>Initializes a new instance of the <see cref="DeployedObject"/> class.</summary>
    public DeployedObject()
    {
        cacheValue = new FrameCache<ActorDeployedObject>(() => M.Read<ActorDeployedObject>(Address));
    }

    private ActorDeployedObject Struct => cacheValue.Value;

    /// <summary>Gets the entity id of the deployed object.</summary>
    public uint ObjectId => Struct.ObjectId;

    /// <summary>Gets the key of the skill that deployed this object.</summary>
    public ushort SkillKey => Struct.SkillId;

    /// <summary>Gets the entity backing this deployed object (resolved and cached on first access).</summary>
    public Entity Entity => _entity ?? (_entity = EntityListWrapper.GetEntityById(ObjectId));
}
