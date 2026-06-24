using System.Runtime.InteropServices;
using GameOffsets.Native;

namespace GameOffsets
{
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct IngameDataOffsets
    {
        [FieldOffset(0xA8)] public long CurrentArea;
        [FieldOffset(0xCC)] public byte CurrentAreaLevel;
        [FieldOffset(0x120)] public NativePtrArray MapStats;
        [FieldOffset(0x10C)] public uint CurrentAreaHash;
        [FieldOffset(0x8E8)] public long LocalPlayer;
        [FieldOffset(0x78)] public long LabDataPtr;
        [FieldOffset(0x9A0)] public long EntityList;
        [FieldOffset(0x9A8)] public long EntitiesCount;
        // VERIFY against your ExileApi-Compiled reference for the current PoE build.
        [FieldOffset(0xB68)] public TerrainData Terrain;
    }
}
