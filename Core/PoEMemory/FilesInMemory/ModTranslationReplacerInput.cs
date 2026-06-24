using ExileCore.Shared.Enums;

namespace ExileCore.PoEMemory.FilesInMemory;

/// <summary>Input to a mod-translation replacer: a stat with its raw and converted values.</summary>
public record ModTranslationReplacerInput(GameStat Stat, int RawValue, float ConvertedValue, string ConvertedValueString);
