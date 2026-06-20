using System;

namespace ExileCore.Shared.Enums;
[Flags]
public enum InventoryTabAffinity : uint
{
    Bit0 = 1u,
    Incubator = 2u,
    Bit2 = 4u,
    Currency = 8u,
    Unique = 0x10u,
    Map = 0x20u,
    DivinationCard = 0x40u,
    Settlers = 0x80u,
    Essence = 0x100u,
    Fragment = 0x200u,
    Sanctum = 0x400u,
    Bit11 = 0x800u,
    Delve = 0x1000u,
    Blight = 0x2000u,
    Ultimatum = 0x4000u,
    Delirium = 0x8000u,
    Flask = 0x20000u,
    Gem = 0x40000u,
    Bit19 = 0x80000u,
    Bit20 = 0x100000u,
    Ritual = 0x200000u
}