using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using ExileCore.Shared.Enums;

namespace ExileCore.PoEMemory.FilesInMemory;
public record CachedStatDescription
{
    public List<GameStat> Stats { get; init; }
    public List<CachedStatDescriptionSection> Sections { get; init; }

    public CachedStatDescription(List<GameStat> Stats, List<CachedStatDescriptionSection> Sections)
    {
    }

    [CompilerGenerated]
    public void Deconstruct(out List<GameStat> Stats, out List<CachedStatDescriptionSection> Sections)
    {
        Stats = (List<GameStat>)(object)this;
        Sections = (List<CachedStatDescriptionSection>)(object)this;
    }
}