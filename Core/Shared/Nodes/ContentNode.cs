using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace ExileCore.Shared.Nodes
{
    /// <summary>
    /// A settings entry holding a user-editable <b>list</b> of sub-settings: the menu renders one
    /// group per item plus add/remove controls, and the list is persisted with the rest of the
    /// plugin's settings.
    /// </summary>
    /// <typeparam name="T">
    /// The item type. Either a class carrying <c>[Submenu]</c> (its properties are expanded into a
    /// per-item group), or a plain settings node such as <see cref="TextNode"/> combined with
    /// <see cref="UseFlatItems"/>.
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// Only <see cref="Content"/> is persisted (see <see cref="ContentNodeConverter{T}"/>): the
    /// rest of the members configure how the list is drawn and are supplied in code, so a settings
    /// file cannot pin them to a stale value.
    /// </para>
    /// <para>
    /// Item headers use <c>ToString()</c>, so overriding it on the item type gives the list
    /// readable rows. The ImGui <c>label##id</c> convention works there as usual — e.g.
    /// <c>$"{MetadataRegex.Value}###{base.ToString()}"</c> keeps a stable id while the visible
    /// text follows the edited value.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// [Submenu]
    /// public class ChestPattern
    /// {
    ///     public ToggleNode Enabled { get; set; } = new ToggleNode(true);
    ///     public TextNode MetadataRegex { get; set; } = new TextNode("^$");
    ///     public override string ToString() => $"{MetadataRegex.Value}###{base.ToString()}";
    /// }
    ///
    /// public ContentNode&lt;ChestPattern&gt; ChestList { get; set; } = new()
    /// {
    ///     ItemFactory = () =&gt; new ChestPattern(),
    /// };
    /// </code>
    /// </example>
    [JsonConverter(typeof(ContentNodeConverter))]
    public class ContentNode<T> : IContentNodeBase
    {
        private List<T> _content = new List<T>();

        /// <summary>The items. Never <c>null</c>: assigning <c>null</c> stores an empty list.</summary>
        public List<T> Content
        {
            get => _content;
            set => _content = value ?? new List<T>();
        }

        /// <summary>
        /// Whether the menu draws the add/remove controls. Turn it off for a list the plugin
        /// maintains itself and the user should only inspect and edit in place.
        /// </summary>
        [JsonIgnore]
        public bool EnableControls { get; set; } = true;

        /// <summary>Whether each item gets its own collapsible header.</summary>
        [JsonIgnore]
        public bool EnableItemCollapsing { get; set; } = true;

        /// <summary>
        /// Whether an item is a single settings node to be drawn as one inline widget per row
        /// (e.g. <c>ContentNode&lt;TextNode&gt;</c>) instead of an object expanded into a group.
        /// </summary>
        [JsonIgnore]
        public bool UseFlatItems { get; set; }

        /// <summary>
        /// Creates a new item for the "add" control. A node without a factory shows no add button:
        /// there is no general way to construct <typeparamref name="T"/>, and quietly requiring a
        /// parameterless constructor would fail at the worst moment.
        /// </summary>
        /// <remarks>
        /// Not serialized (a delegate cannot be), and re-supplied by the property initializer on
        /// every load — which is why <see cref="ContentNodeConverter{T}"/> deserializes into the
        /// existing node instead of replacing it.
        /// </remarks>
        [JsonIgnore]
        public Func<T> ItemFactory { get; set; }

        /// <summary>Called after an item has been removed through the menu.</summary>
        [JsonIgnore]
        public Action<T> OnRemove { get; set; }

        /// <summary>Removes the given item, invoking <see cref="OnRemove"/> if it was present.</summary>
        /// <param name="item">The item to remove. A value of another type is simply not found.</param>
        /// <returns>Whether the item was present and removed.</returns>
        public bool Remove(object item)
        {
            if (!(item is T typedItem)) return false;
            if (!_content.Remove(typedItem)) return false;

            try
            {
                OnRemove?.Invoke(typedItem);
            }
            catch (Exception e)
            {
                DebugWindow.LogError($"Error in function that subscribed for: {nameof(ContentNode<T>)}.{nameof(OnRemove)}. {e}", 10);
            }

            return true;
        }

        IEnumerable<object> IContentNodeBase.Content => _content.Cast<object>();

        bool IContentNodeBase.EnableControls => EnableControls;

        bool IContentNodeBase.EnableItemCollapsing => EnableItemCollapsing;

        bool IContentNodeBase.UseFlatItems => UseFlatItems;

        Action IContentNodeBase.SpawnItem => ItemFactory == null
            ? null
            : () => _content.Add(ItemFactory());
    }
}
