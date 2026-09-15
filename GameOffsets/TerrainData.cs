using System.Runtime.InteropServices;
using GameOffsets.Native;

namespace GameOffsets
{
    /// <summary>
    /// Walkable-terrain description of the current area, embedded in
    /// <see cref="IngameDataOffsets.Terrain"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MEASURED against the running client, field by field: the reference distribution was asked
    /// for the value of every member (tools/RefLive prints the struct it reads), and each value was
    /// located inside this object with tools/FindOffset. The pointer members matched uniquely; the
    /// scalars were then read straight out of a window dump at the offsets the pointers implied,
    /// and every one of them agreed with the reference — 87/81 tiles, 88/82 tile-index cells,
    /// stride 1001, height multiplier 1, in an area where the reference reported exactly that.
    /// </para>
    /// <para>
    /// The offsets below are RELATIVE to <see cref="IngameDataOffsets.Terrain"/>, which sits at
    /// 0xC08 on this build. The layout is not the reference's: there <c>NumCols</c> and
    /// <c>NumRows</c> are adjacent 4-byte fields and <c>TileDescriptions</c> is followed
    /// immediately by <c>LayerMelee</c>; here each scalar is padded to 8 bytes and 0x50 bytes of
    /// unidentified data sit between the tile descriptions and the layers.
    /// </para>
    /// <para>
    /// UNITS, and the trap in them. <see cref="NumCols"/> and <see cref="NumRows"/> count TILES,
    /// not cells: one tile is 23 cells on a side. The walkable grid is
    /// <c>BytesPerRow * 2</c> cells wide (one byte packs two 4-bit cells) and
    /// <c>LayerMelee.Size / BytesPerRow</c> cells tall — which is <c>NumRows * 23</c>. Reading
    /// <see cref="NumRows"/> as a cell count yields a grid 23 times too short, and nothing in the
    /// numbers themselves says so: on the measured area NumRows was 81 while the grid was 1863
    /// rows tall, and 81 is a perfectly plausible-looking row count.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct TerrainData
    {
        /// <summary>Area width in TILES. Cells per row is <c>BytesPerRow * 2</c>, not this.</summary>
        [FieldOffset(0x00)] public int NumCols;

        /// <summary>Area height in TILES. Cells per column is this times 23.</summary>
        [FieldOffset(0x08)] public int NumRows;

        /// <summary>Targeting/height source array the client fills per tile.</summary>
        [FieldOffset(0x10)] public NativePtrArray TgtArray;

        /// <summary>Tile-index grid width. One more than <see cref="NumCols"/> on every area seen.</summary>
        [FieldOffset(0x28)] public int NumTileIndexCols;

        /// <summary>Tile-index grid height. One more than <see cref="NumRows"/> on every area seen.</summary>
        [FieldOffset(0x30)] public int NumTileIndexRows;

        /// <summary>Per-tile indices into <see cref="TileDescriptions"/>.</summary>
        [FieldOffset(0x38)] public NativePtrArray TileIndexes;

        /// <summary>Tile description records; each one names a .tmd asset.</summary>
        [FieldOffset(0x50)] public NativePtrArray TileDescriptions;

        /// <summary>Melee walkability plane: one 4-bit cell per nibble, row stride <see cref="BytesPerRow"/>.</summary>
        [FieldOffset(0xB8)] public NativePtrArray LayerMelee;

        /// <summary>Ranged (line of sight) plane. Same shape and stride as <see cref="LayerMelee"/>.</summary>
        [FieldOffset(0xD0)] public NativePtrArray LayerRanged;

        /// <summary>Row stride of both planes, in bytes. Cells per row is twice this.</summary>
        [FieldOffset(0xE8)] public int BytesPerRow;

        /// <summary>Scale applied to the tile height samples.</summary>
        [FieldOffset(0xEC)] public int TileHeightMultiplier;
    }
}
