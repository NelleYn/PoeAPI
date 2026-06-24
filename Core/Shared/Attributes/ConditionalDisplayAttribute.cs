using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ExileCore.Shared.Attributes;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public class ConditionalDisplayAttribute : Attribute
{
    public string ConditionMethodName
    {
        [CompilerGenerated]
        get
        {
            return (string)(object)this;
        }
    }

    public bool ComparisonValue
    {
        [CompilerGenerated]
        get
        {
            //IL_0002: Expected I4, but got O
            return (byte)(int)this != 0;
        }
    }

    public ConditionalDisplayAttribute([NotNull] string conditionMethodName, bool comparisonValue = true)
    {
    }
}