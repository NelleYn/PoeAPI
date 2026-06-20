using ExileCore.Shared.Cache;
using GameOffsets;
using GameOffsets.Native;

namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing an entity's movement and pathfinding state.
/// </summary>
public class Pathfinding : Component
{
    private readonly CachedValue<PathfindingComponentOffsets> _cachedValue;

    /// <summary>Initializes a new instance of the <see cref="Pathfinding"/> class.</summary>
    public Pathfinding()
    {
        _cachedValue = new FrameCache<PathfindingComponentOffsets>(() => M.Read<PathfindingComponentOffsets>(Address));
    }

    private PathfindingComponentOffsets _offsets => _cachedValue.Value;

    /// <summary>Gets the next grid position the entity is moving toward.</summary>
    public Vector2i TargetMovePos => _offsets.ClickToNextPosition;

    /// <summary>Gets the previous grid position the entity occupied.</summary>
    public Vector2i PreviousMovePos => _offsets.WasInThisPosition;

    /// <summary>Gets the grid position the entity ultimately wants to move to.</summary>
    public Vector2i WantMoveToPosition => _offsets.WantMoveToPosition;

    /// <summary>Gets a value indicating whether the entity is currently moving.</summary>
    public bool IsMoving => _offsets.IsMoving == 2;

    /// <summary>Gets the time the entity has been stationary at its current position.</summary>
    public float StayTime => _offsets.StayTime;
}
