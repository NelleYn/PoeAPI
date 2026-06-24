using System.Runtime.CompilerServices;
using System.Text;
using ExileCore.Shared.Cache;

namespace ExileCore.PoEMemory.FilesInMemory;
public class UniqueItemDescription : RemoteMemoryObject
{
    private record struct DataStruct(long NamePtr, long _, long VisualPtr)
    {
        public unsafe long NamePtr
        {
            [CompilerGenerated]
            readonly get
            {
                return (nint)Unsafe.AsPointer(ref this);
            }

            [CompilerGenerated]
            set
            {
            }
        }

        public unsafe long _
        {
            [CompilerGenerated]
            readonly get
            {
                return (nint)Unsafe.AsPointer(ref this);
            }

            [CompilerGenerated]
            set
            {
            }
        }

        public unsafe long VisualPtr
        {
            [CompilerGenerated]
            readonly get
            {
                return (nint)Unsafe.AsPointer(ref this);
            }

            [CompilerGenerated]
            set
            {
            }
        }

        [CompilerGenerated]
        public override readonly string ToString()
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }

        [CompilerGenerated]
        private readonly bool PrintMembers(StringBuilder builder)
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }

        [CompilerGenerated]
        public unsafe override readonly int GetHashCode()
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }

        [CompilerGenerated]
        public readonly bool Equals(DataStruct other)
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }

        [CompilerGenerated]
        public unsafe readonly void Deconstruct(out long NamePtr, out long _, out long VisualPtr)
        {
            NamePtr = (nint)Unsafe.AsPointer(ref this);
            _ = (nint)Unsafe.AsPointer(ref this);
            VisualPtr = (nint)Unsafe.AsPointer(ref this);
        }
    }

    private ItemVisualIdentity _itemVisualIdentity;
    private WordEntry _uniqueName;
    private readonly CachedValue<DataStruct> _data;
    public ItemVisualIdentity ItemVisualIdentity
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public WordEntry UniqueName
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public override string ToString()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}