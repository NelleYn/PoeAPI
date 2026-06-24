using System;
using System.Runtime.CompilerServices;

namespace ExileCore.Shared.Attributes;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public class SubmenuAttribute : Attribute
{
    public bool CollapsedByDefault { get; set; }
    public bool EnableSelfDrawCollapsing { get; set; }
    public bool EnableCollapsing { get; set; }
    public string RenderMethod { get; set; }

    public SubmenuAttribute()
    {
        _ = 0;
        _ = 0;
        _ = 1;
    }
}