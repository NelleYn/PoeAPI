namespace GameOffsets.Native;

/// <summary>
/// Shape tests for raw values read out of the game process.
/// </summary>
public static class NativePointer
{
    /// <summary>
    /// Whether a value has the shape of a user-mode heap pointer on x64: inside the user half of the
    /// address space and 8-aligned.
    /// </summary>
    /// <param name="value">A qword read from the game process.</param>
    /// <returns>True when the value could be a pointer; false when it is certainly not one.</returns>
    /// <remarks>
    /// <para>
    /// THIS IS THE MINIMUM CHECK, AND "NOT ZERO" IS NOT A CHECK AT ALL. <c>Memory.Read&lt;T&gt;</c>
    /// returns <c>default</c> on an unreadable address instead of throwing, and an offset that no
    /// longer matches the client holds GARBAGE far more often than it holds zero — so a "!= 0" test
    /// accepts the garbage and turns a wrong offset into silent, plausible-looking data. Every
    /// offset measured on this client points at an 8-aligned user-mode address, so this predicate
    /// rejects the usual garbage.
    /// </para>
    /// <para>
    /// IT DOES NOT PRETEND TO VALIDATE THE POINTER. A value passing this test may still be stale,
    /// unmapped, or point at something entirely unrelated. It only rules out values that cannot
    /// possibly be pointers.
    /// </para>
    /// <para>
    /// The same predicate exists privately as <c>IngameData.IsCanonicalPointer</c>, where it earned
    /// its keep on LabDataPtr. That copy should be pointed at this one in a later change, not in
    /// this one — Core/PoEMemory/MemoryObjects/IngameData.cs is outside the scope that introduced
    /// this helper.
    /// </para>
    /// </remarks>
    public static bool IsCanonical(long value)
    {
        return value >= 0x10000L && value <= 0x7FFFFFFFFFFFL && (value & 7) == 0;
    }
}
