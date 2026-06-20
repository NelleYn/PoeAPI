using System.Collections.Generic;
using ExileCore.PoEMemory.FilesInMemory.Sanctum;
using GameOffsets.Native;

namespace ExileCore.PoEMemory.Elements.Sanctum;
public class SanctumFloorData : RemoteMemoryObject
{
    public NativePtrArray RoomDataArray => (NativePtrArray)(this + 24);

    public List<SanctumRoomData> RoomData
    {
        get
        {
            //IL_0004: Unknown result type (might be due to invalid IL or missing references)
            //IL_0008: Unknown result type (might be due to invalid IL or missing references)
            _ = this + 8;
            _ = this + 8;
            _ = 112;
            return null;
        }
    }

    public byte[][][] RoomLayout
    {
        get
        {
            _ = 32;
            return (byte[][][])(object)this;
        }
    }

    public List<SanctumDeferredReward> Rewards
    {
        get
        {
            //IL_0006: Unknown result type (might be due to invalid IL or missing references)
            //IL_0010: Expected O, but got I4
            _ = this + 104;
            while (this != null)
            {
            }

            return (List<SanctumDeferredReward>)16;
        }
    }

    public List<byte> RoomChoices
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public short CurrentResolve => (short)(this + 80);
    public short MaxResolve => (short)(this + 82);
    public short Inspiration => (short)(this + 84);
    public int Gold => this + 72;
}