using System.Runtime.CompilerServices;
using System.Text;

namespace ExileCore.PoEMemory.Components;
public record struct ArmourStatRange(int Min, int Max)
{
    public unsafe int Min { get; set; }
    public unsafe int Max { get; set; }

    [CompilerGenerated]
    public unsafe readonly void Deconstruct(out int Min, out int Max)
    {
        Min = (int)Unsafe.AsPointer(ref this);
        Max = (int)Unsafe.AsPointer(ref this);
    }
}