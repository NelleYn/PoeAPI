using System;

namespace ExileCore.Shared.Enums
{
    [Flags]
    public enum InventoryTabPermissions : uint
    {
        Add = 2,
        None = 0,
        Remove = 4,
        View = 1
    }

    public enum InventoryTabType : uint
    {
        Currency = 3,
        Divination = 6,
        Essence = 8,
        Fragment = 9,
        Map = 5,
        Normal = 0,
        Premium = 1,
        Quad = 7,
        // Values 2 and 4 are stash tab types read directly from the game's raw TabType byte
        // (see ServerStashTabOffsets.TabType) but their real names are not confidently known.
        // Do not guess: renaming these incorrectly would silently mislabel a live stash tab type
        // for API consumers. Confirm against a live client (or corroborating GGPK/community
        // reverse-engineering data) before renaming.
        Todo2 = 2,
        Todo4 = 4
    }

    [Flags]
    public enum InventoryTabFlags : byte
    {
        Hidden = 0x80,
        MapSeries = 0x40,
        Premium = 4,
        Public = 0x20,
        RemoveOnly = 1,
        Unknown1 = 0x10,
        Unknown2 = 2,
        Unknown3 = 8
    }
}
