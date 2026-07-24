using System;

namespace ExileCore.Shared.Attributes;

/// <summary>
/// Hides a settings property in the menu unless a condition on the declaring settings object
/// evaluates to <see cref="ComparisonValue"/>. The condition is re-evaluated every frame, so the
/// property appears and disappears as the setting it depends on is toggled.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ConditionMethodName"/> is resolved against the type that declares the decorated
/// property and may name any of the following, public or not:
/// </para>
/// <list type="bullet">
///   <item><description>a parameterless method returning <see cref="bool"/>,</description></item>
///   <item><description>a <see cref="bool"/> property or field,</description></item>
///   <item><description>a <see cref="Nodes.ToggleNode"/> property or field (its
///   <see cref="Nodes.ToggleNode.Value"/> is used).</description></item>
/// </list>
/// <para>
/// Hiding is cosmetic only: the property keeps its value and is still persisted, and nothing stops
/// the plugin from reading it while it is hidden.
/// </para>
/// <para>
/// A name that cannot be resolved is reported once, at menu-build time, and the property is then
/// shown unconditionally — a visible setting is a far smaller problem than one that silently
/// vanishes.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public ToggleNode ClickChests { get; set; } = new ToggleNode(true);
///
/// [ConditionalDisplay(nameof(ClickChests))]
/// public RangeNode&lt;int&gt; ChestRadius { get; set; } = new RangeNode&lt;int&gt;(12, 1, 200);
///
/// // shown only while ClickChests is *off*
/// [ConditionalDisplay(nameof(ClickChests), false)]
/// public TextNode WhyNotClickChests { get; set; } = new TextNode("");
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public class ConditionalDisplayAttribute : Attribute
{
    /// <summary>Creates the attribute.</summary>
    /// <param name="conditionMethodName">
    /// The name of the method, property or field to evaluate. Prefer <c>nameof(...)</c>.
    /// </param>
    /// <param name="comparisonValue">The value the condition must have for the property to be shown.</param>
    public ConditionalDisplayAttribute(string conditionMethodName, bool comparisonValue = true)
    {
        ConditionMethodName = conditionMethodName;
        ComparisonValue = comparisonValue;
    }

    /// <summary>The name of the member evaluated to decide whether the property is drawn.</summary>
    public string ConditionMethodName { get; }

    /// <summary>The value the condition must equal for the property to be drawn.</summary>
    public bool ComparisonValue { get; }
}
