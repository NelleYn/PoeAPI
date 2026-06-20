using System;
using ExileCore.PoEMemory.Models;

namespace ExileCore.PoEMemory.MemoryObjects;
public class PlacedCurrencyExchangeOrder : RemoteMemoryObject
{
    public DateTimeOffset CreationDate => (DateTimeOffset)(long)this;
    public int PlayerOrderId => this + 40;
    public uint OfferedItemHash => (uint)(this + 44);
    public BaseItemType OfferedItemType => (BaseItemType)(object)this;
    public uint WantedItemHash => (uint)(this + 48);
    public BaseItemType WantedItemType => (BaseItemType)(object)this;
    public int OriginalOfferedItemStackSize => this + 52;
    public int OfferedItemStackSize => this + 56;
    public int WantedItemStackSize => this + 60;
    public int GoldCost => this + 64;
    public int OfferedItemRatioPart => this + 72;
    public int WantedItemRatioPart => this + 74;
    public int CompetingOfferedItemRatioPart => this + 76;
    public int CompetingWantedItemRatioPart => this + 78;
    public bool IsCompleted => (nint)this > 0;
    public bool IsCanceled => (nint)this == 3;
    private byte State => (byte)(this + 80);

    public override string ToString()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}