// EXPERIMENTAL candidate ported from exApiTools/ItemFilter — see proposals/ItemFilterLibrary/README.md. Not part of the build.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.CustomTypeProviders;
using ExileCore.Shared.Enums;

namespace ExileCore.ItemFilterLibrary;

/// <summary>
/// Dynamic LINQ custom type provider that exposes this fork's engine enums (e.g.
/// <see cref="ItemRarity"/>, <see cref="GameStat"/>) to rule expressions. Combined with
/// <c>ParsingConfig.ResolveTypesBySimpleName = true</c>, it lets a rule name an enum by its short
/// name — <c>Rarity == ItemRarity.Unique</c> — instead of a fully-qualified type.
///
/// Ported from <c>exApiTools/ItemFilter/CustomDynamicLinqCustomTypeProvider.cs</c>. It registers
/// every public enum from the ExileCore assembly so any engine enum is usable in a rule, on top of
/// the Dynamic LINQ defaults.
///
/// NOTE: the exact base class / constructor of the Dynamic LINQ default provider is
/// package-version-sensitive; this targets <c>System.Linq.Dynamic.Core</c> 1.3.5 (see README).
/// </summary>
public class CustomDynamicLinqCustomTypeProvider : DefaultDynamicLinqCustomTypeProvider
{
    private HashSet<Type> _customTypes;

    public CustomDynamicLinqCustomTypeProvider(ParsingConfig config, bool cacheCustomTypes = true)
        : base(config, cacheCustomTypes)
    {
    }

    public override HashSet<Type> GetCustomTypes()
    {
        if (_customTypes != null)
            return _customTypes;

        _customTypes = new HashSet<Type>(base.GetCustomTypes());

        foreach (var type in GetEngineEnumTypes())
            _customTypes.Add(type);

        return _customTypes;
    }

    // The engine-enum scan is process-wide and immutable, so compute it once instead of
    // reflecting over the whole ExileCore assembly every time a rule is compiled.
    private static Type[] _engineEnumTypes;

    /// <summary>Every public enum in the ExileCore assembly (ItemRarity, GameStat, EntityType, …).</summary>
    private static Type[] GetEngineEnumTypes()
    {
        if (_engineEnumTypes != null)
            return _engineEnumTypes;

        var assembly = typeof(ItemRarity).Assembly; // ExileCore.dll

        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (System.Reflection.ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t != null).ToArray();
        }

        // IsVisible (not IsPublic) so nested public enums declared inside a public class are included too.
        return _engineEnumTypes = types.Where(t => t != null && t.IsEnum && t.IsVisible).ToArray();
    }
}
