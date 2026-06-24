using System.Collections.Generic;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using GameOffsets;
using SharpDX;

namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing an actor's current action, animation, deployed objects, and skills.
/// </summary>
public class Actor : Component
{
    private readonly FrameCache<ActorComponentOffsets> cacheValue;

    /// <summary>Initializes a new instance of the <see cref="Actor"/> class.</summary>
    public Actor()
    {
        cacheValue = new FrameCache<ActorComponentOffsets>(() => M.Read<ActorComponentOffsets>(Address));
    }

    private ActorComponentOffsets Struct => cacheValue.Value;

    /// <summary>
    /// Standing still = 2048 =bit 11 set
    /// running = 2178 = bit 11 &amp; 7
    /// Maybe Bit-field : Bit 7 set = running
    /// </summary>
    public short ActionId => Address != 0 ? Struct.ActionId : (short) 0;

    /// <summary>Gets the current action as a set of <see cref="ActionFlags"/>.</summary>
    public ActionFlags Action => Address != 0 ? (ActionFlags) Struct.ActionId : ActionFlags.None;

    /// <summary>Gets a value indicating whether the actor is currently moving.</summary>
    public bool isMoving => (Action & ActionFlags.Moving) > 0;

    /// <summary>Gets a value indicating whether the actor is currently using an ability.</summary>
    public bool isAttacking => (Action & ActionFlags.UsingAbility) > 0;

    /// <summary>Gets the raw animation id of the current animation.</summary>
    public int AnimationId => Address != 0 ? Struct.AnimationId : 0;

    /// <summary>Gets the current animation as an <see cref="AnimationE"/>.</summary>
    public AnimationE Animation => Address != 0 ? (AnimationE) Struct.AnimationId : AnimationE.Idle;

    /// <summary>
    /// Currently performed action information.
    /// WARNING: This memory location changes a lot,
    /// put try catch if you are accessing this variable and the fields in it.
    /// </summary>
    public ActionWrapper CurrentAction => Struct.ActionPtr > 0 ? GetObject<ActionWrapper>(Struct.ActionPtr) : null;

    /// <summary>Gets the number of objects deployed by this actor (e.g. minions, mines).</summary>
    public long DeployedObjectsCount => Struct.DeployedObjectArray.Size / 8;

    /// <summary>Gets the list of objects deployed by this actor (e.g. minions, mines).</summary>
    public List<DeployedObject> DeployedObjects
    {
        get
        {
            var result = new List<DeployedObject>();

            if ((Struct.DeployedObjectArray.Last - Struct.DeployedObjectArray.First) / 8 > 300)
            {
                return result;
            }

            for (var addr = Struct.DeployedObjectArray.First; addr < Struct.DeployedObjectArray.Last; addr += 8)
            {
                result.Add(GetObject<DeployedObject>(addr));
            }

            return result;
        }
    }

    /// <summary>Gets the list of skills available to this actor.</summary>
    public List<ActorSkill> ActorSkills
    {
        get
        {
            var skillsStartPointer = Struct.ActorSkillsArray.First;
            var skillsEndPointer = Struct.ActorSkillsArray.Last;
            skillsStartPointer += 8; //Don't ask me why. Just skipping first one

            if ((skillsEndPointer - skillsStartPointer) / 16 > 50)
                return new List<ActorSkill>();

            var result = new List<ActorSkill>();

            for (var addr = skillsStartPointer;
                addr < skillsEndPointer;
                addr += 16) //16 because we are reading each second pointer (pointer vectors)
            {
                result.Add(ReadObject<ActorSkill>(addr));
            }

            return result;
        }
    }

    /// <summary>
    /// Wraps the currently performed action, exposing its destination, target, and skill.
    /// </summary>
    public class ActionWrapper : RemoteMemoryObject
    {
        private readonly FrameCache<ActionWrapperOffsets> cacheValue;

        /// <summary>Initializes a new instance of the <see cref="ActionWrapper"/> class.</summary>
        public ActionWrapper()
        {
            cacheValue = new FrameCache<ActionWrapperOffsets>(() => M.Read<ActionWrapperOffsets>(Address));
        }

        private ActionWrapperOffsets Struct => cacheValue.Value;

        /// <summary>Gets the X component of the action destination.</summary>
        public float DestinationX => Struct.Destination.X;

        /// <summary>Gets the Y component of the action destination.</summary>
        public float DestinationY => Struct.Destination.Y;

        /// <summary>Gets the action destination.</summary>
        public Vector2 Destination => Struct.Destination.ToVector2();

        /// <summary>Gets the targeted entity of the action.</summary>
        public Entity Target => GetObject<Entity>(Struct.Target);

        /// <summary>Gets the cast destination of the action.</summary>
        public Vector2 CastDestination => new Vector2(DestinationX, DestinationY);

        /// <summary>Gets the skill being used by the action.</summary>
        public ActorSkill Skill => GetObject<ActorSkill>(Struct.Skill);
    }
}
