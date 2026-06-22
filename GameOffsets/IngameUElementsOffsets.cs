using System.Runtime.InteropServices;

namespace GameOffsets
{
    // Verified against client 328.8 via an in-process Marshal.OffsetOf dump (the dump names this
    // struct IngameUIElementsOffsets). Fields marked UNVERIFIED are not present in the 328.8 dump
    // and keep their previous offsets.
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct IngameUElementsOffsets
    {
        [FieldOffset(0x2E8)] public long GetQuests;
        [FieldOffset(0x320)] public long GameUI;
        [FieldOffset(0x470)] public long Mouse;
        [FieldOffset(0x458)] public long HiddenSkillBar;
        [FieldOffset(0x478)] public long SkillBar;
        [FieldOffset(0x558)] public long QuestTracker;
        [FieldOffset(0x5E0)] public long OpenLeftPanel;
        [FieldOffset(0x5E8)] public long OpenRightPanel;
        [FieldOffset(0x610)] public long InventoryPanel;
        [FieldOffset(0x618)] public long StashElement;
        [FieldOffset(0x638)] public long TreePanel;
        [FieldOffset(0x648)] public long AtlasPanel;
        [FieldOffset(0x6C8)] public long Map;
        [FieldOffset(0x6D0)] public long itemsOnGroundLabelRoot;
        [FieldOffset(0x7A0)] public long PurchaseWindow;
        [FieldOffset(0x7B0)] public long SellWindow;
        [FieldOffset(0x688)] public long MapDeviceWindow; // UNVERIFIED: not in 328.8 dump
        [FieldOffset(0x820)] public long IncursionWindow;
        [FieldOffset(0x840)] public long DelveWindow;
        [FieldOffset(0x858)] public long BetrayalWindow;
        [FieldOffset(0x850)] public long ZanaMissionChoice;
        [FieldOffset(0x868)] public long CraftBenchWindow;
        [FieldOffset(0x870)] public long UnveilWindow;
        [FieldOffset(0x768)] public long SynthesisWindow; // UNVERIFIED: not in 328.8 dump
        [FieldOffset(0x870)] public long MetamorphWindow;
        [FieldOffset(0xA60)] public long AreaInstanceUi;
        [FieldOffset(0xC00)] public long GemLvlUpPanel;
        [FieldOffset(0xBB0)] public long InvitesPanel;
        [FieldOffset(0xD40)] public long ItemOnGroundTooltip;
        [FieldOffset(0x680)] public long WorldMap;
        [FieldOffset(0x0)] public long MapTabWindowStartPtr; // UNVERIFIED: not in 328.8 dump
    }
}
