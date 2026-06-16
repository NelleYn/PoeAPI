using System.Runtime.InteropServices;
using GameOffsets.Native;

namespace GameOffsets
{
    // Walkable-terrain grid for the current area, embedded in IngameStateData.
    // Field layout below (relative offsets) is stable across the ExileApi lineage;
    // LayerMelee/LayerRanged are std::vector<byte> (First/Last/End) and BytesPerRow
    // is the row stride in bytes (each byte packs two 4-bit cells).
    // NOTE: the *position of this struct inside IngameDataOffsets* (FieldOffset on
    // IngameDataOffsets.Terrain) is game-build specific — verify it against your
    // ExileApi-Compiled reference for the PoE patch you run.
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct TerrainData
    {
        [FieldOffset(0x08)] public NativePtrArray LayerMelee;
        [FieldOffset(0x20)] public NativePtrArray LayerRanged;
        [FieldOffset(0x38)] public int NumCols;
        [FieldOffset(0x3C)] public int NumRows;
        [FieldOffset(0x40)] public int BytesPerRow;
    }
}
