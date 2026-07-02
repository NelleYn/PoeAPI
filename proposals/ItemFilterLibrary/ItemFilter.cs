// EXPERIMENTAL candidate ported from exApiTools/ItemFilter — see proposals/ItemFilterLibrary/README.md. Not part of the build.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ExileCore.ItemFilterLibrary;

/// <summary>
/// A compiled set of filter rules loaded from a <c>.ifl</c> file, string, or line list. Each non-blank
/// block of the source (blank lines separate blocks; consecutive lines are concatenated; <c>//</c>
/// starts a comment) becomes one <see cref="ItemQuery"/>. <see cref="Matches"/> evaluates the rules
/// against an <see cref="ItemData"/>.
///
/// Ported from <c>exApiTools/ItemFilter/ItemFilter.cs</c>. Rule semantics (per the cookbook recipe):
/// evaluation stops on the first rule that is <c>true</c> — a positive rule makes the filter match
/// (return <c>true</c>), a <c>^</c>-prefixed rule excludes the item (return <c>false</c>). A rule that
/// failed to compile, or throws at runtime, is skipped — <see cref="Matches"/> never throws.
/// </summary>
public class ItemFilter
{
    private ItemFilter(string name, List<ItemQuery> queries)
    {
        Name = name;
        Queries = queries;
    }

    public string Name { get; }

    public IReadOnlyList<ItemQuery> Queries { get; }

    /// <summary>Reads a <c>.ifl</c> file and compiles its rules.</summary>
    public static ItemFilter LoadFromPath(string filterFilePath)
    {
        var lines = File.ReadAllLines(filterFilePath);
        return LoadFromList(Path.GetFileNameWithoutExtension(filterFilePath), lines);
    }

    /// <summary>Parses and compiles rules from an in-memory string.</summary>
    public static ItemFilter LoadFromString(string filterText)
    {
        var lines = (filterText ?? string.Empty).Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        return LoadFromList("String", lines);
    }

    /// <summary>Compiles rules from an explicit list of source lines.</summary>
    public static ItemFilter LoadFromList(string name, IEnumerable<string> rules)
    {
        return new ItemFilter(name, CompileQueries(rules));
    }

    /// <summary>
    /// Returns true when the item matches this filter. Guards <c>item?.Entity?.IsValid</c> and swallows
    /// per-rule runtime exceptions, so it is safe to call every frame.
    /// </summary>
    public bool Matches(ItemData item)
    {
        if (item?.Entity == null || !item.Entity.IsValid)
            return false;

        foreach (var query in Queries)
        {
            if (query.FailedToCompile != null)
                continue;

            bool result;
            try
            {
                result = query.CompiledQuery(item);
            }
            catch
            {
                continue; // a rule that throws at runtime simply does not match
            }

            if (result)
                return !query.IsNegative; // first hit decides: positive -> match, ^negative -> exclude
        }

        return false;
    }

    private static List<ItemQuery> CompileQueries(IEnumerable<string> rules)
    {
        var queries = new List<ItemQuery>();
        var expression = new List<string>();
        var comments = new List<string>();

        void Flush()
        {
            if (expression.Count > 0)
            {
                var raw = string.Join(" ", expression).Trim();
                var isNegative = false;
                if (raw.StartsWith("^"))
                {
                    isNegative = true;
                    raw = raw.Substring(1).Trim();
                }

                if (raw.Length > 0)
                {
                    var name = comments.Count > 0 ? string.Join(" ", comments).Trim() : raw;
                    queries.Add(ItemQuery.Load(raw, name, isNegative));
                }
            }

            expression.Clear();
            comments.Clear();
        }

        foreach (var line in rules ?? Enumerable.Empty<string>())
        {
            var rawLine = line ?? string.Empty;

            if (string.IsNullOrWhiteSpace(rawLine))
            {
                Flush(); // a blank line separates rules
                continue;
            }

            var stripped = StripComment(rawLine).Trim();
            if (stripped.Length == 0)
            {
                // a whole-line comment: keep as description, does not terminate the block
                comments.Add(rawLine.Trim().TrimStart('/').Trim());
                continue;
            }

            expression.Add(stripped);
        }

        Flush();
        return queries;
    }

    // Strip a trailing "//" comment while respecting double-quoted string literals
    // (including backslash escapes such as \" and \\ inside a string).
    private static string StripComment(string line)
    {
        if (string.IsNullOrEmpty(line))
            return string.Empty;

        var inString = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inString)
            {
                if (c == '\\')
                    i++; // skip the escaped char so \" / \\ don't end the string prematurely
                else if (c == '"')
                    inString = false;
            }
            else if (c == '"')
            {
                inString = true;
            }
            else if (c == '/' && i + 1 < line.Length && line[i + 1] == '/')
            {
                return line.Substring(0, i);
            }
        }

        return line;
    }
}
