using System;

namespace GameOffsets;

/// <summary>
/// Bit flags stored in the entity's object header byte.
/// </summary>
[Flags]
public enum EntityFlags : byte
{
    /// <summary>The entity record is valid.</summary>
    Valid = 1,
}
