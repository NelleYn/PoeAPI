using System;
using Newtonsoft.Json;

namespace ExileCore.Shared.Nodes
{
    /// <summary>
    /// A settings entry that draws itself. Instead of mapping to a built-in ImGui control, the menu
    /// invokes <see cref="DrawDelegate"/> at the point where this node appears, letting a plugin place
    /// arbitrary ImGui widgets inside the generated settings tree.
    /// </summary>
    /// <remarks>
    /// The node holds no value and is not persisted: <c>SettingsParser</c> only calls the delegate, and
    /// there is nothing to serialize. Any state the custom UI edits must live in other nodes (or in the
    /// plugin's own fields) if it needs to survive a restart.
    /// <para>
    /// The delegate runs on the render thread inside the settings window, once per frame while the
    /// plugin's settings are open. Keep it cheap and do not block in it.
    /// </para>
    /// </remarks>
    public class CustomNode
    {
        /// <summary>Initializes an empty node; assign <see cref="DrawDelegate"/> before the menu is built.</summary>
        public CustomNode()
        {
        }

        /// <summary>Initializes the node with the callback that draws it.</summary>
        /// <param name="drawDelegate">The ImGui drawing callback. May be <c>null</c>, in which case the node draws nothing.</param>
        public CustomNode(Action drawDelegate)
        {
            DrawDelegate = drawDelegate;
        }

        /// <summary>
        /// The ImGui drawing callback for this entry. A <c>null</c> delegate simply draws nothing, so an
        /// unassigned node is harmless.
        /// </summary>
        /// <remarks>
        /// Marked <see cref="JsonIgnoreAttribute"/> for the same reason as <c>ButtonNode.OnPressed</c>:
        /// plugin settings are persisted with <c>JsonConvert.SerializeObject</c>
        /// (<c>Core/SettingsContainer.cs</c>), and a delegate is not serializable. It is read through
        /// the field on every draw, so reassigning it after the menu has been built takes effect
        /// immediately.
        /// </remarks>
        [JsonIgnore] public Action DrawDelegate = delegate { };
    }
}
