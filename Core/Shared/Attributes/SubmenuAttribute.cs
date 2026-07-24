using System;

namespace ExileCore.Shared.Attributes;

/// <summary>
/// Marks a settings property — or the class used as a settings property's type — as a nested
/// submenu: instead of being flattened into the parent menu, its own properties are rendered
/// inside a collapsible group.
/// </summary>
/// <remarks>
/// <para>
/// A submenu class does <b>not</b> have to implement <see cref="Interfaces.ISettings"/>; that
/// interface stays reserved for a plugin's root settings object. The attribute may sit either on
/// the property or on the property's type — the property wins if both carry one, which lets one
/// shared settings class be reused with different collapsing defaults.
/// </para>
/// <para>
/// Nested submenus, <c>[Menu]</c> names/tooltips, <see cref="ConditionalDisplayAttribute"/> and
/// <c>[IgnoreMenu]</c> all work inside a submenu exactly as they do at the top level. Properties
/// whose type is not a settings node (plain <c>int</c>, arrays, collections, ...) are skipped
/// silently, so a submenu class may carry its own bookkeeping state.
/// </para>
/// <para>
/// The defaults below (<c>CollapsedByDefault = false</c>, <c>EnableSelfDrawCollapsing = false</c>,
/// <c>EnableCollapsing = true</c>) are the ones the upstream ExileApi-Compiled build uses: its
/// decompiled constructor stores the constants <c>0, 0, 1</c> in that declaration order.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [Submenu(CollapsedByDefault = true)]
/// public class ChestSettings
/// {
///     public ToggleNode ClickChests { get; set; } = new ToggleNode(true);
/// }
///
/// public class MySettings : ISettings
/// {
///     public ChestSettings ChestSettings { get; set; } = new ChestSettings();
///     public ToggleNode Enable { get; set; } = new ToggleNode(false);
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public class SubmenuAttribute : Attribute
{
    /// <summary>
    /// Whether the group starts collapsed. Only meaningful while the group is collapsible, i.e.
    /// when <see cref="EnableCollapsing"/> is set, or when <see cref="RenderMethod"/> is combined
    /// with <see cref="EnableSelfDrawCollapsing"/>.
    /// </summary>
    public bool CollapsedByDefault { get; set; }

    /// <summary>
    /// Whether a <see cref="RenderMethod"/>-driven submenu is still wrapped in a collapsible
    /// header. Without it a self-drawing submenu is rendered bare, with no header of its own —
    /// which is what a submenu that draws a complete UI of its own usually wants.
    /// </summary>
    public bool EnableSelfDrawCollapsing { get; set; }

    /// <summary>
    /// Whether the generated group is collapsible. When <c>false</c> the properties are drawn
    /// under a plain indented label that cannot be folded away.
    /// </summary>
    public bool EnableCollapsing { get; set; } = true;

    /// <summary>
    /// The name of a method on the submenu type that draws the whole submenu itself, replacing
    /// the generated per-property widgets. The method must be parameterless, or take a single
    /// parameter that the owning plugin instance is assignable to (e.g.
    /// <c>void Render(MyPlugin plugin)</c>).
    /// </summary>
    /// <remarks>
    /// Use <c>nameof(...)</c> rather than a string literal so a rename cannot silently turn the
    /// submenu into an error message in the menu.
    /// </remarks>
    public string RenderMethod { get; set; }
}
