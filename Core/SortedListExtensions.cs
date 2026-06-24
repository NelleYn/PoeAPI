using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace ExileCore;
public static class SortedListExtensions
{
    private static readonly Func<SortedList<long, byte[]>, long[]> KeysGetter;
    private static readonly Func<SortedList<long, byte[]>, int> SizeGetter;
    private static readonly Func<SortedList<long, byte[]>, IComparer<long>> ComparerGetter;
    public static bool TryGetLowerBound(this SortedList<long, byte[]> list, long key, [NotNullWhen(true)] out long lowerBoundKey, [NotNullWhen(true)] out byte[] lowerBoundValue)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    private static Func<TSource, TResult> GetGetter<TSource, TResult>(FieldInfo field)
    {
        ParameterExpression parameterExpression = Expression.Parameter(typeof(TSource));
        return Expression.Lambda<Func<TSource, TResult>>(Expression.Field(parameterExpression, field), new ParameterExpression[1] { parameterExpression }).Compile();
    }

    static SortedListExtensions()
    {
        _ = typeof(SortedList<long, byte[]>).TypeHandle;
        _ = 36;
        _ = typeof(SortedList<long, byte[]>).TypeHandle;
        _ = 36;
        _ = typeof(SortedList<long, byte[]>).TypeHandle;
        _ = 36;
    }
}