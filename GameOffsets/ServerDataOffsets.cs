using System.Runtime.InteropServices;
using GameOffsets.Native;

namespace GameOffsets
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SkillBarIdsStruct
    {
        public ushort SkillBar1;
        public ushort SkillBar2;
        public ushort SkillBar3;
        public ushort SkillBar4;
        public ushort SkillBar5;
        public ushort SkillBar6;
        public ushort SkillBar7;
        public ushort SkillBar8;
        public ushort SkillBar9;
        public ushort SkillBar10;
        public ushort SkillBar11;
        public ushort SkillBar12;
        public ushort SkillBar13;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct ServerDataOffsets
    {
        /// <summary>
        /// Offset, from ServerData.Address, of the window this struct is blitted from. ServerData
        /// reads the whole struct in ONE call at <c>Address + StructBase</c>, so a
        /// <c>[FieldOffset]</c> below is an offset INSIDE that window, never an offset from Address.
        /// </summary>
        /// <remarks>
        /// <para>
        /// THE CONVENTION EVERY FIELD BELOW FOLLOWS: each one is written as
        /// <c>&lt;offset from Address&gt; - StructBase</c>. The number a reader actually needs - the
        /// offset from Address - therefore stays literally visible in the source, and the
        /// subtraction is nothing but window bookkeeping. Offsets that are NOT part of the window
        /// (see <see cref="ATLAS_REGION_UPGRADES"/>, <see cref="LATENCY"/>) are plain consts and are
        /// stated directly as offsets from Address, with no arithmetic.
        /// </para>
        /// <para>
        /// THE TRAP THIS NAME REPLACES. The constant used to be called <c>Skip</c>, which said what
        /// the read skips but not what the field offsets are relative to, and the ambiguity bit:
        /// <c>Extensions.GetOffset&lt;ServerDataOffsets&gt;(name)</c> returns the WINDOW-relative
        /// number, and of its two callers in ServerData one added StructBase back and the other did
        /// not, so the stash-tab reader was addressing Address + 0x1CB0 for a field this table
        /// declares at Address + 0x6CB0. Anything that turns a name here into an absolute address
        /// MUST add StructBase.
        /// </para>
        /// <para>
        /// WHY THE WINDOW EXISTS AT ALL. Marshalled size of an explicit-layout struct is
        /// (highest field offset + that field's size), and ServerData re-reads the whole struct once
        /// per frame. Starting at 0x5000 rather than 0 keeps that per-frame read near 12 KB instead
        /// of near 32 KB. That is also why a far-flung field belongs in a const and not in here.
        /// </para>
        /// </remarks>
        public const int StructBase = 0x5000;

        // ── Offsets stated directly from ServerData.Address (outside the blitted window) ──────────

        /// <summary>
        /// Base of the per-region atlas upgrade bytes, as an offset from ServerData.Address.
        /// Provenance: inherited from upstream, NOT measured by the 2026-09-16 survey.
        /// </summary>
        public const int ATLAS_REGION_UPGRADES = 0x7782;

        /// <summary>
        /// Round-trip latency to the game server, in milliseconds, as an int32 at
        /// ServerData.Address + this offset.
        /// </summary>
        /// <remarks>
        /// <para>
        /// MEASURED. Live client on 2026-09-16 (PathOfExile_KG pid 13660, module base
        /// 0x7FF78FCD0000, zone "The Reliquary" 1_5_7): the qword at Address + 0xC490 read
        /// 0x000004AA00000003, whose low uint32 is 3, and the reference distribution reported
        /// Latency = 3 at that same moment. The offset it replaces, 0x6CA0, reads 0x128CBA26 there,
        /// which is not a latency under any interpretation of those bytes.
        /// </para>
        /// <para>
        /// HONEST WEAKNESS OF THAT AGREEMENT. Three is a small number, and a small integer will
        /// coincide with plenty of unrelated fields; the agreement on its own is weak evidence and
        /// is not claimed as more. What carries the weight is the pair of facts around it: the
        /// offset came out of an independent survey of the region rather than out of scanning for a
        /// value that matched, and the number being replaced reads demonstrable nonsense. So the
        /// claim here is not "confirmed offset". It is "measured candidate, replacing a known-wrong
        /// one". A second reading at a visibly different latency would settle it; that has not been
        /// taken yet.
        /// </para>
        /// <para>
        /// WHY A CONST AND NOT A [FieldOffset] IN THIS STRUCT. 0xC490 lies far outside the blitted
        /// window. Declaring it as a field would grow the struct from ~0x2F12 to ~0x7494 bytes, and
        /// ServerData re-reads the whole struct EVERY FRAME - so one 4-byte value would have cost
        /// roughly 18 KB of extra per-frame traffic. Worse, a read that long is likelier to run off
        /// the end of the mapped region, and a failed read returns default for the WHOLE struct,
        /// i.e. every other ServerData field would silently go to zero at once.
        /// </para>
        /// </remarks>
        public const int LATENCY = 0xC490;

        // ── Fields of the blitted window: "<offset from Address> - StructBase" ───────────────────

        [FieldOffset(0x6E98 - StructBase)] public NativeStringU League;
        [FieldOffset(0x6E20 - StructBase)] public NativePtrArray PassiveSkillIds;
        [FieldOffset(0x6BC0 - StructBase)] public byte PlayerClass;
        [FieldOffset(0x6BC4 - StructBase)] public int CharacterLevel;
        [FieldOffset(0x6BC8 - StructBase)] public int PassiveRefundPointsLeft;
        [FieldOffset(0x6BCC - StructBase)] public int QuestPassiveSkillPoints;
        [FieldOffset(0x63D0 - StructBase)] public int FreePassiveSkillPointsLeft;//Known-stale offset dating to game version 3.8.1, far outside the sibling passive/ascendancy cluster (0x6BC8-0x6BD8); consumed by ServerData.FreePassiveSkillPointsLeft, needs re-verification against a live game before trusting
        [FieldOffset(0x6BD4 - StructBase)] public int TotalAscendencyPoints;
        [FieldOffset(0x6BD8 - StructBase)] public int SpentAscendencyPoints;
        [FieldOffset(0x6DD8 - StructBase)] public byte PartyStatusType;
        [FieldOffset(0x6F00 - StructBase)] public byte NetworkState;
        [FieldOffset(0x6DF8 - StructBase)] public byte PartyAllocationType;
        [FieldOffset(0x6C98 - StructBase)] public float TimeInGame;
        [FieldOffset(0x7168 - StructBase)] public SkillBarIdsStruct SkillBarIds;
        [FieldOffset(0x6CB0 - StructBase)] public NativePtrArray PlayerStashTabs;
        [FieldOffset(0x73E0 - StructBase)] public NativePtrArray GuildStashTabs;
        [FieldOffset(0x71C0 - StructBase)] public NativePtrArray NearestPlayers;
        [FieldOffset(0x72C0 - StructBase)] public NativePtrArray PlayerInventories;
        [FieldOffset(0x7390 - StructBase)] public NativePtrArray NPCInventories;
        [FieldOffset(0x7460 - StructBase)] public NativePtrArray GuildInventories;
        [FieldOffset(0x7270 - StructBase)] public ushort TradeChatChannel;
        [FieldOffset(0x7278 - StructBase)] public ushort GlobalChatChannel;
        [FieldOffset(0x76A0 - StructBase)] public long CompletedMaps;//search for a LONG value equals to your current amount of completed maps. Pointer will be under this offset
        [FieldOffset(0x7660 - StructBase)] public long BonusCompletedAreas;
        [FieldOffset(0x7440 - StructBase)] public long ElderInfluencedAreas;
        [FieldOffset(0)] public long MasterAreas;
        [FieldOffset(0x7660 - StructBase)] public long ElderGuardiansAreas; //Maybe wrong not tested
        [FieldOffset(0x7660 - StructBase)] public long ShapedAreas; //Maybe wrong not tested
        [FieldOffset(0x6E77 - StructBase)] public ushort LastActionId;//Do we need this?
        [FieldOffset(0x7E5C - StructBase)] public byte MonsterLevel;
        [FieldOffset(0x7E5D - StructBase)] public byte MonstersRemaining;
        [FieldOffset(0x7F10 - StructBase)] public ushort CurrentSulphiteAmount; //Maybe wrong not tested
        [FieldOffset(0x7F00 - StructBase)] public int CurrentAzuriteAmount;
    }
}
