using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ExileCore.PoEMemory.FilesInMemory;
public record CachedStatDescriptionSection
{
    public List<(int Min, int Max)> StatRanges { get; init; }
    public List<StatHandling> StatConversionTypes { get; init; }
    public string String { get; init; }

    public CachedStatDescriptionSection(List<(int Min, int Max)> StatRanges, List<StatHandling> StatConversionTypes, string String)
    {
    }

    [CompilerGenerated]
    public void Deconstruct(out List<(int Min, int Max)> StatRanges, out List<StatHandling> StatConversionTypes, out string String)
    {
        StatRanges = (List<(int Min, int Max)>)(object)this;
        StatConversionTypes = (List<StatHandling>)(object)this;
        String = (string)(object)this;
    }
}