// EXPERIMENTAL candidate ported from exApiTools/ItemFilter — see proposals/ItemFilterLibrary/README.md. Not part of the build.

using System;
using System.Linq.Dynamic.Core;

namespace ExileCore.ItemFilterLibrary;

/// <summary>
/// A single compiled filter rule: one Dynamic LINQ boolean expression over an <see cref="ItemData"/>,
/// compiled once into a <see cref="Func{ItemData, Boolean}"/>.
///
/// Ported from <c>exApiTools/ItemFilter/ItemQuery.cs</c>. Compilation failures are non-fatal: a bad
/// rule is logged via <c>DebugWindow.LogError</c>, its message is stored on
/// <see cref="FailedToCompile"/>, and <see cref="CompiledQuery"/> is a constant <c>false</c> — so a
/// broken rule never matches and never throws.
/// </summary>
public class ItemQuery
{
    private ItemQuery()
    {
    }

    /// <summary>The rule expression text (with any leading <c>^</c> already removed).</summary>
    public string RawQuery { get; private set; }

    /// <summary>A human-readable label (the rule's comment lines, or the raw expression).</summary>
    public string Name { get; private set; }

    /// <summary>True when the rule was prefixed with <c>^</c> — a match excludes the item (filter returns false).</summary>
    public bool IsNegative { get; private set; }

    /// <summary>Non-null when compilation failed; holds the error message. Such a rule never matches.</summary>
    public string FailedToCompile { get; private set; }

    /// <summary>The compiled predicate. Defaults to constant <c>false</c> until (and if) compilation succeeds.</summary>
    public Func<ItemData, bool> CompiledQuery { get; private set; } = _ => false;

    public static ItemQuery Load(string rawQuery) => Load(rawQuery, rawQuery, false);

    public static ItemQuery Load(string rawQuery, string name, bool isNegative)
    {
        var query = new ItemQuery
        {
            RawQuery = rawQuery,
            Name = string.IsNullOrWhiteSpace(name) ? rawQuery : name,
            IsNegative = isNegative,
        };

        try
        {
            var parsingConfig = new ParsingConfig
            {
                ResolveTypesBySimpleName = true,
            };
            parsingConfig.CustomTypeProvider = new CustomDynamicLinqCustomTypeProvider(parsingConfig);

            var lambda = DynamicExpressionParser.ParseLambda<ItemData, bool>(parsingConfig, false, rawQuery);
            query.CompiledQuery = lambda.Compile();
        }
        catch (Exception ex)
        {
            query.FailedToCompile = ex.Message;
            query.CompiledQuery = _ => false;
            DebugWindow.LogError($"[ItemFilterLibrary] Failed to compile rule \"{query.Name}\": {ex.Message}");
        }

        return query;
    }
}
