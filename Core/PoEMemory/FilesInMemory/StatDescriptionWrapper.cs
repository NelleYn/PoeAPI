using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Interfaces;

namespace ExileCore.PoEMemory.FilesInMemory;
public class StatDescriptionWrapper : UniversalFileWrapper<StatDescription>
{
    private readonly List<CachedStatDescription> _cachedDescriptions;
    private bool _cachedStatDescriptionsAreValid;
    private readonly ConcurrentDictionary<HashSet<GameStat>, List<CachedStatDescription>> _statsByStatSet;
    private List<CachedStatDescription> _descriptionsFromFile;
    public string StaticFileName
    {
        [CompilerGenerated]
        get
        {
            return (string)(object)this;
        }
    }

    protected override long RecordLength => 8L;
    protected override int NumberOfRecords => (object)((object)this - (object)this) / (object)this;

    protected unsafe override int? ArrayPointerStride
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    private List<CachedStatDescription> DescriptionsFromFile
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    private string CacheFilePath => ".json";

    public StatDescriptionWrapper(IMemory mem, Func<string, long> address, string fileName)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    private void UpdateFileDescriptions()
    {
        _ = 1;
        _ = null;
    }

    private bool Intersects(HashSet<GameStat> s1, List<GameStat> s2)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public string TranslateMod(IReadOnlyDictionary<GameStat, int> values, int? sectionIndexOverride, Func<ModTranslationReplacerInput, string> replacer)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public string TranslateMod(IReadOnlyCollection<GameStat> stats, int sectionIndexOverride, Func<GameStat, string> replacer)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public unsafe string TranslateMod(IReadOnlyDictionary<GameStat, int> values)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public string TranslateMod(IReadOnlyDictionary<GameStat, int> values, int? sectionIndexOverride)
    {
        return null;
    }

    protected override void EntryAdded(long addr, StatDescription entry)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    protected override void OnReload()
    {
    }

    [GeneratedRegex("{(?<id>\\d*)(:(?<format>[^}]*))?}", RegexOptions.ExplicitCapture | RegexOptions.Compiled)]
    [GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.7603")]
    private static Regex StringTemplateRegex()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}