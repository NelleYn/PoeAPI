// Partial extension that restores a nested type missing from the modernized source.
using ExileCore.PoEMemory.MemoryObjects;

namespace ExileCore.PoEMemory.Models;
partial class StatTranslator
{
    private delegate void AddStat(ItemStats stats, ItemMod m);
}