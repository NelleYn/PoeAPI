using System;
using System.Runtime.InteropServices;

namespace GameOffsets.Native
{
    /// <summary>
    /// Mirror of the game's native <c>std::vector</c> header (begin / end / capacity pointers).
    /// </summary>
    /// <remarks>
    /// Layout-identical to <see cref="NativePtrArray"/> — three sequential 8-byte pointers — and kept
    /// alongside it purely for source compatibility with ExileApi-Compiled plugins, which name this
    /// type <c>StdVector</c> (e.g. <c>Radar/Radar.Pathfinding.cs</c>,
    /// <c>PathfindSanctum/RewardHelper.cs</c>). Prefer <see cref="NativePtrArray"/> in new fork code;
    /// this type exists so ported call sites compile unchanged.
    /// <para>
    /// Because the two structs share a layout, a header already read as a <see cref="NativePtrArray"/>
    /// can be reused here via <see cref="FromNativePtrArray"/> at no memory-read cost. There is no
    /// conversion in the opposite direction: <see cref="NativePtrArray"/> exposes <c>readonly</c>
    /// fields and no constructor, so it cannot be built from a <see cref="StdVector"/> — pass
    /// <see cref="First"/>/<see cref="Last"/> to the raw-bounds reader overloads instead.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct StdVector : IEquatable<StdVector>
    {
        /// <summary>Pointer to the first element.</summary>
        public long First;

        /// <summary>Pointer to one past the last element.</summary>
        public long Last;

        /// <summary>Pointer to the end of the allocated capacity.</summary>
        public long End;

        /// <summary>The number of bytes between <see cref="First"/> and <see cref="Last"/>.</summary>
        public long Size => Last - First;

        /// <summary>Creates a <see cref="StdVector"/> from the fork-native <see cref="NativePtrArray"/>.</summary>
        /// <param name="array">The source header.</param>
        public static StdVector FromNativePtrArray(NativePtrArray array)
        {
            return new StdVector { First = array.First, Last = array.Last, End = array.End };
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"First: 0x{First}, Last: 0x{Last}, End: 0x{End} Size:{Size}";
        }

        /// <inheritdoc/>
        public bool Equals(StdVector other)
        {
            return First == other.First && Last == other.Last && End == other.End;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is StdVector other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (((First.GetHashCode() * 397) ^ Last.GetHashCode()) * 397) ^ End.GetHashCode();
        }
    }
}
