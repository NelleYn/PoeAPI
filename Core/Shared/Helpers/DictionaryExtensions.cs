using System.Collections.Generic;
using System.Linq;

namespace ExileCore.Shared.Helpers;

/// <summary>
/// Provides extension methods for merging dictionaries.
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    /// Returns a new dictionary of this ... others merged leftward.
    /// Keeps the type of 'this', which must be default-instantiable.
    /// Example:
    ///   result = map.MergeLeft(other1, other2, ...)
    /// </summary>
    /// <remarks>Taken from: https://stackoverflow.com/a/2679857</remarks>
    /// <param name="me">The base dictionary whose entries are merged first.</param>
    /// <param name="others">The additional dictionaries to merge, overriding earlier values on key collisions.</param>
    /// <returns>A new dictionary containing the merged entries.</returns>
    public static T MergeLeft<T, TK, TV>(this T me, params IDictionary<TK, TV>[] others) where T : IDictionary<TK, TV>, new()
    {
        var newMap = new T();

        foreach (var src in new List<IDictionary<TK, TV>> {me}.Concat(others))

            // ^-- echk. Not quite there type-system.
        foreach (var p in src)
        {
            newMap[p.Key] = p.Value;
        }

        return newMap;
    }
}
