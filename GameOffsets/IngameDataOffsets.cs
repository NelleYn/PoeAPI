using System.Runtime.InteropServices;
using GameOffsets.Native;

namespace GameOffsets
{
    /// <summary>
    /// Layout of the object behind <see cref="IngameStateOffsets.Data"/> on the installed client.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every offset in the MEASURED block below was read off the running game, not carried over
    /// from the reference distribution. The reference declares the same fields in the same ORDER,
    /// but its numbers belong to an older build: there <c>LocalPlayer</c> and <c>EntityList</c> sit
    /// 8 bytes apart, on this client they are 0xB8 apart. See docs/api/parity-measured.md.
    /// </para>
    /// <para>
    /// How each number was established. The reference distribution reads this very process
    /// correctly, so it was asked for the true address of every sub-object (tools/RefLive) and
    /// tools/FindOffset was told to locate that address inside this object. Every offset below
    /// matched in exactly one place, and reproduced across three different zones — each time with a
    /// different base address for this object and different values in its fields.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct IngameDataOffsets
    {
        // ── MEASURED ────────────────────────────────────────────────────────────────────────────
        // Confirmed by a unique match in a 0x1000-byte window, repeated in three zones.

        /// <summary>Pointer to the WorldArea record of the area the character stands in.</summary>
        [FieldOffset(0xB0)] public long CurrentArea;

        /// <summary>
        /// Monster level of the current area. One byte, as in the reference: the value is capped
        /// around 100, and the three bytes above it were zero in every zone measured.
        /// </summary>
        [FieldOffset(0xD4)] public byte CurrentAreaLevel;

        /// <summary>Instance hash of the current area. A new number on every zone in.</summary>
        [FieldOffset(0x114)] public uint CurrentAreaHash;

        /// <summary>Pointer to the server-side view of the character (stash, inventories, party).</summary>
        [FieldOffset(0x968)] public long ServerData;

        /// <summary>Pointer to the Entity of the local character.</summary>
        [FieldOffset(0x970)] public long LocalPlayer;

        /// <summary>Pointer to the list of entities the client currently simulates.</summary>
        [FieldOffset(0xA28)] public long EntityList;

        /// <summary>
        /// Number of entities in <see cref="EntityList"/>. Volatile by nature — it changes several
        /// times a second, which is why it was confirmed from a window dump rather than by
        /// searching for a value the reference had reported a moment earlier.
        /// </summary>
        [FieldOffset(0xA30)] public long EntitiesCount;

        /// <summary>Pointer to the list of entities outside simulation range.</summary>
        [FieldOffset(0xA38)] public long SleepingEntityList;

        /// <summary>Number of entities in <see cref="SleepingEntityList"/>.</summary>
        [FieldOffset(0xA40)] public long SleepingEntityCount;

        /// <summary>
        /// Stats of the current map. Confirmed by CONTENT, which is the strongest check available
        /// here: the reference reported eleven (stat, value) pairs, and the array this field points
        /// at holds exactly those eleven pairs, in that order. The earlier guess — the 48-byte run
        /// of zeroes between CurrentAreaLevel and CurrentAreaHash — was refuted by the same
        /// measurement. Measurable only while the character is inside a map; elsewhere the set is
        /// empty and there is nothing to search for.
        /// </summary>
        [FieldOffset(0x128)] public NativePtrArray MapStats;

        /// <summary>Walkable-terrain description of the current area.</summary>
        [FieldOffset(0xC08)] public TerrainData Terrain;

        // ── NOT MEASURED ────────────────────────────────────────────────────────────────────────
        // Numbers below are leftovers from an older build, kept only so that the consumers in
        // Core/ keep compiling. They are NOT a claim about this client.
        //
        // LabDataPtr could not be measured, and for a stated reason rather than for lack of trying:
        // the character was not in the Labyrinth, so the reference reported null and there was no
        // address to search for. The reference declares this field immediately before CurrentArea;
        // at the matching place here (0xA8) the memory is not a pointer at all, so even the order
        // hypothesis does not hold and no number is proposed.
        //
        // To close it: enter the Labyrinth, then follow the usual three steps — ask tools/RefLive
        // for the true address, locate it with tools/FindOffset, confirm by content.

        /// <summary>NOT MEASURED on this build. See the remark above.</summary>
        [FieldOffset(0x11C)] public long LabDataPtr;
    }
}
