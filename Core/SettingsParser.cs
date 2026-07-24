using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Helpers;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using ImGuiNET;
using JM.LinqFaster;
using MoreLinq;

namespace ExileCore;

/// <summary>
/// Reflects over an <see cref="ISettings"/> object and builds the ImGui menu drawers for each
/// property, wiring up the appropriate widget (button, toggle, slider, color, list, ...) based
/// on the property's node type and <c>[Menu]</c> attributes.
/// </summary>
/// <remarks>
/// <c>[Submenu]</c>, <c>[ConditionalDisplay]</c> and <see cref="ContentNode{T}"/> are handled in
/// <c>SettingsParser.Reflection.cs</c>: unlike the widgets below, those render a tree whose shape
/// (and, for content nodes, whose contents) is only known at draw time.
/// </remarks>
public static partial class SettingsParser
{
    /// <summary>
    /// Recursively parses the settings object into the supplied drawer list, nesting child
    /// drawers under their parent tabs/borders according to the menu attributes.
    /// </summary>
    /// <param name="settings">The settings object to reflect over.</param>
    /// <param name="draws">The drawer list to append to.</param>
    /// <param name="id">The parent holder id, or -1 at the top level.</param>
    /// <param name="owner">
    /// The plugin instance the settings belong to, if any. Only used to call a
    /// <see cref="SubmenuAttribute.RenderMethod"/> that takes the plugin as its parameter.
    /// </param>
    public static void Parse(ISettings settings, List<ISettingsHolder> draws, int id = -1, object owner = null)
    {
        if (settings == null)
        {
            DebugWindow.LogError("Cant parse null settings.");
            return;
        }

        var props = settings.GetType().GetProperties();

        foreach (var property in props)
        {
            if (property.GetCustomAttribute<IgnoreMenuAttribute>() != null) continue;
            var menuAttribute = property.GetCustomAttribute<MenuAttribute>();
            var submenuAttribute = GetSubmenuAttribute(property);
            var isSettings = property.PropertyType.GetInterfaces().ContainsF(typeof(ISettings));

            if (property.Name == "Enable" && menuAttribute == null) continue;

            if (menuAttribute == null)
                menuAttribute = new MenuAttribute(PrettifyName(property.Name));

            var condition = BuildCondition(settings, property.GetCustomAttribute<ConditionalDisplayAttribute>());

            var holder = condition == null
                ? new SettingsHolder()
                : new ConditionalSettingsHolder {ShouldDraw = condition};

            holder.Name = ResolveName(menuAttribute, property.Name);
            holder.Tooltip = menuAttribute.Tooltip;
            holder.ID = menuAttribute.index == -1 ? MathHepler.Randomizer.Next(int.MaxValue) : menuAttribute.index;

            // A settings object nested via ISettings is flattened into the parent menu (or into its
            // own tab), which is the pre-[Submenu] way of grouping. [Submenu] on the same property
            // asks for the grouped rendering instead, so it wins.
            if (isSettings && submenuAttribute == null)
            {
                var innerSettings = (ISettings) property.GetValue(settings);

                if (menuAttribute.index != -1)
                {
                    holder.Type = HolderChildType.Tab;
                    draws.Add(holder);
                    Parse(innerSettings, draws, menuAttribute.index, owner);
                    var parent = GetAllDrawers(draws).Find(x => x.ID == menuAttribute.parentIndex);
                    parent?.Children.Add(holder);
                }
                else
                    Parse(innerSettings, draws, -1, owner);

                continue;
            }

            var type = property.GetValue(settings);

            if (menuAttribute.parentIndex != -1)
            {
                var parent = GetAllDrawers(draws).Find(x => x.ID == menuAttribute.parentIndex);
                parent?.Children.Add(holder);
            }
            else if (id != -1)
            {
                var parent = GetAllDrawers(draws).Find(x => x.ID == id);
                parent?.Children.Add(holder);
            }
            else
                draws.Add(holder);

            if (submenuAttribute != null)
            {
                holder.DrawDelegate = BuildSubmenuDrawer(type, submenuAttribute, holder.Name, holder.ID.ToString(), owner);
                continue;
            }

            if (type is IContentNodeBase contentNode)
            {
                holder.DrawDelegate = BuildContentNodeDrawer(contentNode, holder.Name, holder.ID.ToString(), owner);
                continue;
            }

            var drawDelegate = GetNodeDrawDelegate(type, holder.Name, holder.ID.ToString());

            if (drawDelegate == null)
            {
                Core.Logger.Warning($"{type} not supported for menu now. Ask developers to add this type.");
                continue;
            }

            holder.DrawDelegate = drawDelegate;
        }
    }

    /// <summary>
    /// Builds the widget for a single settings node, or <c>null</c> when the value is not a node
    /// type the menu knows how to draw.
    /// </summary>
    /// <param name="node">The node value.</param>
    /// <param name="name">The display name.</param>
    /// <param name="id">The ImGui id suffix, unique within the settings tree.</param>
    internal static Action GetNodeDrawDelegate(object node, string name, string id)
    {
        var label = $"{name}##{id}";

        switch (node)
        {
            case ButtonNode n:
                return () =>
                {
                    if (ImGui.Button(label)) n.OnPressed();
                };
            case EmptyNode:
                return () => { };
            // The node supplies its own ImGui drawing instead of mapping to a built-in control.
            // The field is read at draw time (not captured), so a plugin may swap the callback
            // after the menu is built; ?.Invoke() keeps a node whose delegate is null or has been
            // cleared a silent no-op, like EmptyNode, instead of throwing on the render thread.
            case CustomNode n:
                return () => n.DrawDelegate?.Invoke();
            case HotkeyNodeV2 n:
                return () => n.DrawPickerButton($"{name}: {n.Value}##{id}");
            case HotkeyNode n:
                return () =>
                {
                    var holderName = $"{name} {n.Value}##{n.Value}";
                    var open = true;

                    if (ImGui.Button(holderName))
                    {
                        ImGui.OpenPopup(holderName);
                        open = true;
                    }

                    if (ImGui.BeginPopupModal(holderName, ref open, (ImGuiWindowFlags) 35))
                    {
                        if (Input.GetKeyState(Keys.Escape))
                        {
                            ImGui.CloseCurrentPopup();
                            ImGui.EndPopup();
                            return;
                        }

                        foreach (var key in Enum.GetValues(typeof(Keys)))
                        {
                            var keyState = Input.GetKeyState((Keys) key);

                            if (keyState)
                            {
                                n.Value = (Keys) key;
                                ImGui.CloseCurrentPopup();
                                break;
                            }
                        }

                        ImGui.Text($" Press new key to change '{n.Value}' or Esc for exit.");

                        ImGui.EndPopup();
                    }
                };
            case ToggleNode n:
                return () =>
                {
                    var value = n.Value;
                    ImGui.Checkbox(label, ref value);
                    n.Value = value;
                };
            case ColorNode n:
                return () =>
                {
                    var vector4 = n.Value.ToVector4().ToVector4Num();

                    if (ImGui.ColorEdit4(label, ref vector4,
                        ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.NoInputs |
                        ImGuiColorEditFlags.AlphaPreviewHalf)) n.Value = vector4.ToSharpColor();
                };
            case TextNode n:
                return () =>
                {
                    var value = n.Value ?? "";
                    if (ImGui.InputText(label, ref value, TextNodeMaxLength, ImGuiInputTextFlags.None)) n.Value = value;
                };
            case ListNode n:
                return () =>
                {
                    if (ImGui.BeginCombo(label, n.Value))
                    {
                        foreach (var t in n.Values)
                        {
                            if (ImGui.Selectable(t))
                            {
                                n.Value = t;
                                ImGui.EndCombo();
                                return;
                            }
                        }

                        ImGui.EndCombo();
                    }
                };
            case FileNode n:
                return () =>
                {
                    if (ImGui.TreeNode(label))
                    {
                        var selected = n.Value;

                        if (ImGui.BeginChildFrame(1, new Vector2(0, 300)))
                        {
                            var di = new DirectoryInfo("config");

                            if (di.Exists)
                            {
                                foreach (var file in di.GetFiles())
                                {
                                    if (ImGui.Selectable(file.Name, selected == file.FullName))
                                        n.Value = file.FullName;
                                }
                            }

                            ImGui.EndChildFrame();
                        }

                        ImGui.TreePop();
                    }
                };
            case RangeNode<int> n:
                return () =>
                {
                    var r = n.Value;
                    ImGui.SliderInt(label, ref r, n.Min, n.Max);
                    n.Value = r;
                };
            case RangeNode<float> n:
                return () =>
                {
                    var r = n.Value;
                    ImGui.SliderFloat(label, ref r, n.Min, n.Max);
                    n.Value = r;
                };
            case RangeNode<long> n:
                return () =>
                {
                    var r = (int) n.Value;
                    ImGui.SliderInt(label, ref r, (int) n.Min, (int) n.Max);
                    n.Value = r;
                };
            case RangeNode<Vector2> n:
                return () =>
                {
                    var vect = n.Value;
                    ImGui.SliderFloat2(label, ref vect, n.Min.X, n.Max.X);
                    n.Value = vect;
                };
            default:
                return null;
        }
    }

    /// <summary>Turns <c>SomePropertyName</c> into <c>Some Property Name</c>.</summary>
    private static string PrettifyName(string propertyName)
    {
        return Regex.Replace(propertyName, "(\\B[A-Z])", " $1");
    }

    /// <summary>
    /// The display name for a property: the <c>[Menu]</c> name when it has one, otherwise the
    /// prettified property name. <c>[Menu(null, "tooltip")]</c> is a common way to attach only a
    /// tooltip, and must not blank out the label.
    /// </summary>
    private static string ResolveName(MenuAttribute menuAttribute, string propertyName)
    {
        return string.IsNullOrEmpty(menuAttribute?.MenuName) ? PrettifyName(propertyName) : menuAttribute.MenuName;
    }

    private static List<ISettingsHolder> GetAllDrawers(List<ISettingsHolder> SettingPropertyDrawers)
    {
        var result = new List<ISettingsHolder>();
        GetDrawersRecurs(SettingPropertyDrawers, result);
        return result;
    }

    private static void GetDrawersRecurs(IList<ISettingsHolder> drawers, IList<ISettingsHolder> result)
    {
        foreach (var drawer in drawers)
        {
            if (!result.Contains(drawer))
                result.Add(drawer);
            else
            {
                Core.Logger.Error(
                    $" Possible stashoverflow or duplicating drawers detected while generating menu. Drawer SettingName: {drawer.Name}, Id: {drawer.ID}",
                    5);
            }
        }

        drawers.ForEach(x => GetDrawersRecurs(x.Children, result));
    }
}

/// <summary>How a settings holder groups its children when rendered.</summary>
public enum HolderChildType
{
    /// <summary>Rendered as a tab.</summary>
    Tab,

    /// <summary>Rendered as a bordered group.</summary>
    Border
}

/// <summary>
/// A node in the settings menu tree: its label, tooltip, draw delegate and child holders.
/// Knows how to render itself (and its children) via ImGui.
/// </summary>
public class SettingsHolder : ISettingsHolder
{
    /// <summary>Creates a holder with an empty tooltip.</summary>
    public SettingsHolder()
    {
        Tooltip = "";
    }

    /// <summary>How this holder groups its children.</summary>
    public HolderChildType Type { get; set; } = HolderChildType.Border;

    /// <summary>The display name.</summary>
    public string Name { get; set; } = "";

    /// <summary>The optional tooltip text.</summary>
    public string Tooltip { get; set; }

    /// <summary>A unique ImGui id derived from the name and <see cref="ID"/>.</summary>
    public string Unique => $"{Name}##{ID}";

    /// <summary>The holder's identifier (used to resolve parent/child relationships).</summary>
    public int ID { get; set; } = -1;

    /// <summary>The delegate that draws this holder's widget.</summary>
    public Action DrawDelegate { get; set; }

    /// <summary>The child holders nested under this one.</summary>
    public IList<ISettingsHolder> Children { get; } = new List<ISettingsHolder>();

    /// <summary>Draws this holder and its children via ImGui.</summary>
    public virtual void Draw()
    {
        var size = ImGui.GetFont();

        if (Children.Count > 0)
        {
            for (var i = 0; i < 5; i++)
            {
                ImGui.Spacing();
            }

            ImGui.BeginGroup();
            var contentRegionAvail = ImGui.GetContentRegionAvail();

            var OverChild = ImGui.GetCursorPos().Translate(10, size.FontSize * -0.66f);

            ImGui.BeginChild(Unique, new Vector2(contentRegionAvail.X, size.FontSize * 2 * (Children.Count + 0.2f)), true);

            foreach (var child in Children)
            {
                child.Draw();
            }

            // var fontContainer = Fonts.Last().Value;
            // ImGui.PushFont(fontContainer.Atlas);

            var getCursor = ImGui.GetCursorPos().Translate(0, size.FontSize);
            ImGui.EndChild();
            ImGui.SetCursorPos(OverChild);
            ImGui.Text(Name);

            if (Tooltip?.Length > 0)
            {
                ImGui.SameLine();
                ImGui.TextDisabled("(?)");
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.None)) ImGui.SetTooltip(Tooltip);
            }

            ImGui.SetCursorPos(getCursor);
            ImGui.EndGroup();

            DrawDelegate?.Invoke();

            //  ImGui.PopFont();
        }
        else
        {
            DrawDelegate?.Invoke();

            if (Tooltip?.Length > 0)
            {
                ImGui.SameLine();
                ImGui.TextDisabled("(?)");
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.None)) ImGui.SetTooltip(Tooltip);
            }
        }
    }
}

/// <summary>
/// A holder that is skipped entirely — widget, tooltip and children — while its condition is
/// false. Built for properties carrying <see cref="ConditionalDisplayAttribute"/>.
/// </summary>
/// <remarks>
/// The check has to sit at the holder level rather than inside the draw delegate, otherwise the
/// tooltip marker drawn by <see cref="SettingsHolder.Draw"/> would be left behind on its own.
/// </remarks>
public class ConditionalSettingsHolder : SettingsHolder
{
    /// <summary>Evaluated every frame; the holder is drawn only when it returns <c>true</c>.</summary>
    public Func<bool> ShouldDraw { get; set; }

    /// <summary>Draws the holder unless <see cref="ShouldDraw"/> says otherwise.</summary>
    public override void Draw()
    {
        if (ShouldDraw != null && !ShouldDraw()) return;
        base.Draw();
    }
}
