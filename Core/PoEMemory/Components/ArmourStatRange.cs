using System.Runtime.CompilerServices;
using System.Text;

namespace ExileCore.PoEMemory.Components;
public record struct ArmourStatRange(int Min, int Max)
{
    public unsafe int Min
    {
        [CompilerGenerated]
        readonly get
        {
            return (int)Unsafe.AsPointer(ref this);
        }

        [CompilerGenerated]
        set
        {
        }
    }

    public unsafe int Max
    {
        [CompilerGenerated]
        readonly get
        {
            return (int)Unsafe.AsPointer(ref this);
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
    public readonly bool Equals(ArmourStatRange other)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    [CompilerGenerated]
    public unsafe readonly void Deconstruct(out int Min, out int Max)
    {
        Min = (int)Unsafe.AsPointer(ref this);
        Max = (int)Unsafe.AsPointer(ref this);
    }
}