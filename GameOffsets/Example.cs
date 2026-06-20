using System.Runtime.InteropServices;

namespace GameOffsets;

/// <summary>
/// Template/reference struct illustrating the explicit-layout offset pattern used
/// throughout this assembly. Not used at runtime.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct Example
{
    /// <summary>Placeholder field demonstrating a <see cref="FieldOffsetAttribute"/>.</summary>
    [FieldOffset(0x0)] public int SomeField;
}
