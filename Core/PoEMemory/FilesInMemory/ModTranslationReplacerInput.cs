using System;
using System.Runtime.CompilerServices;
using System.Text;
using ExileCore.Shared.Enums;

namespace ExileCore.PoEMemory.FilesInMemory;
public record ModTranslationReplacerInput
{
    public GameStat Stat { get; init; }
    public int RawValue { get; init; }
    public float ConvertedValue { get; init; }
    public string ConvertedValueString { get; init; }

    public ModTranslationReplacerInput(GameStat Stat, int RawValue, float ConvertedValue, string ConvertedValueString)
    {
    }

    [CompilerGenerated]
    public unsafe void Deconstruct(out GameStat Stat, out int RawValue, out float ConvertedValue, out string ConvertedValueString)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}