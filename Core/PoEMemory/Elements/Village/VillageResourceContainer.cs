using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ExileCore.PoEMemory.FilesInMemory.Village;
using GameOffsets;

namespace ExileCore.PoEMemory.Elements.Village;
public class VillageResourceContainer : StructuredRemoteMemoryObject<VillageResourceContainer.VillageResourceContainerOffsets>
{
    [StructLayout(LayoutKind.Explicit)]
    public struct VillageResourceContainerOffsets
    {
        [FieldOffset(0)]
        public int SnapshotTimestamp;
        [FieldOffset(8)]
        public int Gold;
        [FieldOffset(166)]
        public Buffer3<(int CompletedWork, int TotalWork)> ShipmentWork;
        [FieldOffset(190)]
        public Buffer3<(int CompletedWork, int TotalWork, int A, int B, int C, short D, short E)> MappingWork;
        [FieldOffset(262)]
        public int CompletedDisenchantmentWork;
        [FieldOffset(266)]
        public int TotalDisenchantmentWork;
    }

    public Dictionary<VillageResource, int> Resources
    {
        get
        {
            //IL_0005: Unknown result type (might be due to invalid IL or missing references)
            _ = this + 16;
            while (this != null)
            {
            }

            while (this != null)
            {
            }

            return (Dictionary<VillageResource, int>)(object)this;
        }
    }

    public List<int> RawRemainingShipmentTimes
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<int> ActiveShipmentIndices
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<int> ActiveMappingIndices
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public int RawRemainingDisenchantmentWork
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public int Gold => (int)this;
    public DateTimeOffset SnapshotTimestamp => (DateTimeOffset)(long)this;
}