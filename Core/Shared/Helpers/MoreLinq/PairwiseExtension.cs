using System;
using System.Collections.Generic;
using MoreLinq;

namespace ExileCore.Shared.Helpers.MoreLinq;
public static class PairwiseExtension
{
    public static IEnumerable<TResult> Pairwise<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TSource, TResult> resultSelector)
    {
        return MoreEnumerable.Pairwise(source, resultSelector);
    }
}