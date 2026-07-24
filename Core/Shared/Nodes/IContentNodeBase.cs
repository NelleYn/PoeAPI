using System;
using System.Collections.Generic;

namespace ExileCore.Shared.Nodes
{
    /// <summary>
    /// The non-generic view of a <see cref="ContentNode{T}"/> that <c>SettingsParser</c> draws
    /// against, so the menu builder does not need to know the item type.
    /// </summary>
    /// <remarks>
    /// Deliberately <c>internal</c>: it exists for the menu builder, not for plugins. Plugins
    /// declare and use the generic <see cref="ContentNode{T}"/> directly.
    /// </remarks>
    internal interface IContentNodeBase
    {
        /// <summary>The current items, in display order.</summary>
        IEnumerable<object> Content { get; }

        /// <summary>Whether the menu draws add/remove controls for the list.</summary>
        bool EnableControls { get; }

        /// <summary>
        /// Appends a new item, or <c>null</c> when the node has no item factory and therefore
        /// cannot create one.
        /// </summary>
        Action SpawnItem { get; }

        /// <summary>Whether each item is drawn inside its own collapsible header.</summary>
        bool EnableItemCollapsing { get; }

        /// <summary>
        /// Whether items are single settings nodes drawn as one inline widget per row, rather than
        /// objects whose properties are expanded into a group.
        /// </summary>
        bool UseFlatItems { get; }

        /// <summary>Removes the given item. Returns whether it was present.</summary>
        bool Remove(object item);
    }
}
