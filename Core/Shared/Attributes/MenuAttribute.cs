using System;

namespace ExileCore.Shared.Attributes;

/// <summary>
/// Associates a settings property with a menu entry, optionally specifying its tooltip, ordering index and parent index.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class MenuAttribute : Attribute
{
    public int index = -1;
    public string MenuName = "";
    public int parentIndex = -1;
    public string Tooltip;

    public MenuAttribute(string menuName)
    {
        MenuName = menuName;
    }

    public MenuAttribute(string menuName, string tooltip) : this(menuName)
    {
        Tooltip = tooltip;
    }

    public MenuAttribute(string menuName, int index)
    {
        MenuName = menuName;
        this.index = index;
    }

    public MenuAttribute(string menuName, string tooltip, int index) : this(menuName, index)
    {
        Tooltip = tooltip;
    }

    public MenuAttribute(string menuName, int index, int parentIndex)
    {
        MenuName = menuName;
        this.index = index;
        this.parentIndex = parentIndex;
    }

    public MenuAttribute(string menuName, string tooltip, int index, int parentIndex) : this(menuName, index, parentIndex)
    {
        Tooltip = tooltip;
    }
}

/// <summary>
/// Marks a settings property so that it is not rendered as a menu entry.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class IgnoreMenuAttribute : Attribute
{
}
