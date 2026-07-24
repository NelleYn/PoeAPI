using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using ImGuiNET;

namespace ExileCore;

/// <summary>
/// The reflective half of the menu builder: nested submenus (<see cref="SubmenuAttribute"/>),
/// conditional visibility (<see cref="ConditionalDisplayAttribute"/>) and list-of-sub-settings
/// nodes (<see cref="ContentNode{T}"/>).
/// </summary>
/// <remarks>
/// These are drawn from delegates that reflect over the settings objects on the fly instead of
/// being baked into the <see cref="ISettingsHolder"/> tree at parse time. A content node's items
/// come and go while the menu is open, and a conditionally displayed property has to be re-checked
/// every frame, so neither can be expressed as a fixed tree of holders.
/// </remarks>
public static partial class SettingsParser
{
    private const uint TextNodeMaxLength = 1024;
    private const int MaxDrawDepth = 16;

    [ThreadStatic] private static int _drawDepth;

    // Per-object drawer lists, so opening a menu does not re-reflect over every settings object on
    // every frame. Keyed weakly: a content-node item that the user removes must not be kept alive
    // by its cached drawers.
    private static readonly ConditionalWeakTable<object, List<MemberDrawer>> MemberDrawerCache =
        new ConditionalWeakTable<object, List<MemberDrawer>>();

    private const BindingFlags MemberLookupFlags =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

    /// <summary>
    /// The <c>[Submenu]</c> that applies to a property: the one on the property itself, else the
    /// one on the property's type. The property wins so that one settings class can be reused with
    /// different collapsing defaults.
    /// </summary>
    private static SubmenuAttribute GetSubmenuAttribute(PropertyInfo property)
    {
        return property.GetCustomAttribute<SubmenuAttribute>() ?? property.PropertyType.GetCustomAttribute<SubmenuAttribute>();
    }

    /// <summary>
    /// Builds the drawer for a submenu property: either its own <see cref="SubmenuAttribute.RenderMethod"/>
    /// or a group of the generated widgets for the submenu object's own properties.
    /// </summary>
    internal static Action BuildSubmenuDrawer(object value, SubmenuAttribute attribute, string name, string id, object owner)
    {
        if (value == null) return null;

        if (!string.IsNullOrEmpty(attribute.RenderMethod))
        {
            var render = ResolveRenderMethod(value, attribute.RenderMethod, owner);

            if (render == null)
            {
                var message =
                    $"Submenu '{name}': render method '{attribute.RenderMethod}' not found on {value.GetType().Name}. " +
                    "It has to be an instance method taking no parameters, or the plugin instance.";

                DebugWindow.LogError(message, 10);
                return () => ImGui.TextDisabled(message);
            }

            var guardedRender = Guard(render, $"submenu '{name}' render method");

            if (!attribute.EnableSelfDrawCollapsing) return guardedRender;

            return () =>
            {
                if (!BeginCollapsingGroup($"{name}##{id}", attribute.CollapsedByDefault)) return;

                try
                {
                    guardedRender();
                }
                finally
                {
                    ImGui.TreePop();
                }
            };
        }

        if (!attribute.EnableCollapsing)
        {
            return () =>
            {
                ImGui.Text(name);
                ImGui.Indent();

                try
                {
                    DrawMembers(value, owner);
                }
                finally
                {
                    ImGui.Unindent();
                }
            };
        }

        return () =>
        {
            if (!BeginCollapsingGroup($"{name}##{id}", attribute.CollapsedByDefault)) return;

            try
            {
                DrawMembers(value, owner);
            }
            finally
            {
                ImGui.TreePop();
            }
        };
    }

    /// <summary>
    /// Builds the drawer for a <see cref="ContentNode{T}"/>: a collapsible list of its items with
    /// add/remove controls.
    /// </summary>
    internal static Action BuildContentNodeDrawer(IContentNodeBase node, string name, string id, object owner)
    {
        if (node == null) return null;

        return () =>
        {
            if (!BeginCollapsingGroup($"{name}##{id}", false)) return;

            try
            {
                object itemToRemove = null;
                var index = 0;

                foreach (var item in node.Content)
                {
                    if (item == null) continue;
                    ImGui.PushID(index++);

                    try
                    {
                        if (node.EnableControls)
                        {
                            if (ImGui.Button("x##remove")) itemToRemove = item;
                            ImGui.SameLine();
                        }

                        DrawContentItem(node, item, owner);
                    }
                    finally
                    {
                        ImGui.PopID();
                    }
                }

                // Deferred: the list is being enumerated above.
                if (itemToRemove != null) node.Remove(itemToRemove);

                if (node.EnableControls && node.SpawnItem is {} spawnItem && ImGui.Button($"+ Add##add{id}")) spawnItem();
            }
            finally
            {
                ImGui.TreePop();
            }
        };
    }

    private static void DrawContentItem(IContentNodeBase node, object item, object owner)
    {
        if (node.UseFlatItems)
        {
            // The item is a single node (ContentNode<TextNode> and friends): one widget per row,
            // with no name of its own — the row is the value.
            var drawer = GetNodeDrawDelegate(item, "", "value");
            if (drawer == null) ImGui.Text(item.ToString());
            else drawer();

            return;
        }

        // ToString() is the item header, which lets an item type follow the value the user is
        // editing (the ImGui "text##id" convention works here as usual).
        var header = item.ToString() ?? item.GetType().Name;

        if (node.EnableItemCollapsing)
        {
            if (!BeginCollapsingGroup(header, false)) return;

            try
            {
                DrawMembers(item, owner);
            }
            finally
            {
                ImGui.TreePop();
            }

            return;
        }

        ImGui.Text(header);
        ImGui.Indent();

        try
        {
            DrawMembers(item, owner);
        }
        finally
        {
            ImGui.Unindent();
        }
    }

    /// <summary>Draws every visible property of an arbitrary settings object.</summary>
    private static void DrawMembers(object target, object owner)
    {
        // Settings are trees in practice, but nothing stops a plugin from wiring one settings
        // object back into itself. Bail out before the render thread's stack does.
        if (_drawDepth >= MaxDrawDepth)
        {
            ImGui.TextDisabled($"...(settings nested deeper than {MaxDrawDepth} levels)");
            return;
        }

        _drawDepth++;

        try
        {
            foreach (var member in GetMemberDrawers(target, owner))
            {
                if (member.ShouldDraw != null && !member.ShouldDraw()) continue;
                member.Draw();
            }
        }
        finally
        {
            _drawDepth--;
        }
    }

    private static List<MemberDrawer> GetMemberDrawers(object target, object owner)
    {
        return MemberDrawerCache.GetValue(target, key => BuildMemberDrawers(key, owner));
    }

    private static List<MemberDrawer> BuildMemberDrawers(object target, object owner)
    {
        var result = new List<MemberDrawer>();

        foreach (var property in target.GetType().GetProperties())
        {
            if (property.GetCustomAttribute<IgnoreMenuAttribute>() != null) continue;
            if (!property.CanRead || property.GetIndexParameters().Length > 0) continue;

            var menuAttribute = property.GetCustomAttribute<MenuAttribute>();
            var name = ResolveName(menuAttribute, property.Name);

            object value;

            try
            {
                value = property.GetValue(target);
            }
            catch (Exception e)
            {
                DebugWindow.LogError($"Could not read settings property {target.GetType().Name}.{property.Name}: {e.Message}", 10);
                continue;
            }

            if (value == null) continue;

            var draw = BuildMemberDrawer(value, property, name, MakeId(target, property.Name), owner);

            // Not a settings-shaped property. A submenu class is free to carry state of its own
            // (counters, caches, raw arrays), so this is silent by design.
            if (draw == null) continue;

            var tooltip = menuAttribute?.Tooltip;

            if (!string.IsNullOrEmpty(tooltip))
            {
                var inner = draw;

                draw = () =>
                {
                    inner();
                    ImGui.SameLine();
                    ImGui.TextDisabled("(?)");
                    if (ImGui.IsItemHovered(ImGuiHoveredFlags.None)) ImGui.SetTooltip(tooltip);
                };
            }

            result.Add(new MemberDrawer
            {
                Draw = Guard(draw, $"{target.GetType().Name}.{property.Name}"),
                ShouldDraw = BuildCondition(target, property.GetCustomAttribute<ConditionalDisplayAttribute>())
            });
        }

        return result;
    }

    private static Action BuildMemberDrawer(object value, PropertyInfo property, string name, string id, object owner)
    {
        var submenuAttribute = GetSubmenuAttribute(property);
        if (submenuAttribute != null) return BuildSubmenuDrawer(value, submenuAttribute, name, id, owner);

        if (value is IContentNodeBase contentNode) return BuildContentNodeDrawer(contentNode, name, id, owner);

        // A nested ISettings object inside a submenu: grouped like a submenu rather than flattened,
        // because there is no holder tree at this level to flatten it into.
        if (value is ISettings) return BuildSubmenuDrawer(value, new SubmenuAttribute(), name, id, owner);

        return GetNodeDrawDelegate(value, name, id);
    }

    /// <summary>
    /// Builds the predicate for a <see cref="ConditionalDisplayAttribute"/>, or <c>null</c> when
    /// there is no attribute or its condition cannot be resolved.
    /// </summary>
    internal static Func<bool> BuildCondition(object target, ConditionalDisplayAttribute attribute)
    {
        if (attribute == null || target == null) return null;

        var evaluator = ResolveCondition(target, attribute.ConditionMethodName);

        if (evaluator == null)
        {
            // Showing a setting that should have been hidden is a cosmetic problem; hiding one the
            // user is looking for is not. Report it and keep the property visible.
            DebugWindow.LogError(
                $"[ConditionalDisplay] condition '{attribute.ConditionMethodName}' was not found on {target.GetType().Name}. " +
                "It has to be a parameterless bool method, or a bool/ToggleNode property or field.", 10);

            return null;
        }

        var expected = attribute.ComparisonValue;
        var reported = false;

        return () =>
        {
            try
            {
                return evaluator() == expected;
            }
            catch (Exception e)
            {
                if (!reported)
                {
                    reported = true;
                    DebugWindow.LogError($"[ConditionalDisplay] condition '{attribute.ConditionMethodName}' threw: {e}", 10);
                }

                return true;
            }
        };
    }

    private static Func<bool> ResolveCondition(object target, string memberName)
    {
        if (string.IsNullOrEmpty(memberName)) return null;

        var type = target.GetType();

        try
        {
            var method = type.GetMethod(memberName, MemberLookupFlags, null, Type.EmptyTypes, null);
            if (method != null && method.ReturnType == typeof(bool)) return () => (bool) method.Invoke(target, null);

            var property = type.GetProperty(memberName, MemberLookupFlags);

            if (property != null && property.CanRead)
            {
                var reader = GetBooleanReader(property.PropertyType);
                if (reader != null) return () => reader(property.GetValue(target));
            }

            var field = type.GetField(memberName, MemberLookupFlags);

            if (field != null)
            {
                var reader = GetBooleanReader(field.FieldType);
                if (reader != null) return () => reader(field.GetValue(target));
            }
        }
        catch (AmbiguousMatchException)
        {
            // A shadowed member of that name: too ambiguous to pick one silently.
        }

        return null;
    }

    private static Func<object, bool> GetBooleanReader(Type memberType)
    {
        if (memberType == typeof(bool)) return value => value is bool flag && flag;
        if (typeof(ToggleNode).IsAssignableFrom(memberType)) return value => value is ToggleNode node && node.Value;
        return null;
    }

    /// <summary>
    /// Resolves a <see cref="SubmenuAttribute.RenderMethod"/> to a callable delegate. The method
    /// may take no parameters, or the owning plugin instance.
    /// </summary>
    private static Action ResolveRenderMethod(object target, string methodName, object owner)
    {
        var candidates = target.GetType()
            .GetMethods(MemberLookupFlags)
            .Where(method => method.Name == methodName)
            .ToList();

        var parameterless = candidates.Find(method => method.GetParameters().Length == 0);
        if (parameterless != null) return () => parameterless.Invoke(target, null);

        if (owner == null) return null;

        var withOwner = candidates.Find(method =>
        {
            var parameters = method.GetParameters();
            return parameters.Length == 1 && parameters[0].ParameterType.IsInstanceOfType(owner);
        });

        return withOwner == null ? null : () => withOwner.Invoke(target, new[] {owner});
    }

    /// <summary>
    /// Wraps a draw delegate so that a throwing plugin setting reports once instead of taking down
    /// the render loop, or spamming the log at frame rate.
    /// </summary>
    private static Action Guard(Action draw, string description)
    {
        var reported = false;

        return () =>
        {
            try
            {
                draw();
            }
            catch (Exception e)
            {
                if (reported) return;

                reported = true;
                DebugWindow.LogError($"Error drawing {description}: {e}", 10);
            }
        };
    }

    private static bool BeginCollapsingGroup(string label, bool collapsedByDefault)
    {
        return ImGui.TreeNodeEx(label, collapsedByDefault ? ImGuiTreeNodeFlags.None : ImGuiTreeNodeFlags.DefaultOpen);
    }

    /// <summary>
    /// An ImGui id that is stable for the lifetime of the object and distinct between two items of
    /// the same content node.
    /// </summary>
    private static string MakeId(object target, string memberName)
    {
        return $"{memberName}_{RuntimeHelpers.GetHashCode(target)}";
    }

    private sealed class MemberDrawer
    {
        public Action Draw;
        public Func<bool> ShouldDraw;
    }
}
