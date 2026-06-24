using System;
using System.Numerics;

namespace ExileCore.Shared;
internal static class SpanExtensions
{
    public static T Average<T>(this Span<T> span)
        where T : INumber<T>
    {
        if (span.IsEmpty)
        {
            return T.Zero;
        }

        T zero = T.Zero;
        Span<T> span2 = span;
        for (int i = 0; i < span2.Length; i++)
        {
            T val = span2[i];
            zero += val;
        }

        return zero / T.CreateChecked(span.Length);
    }

    public static T Min<T>(this Span<T> span)
        where T : INumber<T>
    {
        if (span.IsEmpty)
        {
            return T.Zero;
        }

        T val = span[0];
        Span<T> span2 = span.Slice(1, span.Length - 1);
        for (int i = 0; i < span2.Length; i++)
        {
            T x = span2[i];
            val = T.Min(x, val);
        }

        return val;
    }

    public static T Max<T>(this Span<T> span)
        where T : INumber<T>
    {
        if (span.IsEmpty)
        {
            return T.Zero;
        }

        T val = span[0];
        Span<T> span2 = span.Slice(1, span.Length - 1);
        for (int i = 0; i < span2.Length; i++)
        {
            T x = span2[i];
            val = T.Max(x, val);
        }

        return val;
    }
}