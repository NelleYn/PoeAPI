using System;
using System.Runtime.CompilerServices;
using System.Text;
using ExileCore.PoEMemory.FilesInMemory.Ancestor;

namespace ExileCore.PoEMemory.MemoryObjects;
public record AncestorFightOptionReward
{
    public AncestralTrialTribe FavorTribe { get; init; }
    public int FavorAmount { get; init; }

    public AncestorFightOptionReward(AncestralTrialTribe FavorTribe, int FavorAmount)
    {
    }

    [CompilerGenerated]
    public void Deconstruct(out AncestralTrialTribe FavorTribe, out int FavorAmount)
    {
        //IL_0006: Expected I4, but got O
        FavorTribe = (AncestralTrialTribe)(object)this;
        FavorAmount = (int)this;
    }
}