using SharpDX;

namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing the start and end world positions of a beam effect.
/// </summary>
public class Beam : Component
{
    /// <summary>Gets the beam start position (the casting entity's world position).</summary>
    public Vector3 BeamStart => M.Read<Vector3>(Address + 0x50);//beam start is actually the entity world pos

    /// <summary>Gets the beam end (target) world position.</summary>
    public Vector3 BeamEnd => M.Read<Vector3>(Address + 0x5C);

    /// <summary>Gets an unidentified value (appears to be two booleans).</summary>
    public int Unknown1 => M.Read<int>(Address + 0x40);//looks like 2 bools

    /// <summary>Gets an unidentified value.</summary>
    public int Unknown2 => M.Read<int>(Address + 0x44);
}
