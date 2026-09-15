using System.Runtime.InteropServices;
using GameOffsets.Native;

namespace GameOffsets
{
    /// <summary>
    /// Layout of the object behind <see cref="IngameStateOffsets.Data"/> on the installed client.
    /// </summary>
    /// <remarks>
    /// <para>
    /// HOW THESE NUMBERS WERE ESTABLISHED. The reference distribution reads this very process
    /// correctly, so it was asked for the true address of every sub-object (tools/RefLive prints
    /// them), and tools/FindOffset was told to locate that address inside this object. The raw
    /// measurements are kept in docs/api/ingamedata-measured.md, with the zone and the base address
    /// each one came from, so that any claim here can be checked rather than believed.
    /// tools/measure-ingamedata.sh reproduces the whole table in one command.
    /// </para>
    /// <para>
    /// HOW STRONG EACH CLAIM IS, honestly. A search reports a unique match only within the window it
    /// was given, and "unique" gets weaker as the window grows: in a 0x2000 window the player
    /// pointer is found TWICE (see <see cref="LocalPlayer"/>), and a one-byte value like the area
    /// level has seven candidates in a 0x10000 window. So uniqueness is never the whole argument
    /// here. What carries the weight is repetition across zones — the base address and the field
    /// values change, the offset does not — and identification by content:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// Three zones (levels 67, 60, 83): <see cref="CurrentAreaLevel"/>, <see cref="CurrentAreaHash"/>.
    /// </description></item>
    /// <item><description>
    /// Two zones (levels 60, 83): <see cref="CurrentArea"/>, <see cref="ServerData"/>,
    /// <see cref="LocalPlayer"/>, <see cref="EntityList"/>, <see cref="EntitiesCount"/>,
    /// <see cref="SleepingEntityList"/>, <see cref="Terrain"/> and all of its members.
    /// </description></item>
    /// <item><description>
    /// One zone, but confirmed by content: <see cref="MapStats"/>. One zone only:
    /// <see cref="SleepingEntityCount"/>, <see cref="EnvironmentData"/>.
    /// </description></item>
    /// </list>
    /// <para>
    /// WHY THE REFERENCE'S OWN NUMBERS ARE NOT USED. Its IngameDataOffsets declares
    /// <c>LocalPlayer</c> and <c>EntityList</c> 8 bytes apart; here they are 0xB8 apart. What does
    /// transfer is the set of fields and their ORDER, which held for every field measured, so the
    /// reference generates hypotheses and never values. See docs/api/parity-measured.md.
    /// </para>
    /// <para>
    /// WHAT THIS METHOD CANNOT SHOW. RefLive executes the reference's code against the same
    /// process, so everything below proves "this fork reads what the reference reads", not "this is
    /// the field layout of the game's own struct". The two coincide as long as the reference is
    /// right, which is the working assumption of this whole repository.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct IngameDataOffsets
    {
        // ── MEASURED ────────────────────────────────────────────────────────────────────────────

        /// <summary>Pointer to the WorldArea record of the area the character stands in.</summary>
        [FieldOffset(0xB0)] public long CurrentArea;

        /// <summary>
        /// Monster level of the current area. One byte, as in the reference: the value is capped
        /// around 100, and the three bytes above it were zero in every zone measured. Seen as 67,
        /// 60 and 83 in three different zones, always at this offset.
        /// </summary>
        [FieldOffset(0xD4)] public byte CurrentAreaLevel;

        /// <summary>Instance hash of the current area. A new number on every zone in.</summary>
        [FieldOffset(0x114)] public uint CurrentAreaHash;

        /// <summary>
        /// Stats of the current map, as 8-byte (stat, value) pairs. Confirmed by CONTENT, which is
        /// the strongest check available here: the reference reported eleven pairs, and the array
        /// this field points at holds exactly those eleven pairs, in that order, and ends exactly
        /// after them. The earlier guess for this field — the 48-byte run of zeroes between
        /// CurrentAreaLevel and CurrentAreaHash — was refuted by that same measurement. Measurable
        /// only while the character is inside a map; elsewhere the set is empty and there is
        /// nothing to search for.
        /// </summary>
        [FieldOffset(0x128)] public NativePtrArray MapStats;

        /// <summary>
        /// Pointer to the server-side view of the character (stash, inventories, party). It lives
        /// here and NOT in IngameState: the true address does not occur anywhere in the first
        /// 0x2000 bytes of that object. See the note in IngameState's constructor.
        /// </summary>
        [FieldOffset(0x968)] public long ServerData;

        /// <summary>
        /// Pointer to the Entity of the local character.
        /// </summary>
        /// <remarks>
        /// Two places in this object hold that pointer: 0x970 and 0x10E8. This one is chosen
        /// because it is exactly <see cref="ServerData"/> + 8, reproducing both the order AND the
        /// spacing the reference declares for these two fields, while 0x10E8 sits in an unrelated
        /// run of floats and counters. That is an argument from structure, not a measurement: the
        /// two candidates would be told apart properly by a state in which they disagree, such as
        /// a loading screen or character select, where one of them should go null first.
        /// </remarks>
        [FieldOffset(0x970)] public long LocalPlayer;

        /// <summary>Pointer to the list of entities the client currently simulates.</summary>
        [FieldOffset(0xA28)] public long EntityList;

        /// <summary>
        /// Number of entities in <see cref="EntityList"/>. Cross-checked end to end rather than by
        /// search alone: tools/SanityRead walks the list itself and arrives at the same count.
        /// </summary>
        [FieldOffset(0xA30)] public long EntitiesCount;

        /// <summary>Pointer to the list of entities outside simulation range.</summary>
        [FieldOffset(0xA38)] public long SleepingEntityList;

        /// <summary>Number of entities in <see cref="SleepingEntityList"/>.</summary>
        [FieldOffset(0xA40)] public long SleepingEntityCount;

        /// <summary>Walkable-terrain description of the current area.</summary>
        [FieldOffset(0xC08)] public TerrainData Terrain;

        /// <summary>
        /// Pointer to the environment description of the area. Measured as a unique match in a
        /// 0x2000 window, but not read by anything in Core/ yet; it is declared so that the map of
        /// this object is complete, and because an earlier 0x1000 window made this field look
        /// absent when it was merely out of frame.
        /// </summary>
        [FieldOffset(0x1110)] public long EnvironmentData;

        // ── NOT MEASURED ────────────────────────────────────────────────────────────────────────
        // LabDataPtr is a leftover from an older build, kept only so that the consumer in Core/
        // keeps compiling. It is NOT a claim about this client, and it is worse than merely
        // unverified: 0x11C is not even 8-aligned, while every offset measured on this client is,
        // and the bytes there currently read as 0x92CFB39000000021 — neither zero nor a pointer. A
        // "== 0" test therefore protects nobody, which is why IngameData.LabyrinthData now demands
        // a canonically shaped pointer before it builds anything.
        //
        // Why it could not be measured: the character was not in the Labyrinth, so the reference
        // reported null and there was no address to search for. The order hypothesis does not help
        // either — the reference declares this field immediately before CurrentArea, and the
        // matching place here (0xA8) holds UTF-16 text rather than a pointer.
        //
        // How to close it: enter the Labyrinth and measure by DIFFERENCE rather than by oracle.
        // Dump this object's first 0xB0 bytes before and after entering, and look for the qword
        // that turns from zero into a pointer. The head of the object is currently zero from 0x30
        // to 0x88, which is exactly the shape of a field that is empty outside the Labyrinth.

        /// <summary>NOT MEASURED on this build. See the remark above.</summary>
        [FieldOffset(0x11C)] public long LabDataPtr;
    }
}
