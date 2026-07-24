using System;
using System.Collections.Generic;

namespace ExileCore.Shared.Helpers.MoreLinq
{
    /// <summary>
    /// Re-exports MoreLINQ's <c>Pairwise</c> under the namespace ExileApi-Compiled plugins import
    /// (<c>ExileCore.Shared.Helpers.MoreLinq</c>), so ported call sites compile without the plugin
    /// itself taking a MoreLINQ package reference — <c>ExileCore</c> already carries one
    /// (<c>morelinq</c> 3.2.0, <c>Core/Core.csproj</c>).
    /// </summary>
    public static class PairwiseExtension
    {
        /// <summary>
        /// Projects each pair of <i>adjacent</i> elements of a sequence into a single result.
        /// </summary>
        /// <typeparam name="TSource">The element type of the source sequence.</typeparam>
        /// <typeparam name="TResult">The result type produced for each adjacent pair.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="resultSelector">
        /// Receives each adjacent pair in order (previous element, current element).
        /// </param>
        /// <returns>
        /// A lazily-evaluated sequence with one element per adjacent pair — that is, one element
        /// fewer than <paramref name="source"/>. A sequence of zero or one element yields nothing.
        /// </returns>
        /// <remarks>
        /// This forwards to <c>MoreLinq.MoreEnumerable.Pairwise</c>, so behaviour (including lazy
        /// evaluation and argument-null validation) is MoreLINQ's, not a reimplementation. The call is
        /// written with a <c>global::</c> qualifier because this namespace is itself named
        /// <c>MoreLinq</c>, which would otherwise shadow the package's root namespace here.
        /// </remarks>
        public static IEnumerable<TResult> Pairwise<TSource, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, TSource, TResult> resultSelector)
        {
            return global::MoreLinq.MoreEnumerable.Pairwise(source, resultSelector);
        }
    }
}
