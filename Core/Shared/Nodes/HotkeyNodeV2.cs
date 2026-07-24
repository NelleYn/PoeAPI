using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpDX;

namespace ExileCore.Shared.Nodes
{
    /// <summary>
    /// A hotkey setting that carries modifiers (Shift / Ctrl / Alt / Win) alongside the key, and
    /// draws its own key picker.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The difference from <see cref="HotkeyNode"/> is the modifier support: <c>Ctrl+F</c> and
    /// <c>F</c> are different hotkeys, and a bound hotkey only fires when the modifier state
    /// matches exactly, so two plugins can share a key with different modifiers.
    /// </para>
    /// <para>
    /// The node registers its key with <see cref="Input"/> whenever the value changes, so a plugin
    /// does not have to call <c>Input.RegisterKey</c> itself (doing so anyway is harmless).
    /// </para>
    /// <para>
    /// <b>Fork note.</b> <see cref="AllowControllerKeys"/> is accepted for source compatibility with
    /// plugins written against ExileApi-Compiled, but this fork has no controller input backend:
    /// the picker only offers keyboard and mouse keys, and the flag has no effect. It is not
    /// serialized, so nothing about a settings file changes if that ever gains a backend.
    /// </para>
    /// </remarks>
    public class HotkeyNodeV2
    {
        private const string PickerPopupName = "Press the new hotkey";

        /// <summary>
        /// Keys that can never be bound: <see cref="Keys.None"/> (which means "unbound") and
        /// <see cref="Keys.Escape"/> (which cancels the picker, so it could never be pressed to
        /// select itself).
        /// </summary>
        public static readonly IReadOnlyCollection<Keys> AlwaysExcludedKeys = new[] {Keys.None, Keys.Escape};

        /// <summary>
        /// The modifier keys. They are tracked as modifiers of the bound key rather than being
        /// bindable on their own.
        /// </summary>
        public static readonly IReadOnlyCollection<Keys> ModifierVirtualKeys = new[]
        {
            Keys.ShiftKey, Keys.LShiftKey, Keys.RShiftKey,
            Keys.ControlKey, Keys.LControlKey, Keys.RControlKey,
            Keys.Menu, Keys.LMenu, Keys.RMenu,
            Keys.LWin, Keys.RWin
        };

        // Enum.GetValues(typeof(Keys)) also yields the modifier *flags* (Keys.Shift = 0x10000 and
        // friends), which are not virtual-key codes at all: polling them reads a key state well
        // outside the 1..0xFE VK range. Restrict the picker to real virtual keys.
        private static readonly Keys[] SelectableKeys = Enum.GetValues(typeof(Keys))
            .Cast<Keys>()
            .Where(key => (int) key > 0 && (int) key <= 0xFE)
            .Where(key => !AlwaysExcludedKeys.Contains(key) && !ModifierVirtualKeys.Contains(key))
            .Distinct()
            .ToArray();

        private bool _pressed;
        private bool _unPressed;
        private HotkeyNodeValue _value = new HotkeyNodeValue(Keys.None);

        /// <summary>Creates an unbound node.</summary>
        public HotkeyNodeV2()
        {
        }

        /// <summary>Creates a node bound to the given key with no modifiers.</summary>
        public HotkeyNodeV2(Keys key)
        {
            Value = new HotkeyNodeValue(key);
        }

        /// <summary>Creates a node with the given value.</summary>
        public HotkeyNodeV2(HotkeyNodeValue value)
        {
            Value = value;
        }

        /// <summary>Raised after <see cref="Value"/> changes.</summary>
        public event Action OnValueChanged;

        /// <summary>The bound key and its modifiers. Never <c>null</c>.</summary>
        public HotkeyNodeValue Value
        {
            get => _value;
            set
            {
                value ??= new HotkeyNodeValue(Keys.None);
                if (_value == value) return;

                _value = value;
                if (value.Key is {} key) Input.RegisterKey(key);

                try
                {
                    OnValueChanged?.Invoke();
                }
                catch (Exception e)
                {
                    DebugWindow.LogMsg($"Error in function that subscribed for: {nameof(HotkeyNodeV2)}.{nameof(OnValueChanged)}. {e}", 10,
                        Color.Red);
                }
            }
        }

        /// <summary>
        /// The bound key without its modifiers, for call sites that still deal in plain
        /// <see cref="Keys"/>. Assigning clears the modifiers.
        /// </summary>
        public Keys LegacyValue
        {
            get => Value.Key ?? Keys.None;
            set => Value = new HotkeyNodeValue(value);
        }

        /// <summary>
        /// Accepted for compatibility with ExileApi-Compiled; this fork has no controller input
        /// backend, so it has no effect. See the remarks on <see cref="HotkeyNodeV2"/>.
        /// </summary>
        [JsonIgnore]
        public bool AllowControllerKeys { get; set; }

        /// <summary>
        /// Whether the hotkey still fires while a text field in the overlay has keyboard focus.
        /// Defaults to <c>false</c>, so typing into a plugin's text box does not trigger hotkeys.
        /// </summary>
        /// <remarks>
        /// This covers the overlay's own ImGui input only. The game's chat box is not visible from
        /// here, so a plugin that must not fire while the player is typing in-game still has to
        /// check <c>IngameState.IngameUi.ChatTitlePanel</c> (or <c>FocusedInputElement</c>) itself.
        /// </remarks>
        [JsonIgnore]
        public bool IgnoreFocusedInput { get; set; }

        /// <summary>Never writes <see cref="LegacyValue"/>: it is a view over <see cref="Value"/>.</summary>
        public bool ShouldSerializeLegacyValue() => false;

        /// <summary>Uses the node's key as a plain <see cref="Keys"/>, dropping any modifiers.</summary>
        public static implicit operator Keys(HotkeyNodeV2 node)
        {
            return node?.Value.Key ?? Keys.None;
        }

        /// <summary>Creates a node bound to the given key with no modifiers.</summary>
        public static implicit operator HotkeyNodeV2(Keys key)
        {
            return new HotkeyNodeV2(key);
        }

        /// <summary>
        /// Draws a button that opens the key picker. Returns whether a new hotkey was assigned this
        /// frame.
        /// </summary>
        /// <param name="id">
        /// The button label. Follows the usual ImGui <c>label##id</c> convention, so a caller that
        /// puts the current value in the label can keep the id stable with <c>##</c>.
        /// </param>
        /// <remarks>
        /// The picker's open state lives in ImGui, not in the node, so a caller may build a
        /// throw-away node every frame (as ReAgent does for its per-rule keys) and the popup still
        /// survives until a key is pressed or the picker is cancelled with Esc.
        /// </remarks>
        public bool DrawPickerButton(string id)
        {
            var changed = false;
            ImGui.PushID(id);

            try
            {
                if (ImGui.Button(id)) ImGui.OpenPopup(PickerPopupName);

                var open = true;

                if (ImGui.BeginPopupModal(PickerPopupName, ref open, (ImGuiWindowFlags) 35))
                {
                    ImGui.Text($" Press the new hotkey for '{Value}', or Esc to cancel.");
                    ImGui.Text(" Hold Shift/Ctrl/Alt/Win to bind them together with the key.");

                    if (Input.GetKeyState(Keys.Escape))
                        ImGui.CloseCurrentPopup();
                    else
                    {
                        foreach (var key in SelectableKeys)
                        {
                            if (!Input.GetKeyState(key)) continue;

                            Value = new HotkeyNodeValue(key, IsModifierDown(Keys.ShiftKey), IsModifierDown(Keys.ControlKey),
                                IsModifierDown(Keys.Menu), IsModifierDown(Keys.LWin) || IsModifierDown(Keys.RWin));

                            changed = true;
                            ImGui.CloseCurrentPopup();
                            break;
                        }
                    }

                    ImGui.EndPopup();
                }
            }
            finally
            {
                ImGui.PopID();
            }

            return changed;
        }

        /// <summary>Whether the hotkey (key plus its exact modifier state) is currently held.</summary>
        public bool IsPressed()
        {
            var value = Value;
            var key = value.Key ?? Keys.None;
            if (key == Keys.None) return false;
            if (!IgnoreFocusedInput && IsOverlayTextInputActive()) return false;

            // Idempotent, and cheap when the key is already known. It keeps a node whose value was
            // never assigned through the setter (or whose plugin forgot to register it) from
            // hitting Input's "key is not registered" path.
            Input.RegisterKey(key);
            if (!Input.IsKeyDown(key)) return false;

            return IsModifierDown(Keys.ShiftKey) == value.Shift &&
                   IsModifierDown(Keys.ControlKey) == value.Ctrl &&
                   IsModifierDown(Keys.Menu) == value.Alt &&
                   (IsModifierDown(Keys.LWin) || IsModifierDown(Keys.RWin)) == value.Win;
        }

        /// <summary>Returns <c>true</c> once per press, on the frame the hotkey goes down.</summary>
        public bool PressedOnce()
        {
            if (IsPressed())
            {
                if (_pressed) return false;

                _pressed = true;
                return true;
            }

            _pressed = false;
            return false;
        }

        /// <summary>Returns <c>true</c> once per press, on the frame the hotkey is released.</summary>
        public bool UnpressedOnce()
        {
            if (IsPressed())
                _unPressed = true;
            else if (_unPressed)
            {
                _unPressed = false;
                return true;
            }

            return false;
        }

        private static bool IsModifierDown(Keys key)
        {
            return Input.GetKeyState(key);
        }

        private static bool IsOverlayTextInputActive()
        {
            try
            {
                return ImGui.GetIO().WantTextInput;
            }
            catch (Exception)
            {
                // No ImGui context yet (settings can be touched before the renderer is up).
                return false;
            }
        }

        /// <summary>
        /// A hotkey: a key plus the modifiers that must be held with it. Compared by value, so it
        /// can be used as a dictionary key or compared with <c>==</c>.
        /// </summary>
        [JsonConverter(typeof(HotkeyNodeValueConverter))]
        public sealed record HotkeyNodeValue
        {
            /// <summary>Creates an unbound value.</summary>
            public HotkeyNodeValue()
            {
            }

            /// <summary>Creates a value for the given key with no modifiers.</summary>
            public HotkeyNodeValue(Keys key)
            {
                Key = key;
            }

            /// <summary>Creates a value for the given key and modifiers.</summary>
            public HotkeyNodeValue(Keys? key, bool shift, bool ctrl, bool alt, bool win)
            {
                Key = key;
                Shift = shift;
                Ctrl = ctrl;
                Alt = alt;
                Win = win;
            }

            /// <summary>The bound key, or <c>null</c>/<see cref="Keys.None"/> when unbound.</summary>
            public Keys? Key { get; init; }

            /// <summary>Whether Shift must be held.</summary>
            public bool Shift { get; init; }

            /// <summary>Whether Ctrl must be held.</summary>
            public bool Ctrl { get; init; }

            /// <summary>Whether Alt must be held.</summary>
            public bool Alt { get; init; }

            /// <summary>Whether a Windows key must be held.</summary>
            public bool Win { get; init; }

            /// <summary>
            /// Uses the value as a plain <see cref="Keys"/>, dropping any modifiers — which is what
            /// <c>Input.RegisterKey(settings.MyHotkey.Value)</c> needs.
            /// </summary>
            public static implicit operator Keys(HotkeyNodeValue value)
            {
                return value?.Key ?? Keys.None;
            }

            /// <summary>Creates a value for the given key with no modifiers.</summary>
            public static implicit operator HotkeyNodeValue(Keys key)
            {
                return new HotkeyNodeValue(key);
            }

            /// <summary>Renders the hotkey the way it is written, e.g. <c>Ctrl+Shift+F</c>.</summary>
            public override string ToString()
            {
                var key = Key ?? Keys.None;
                if (key == Keys.None && !Shift && !Ctrl && !Alt && !Win) return "None";

                var parts = new List<string>(5);
                if (Ctrl) parts.Add("Ctrl");
                if (Shift) parts.Add("Shift");
                if (Alt) parts.Add("Alt");
                if (Win) parts.Add("Win");
                parts.Add(key.ToString());
                return string.Join("+", parts);
            }
        }
    }

    /// <summary>
    /// Reads and writes <see cref="HotkeyNodeV2.HotkeyNodeValue"/>, accepting the plain key that
    /// <see cref="HotkeyNode"/> wrote so a plugin can migrate a setting from
    /// <see cref="HotkeyNode"/> to <see cref="HotkeyNodeV2"/> without discarding — or failing to
    /// parse — the user's existing configuration file.
    /// </summary>
    public class HotkeyNodeValueConverter : JsonConverter
    {
        /// <summary>Whether the given type is a <see cref="HotkeyNodeV2.HotkeyNodeValue"/>.</summary>
        public override bool CanConvert(Type objectType)
        {
            return typeof(HotkeyNodeV2.HotkeyNodeValue).IsAssignableFrom(objectType);
        }

        /// <summary>Writes the key and its modifiers as an object, with the key as its enum name.</summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (!(value is HotkeyNodeV2.HotkeyNodeValue hotkey))
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName(nameof(HotkeyNodeV2.HotkeyNodeValue.Key));
            writer.WriteValue((hotkey.Key ?? Keys.None).ToString());
            writer.WritePropertyName(nameof(HotkeyNodeV2.HotkeyNodeValue.Shift));
            writer.WriteValue(hotkey.Shift);
            writer.WritePropertyName(nameof(HotkeyNodeV2.HotkeyNodeValue.Ctrl));
            writer.WriteValue(hotkey.Ctrl);
            writer.WritePropertyName(nameof(HotkeyNodeV2.HotkeyNodeValue.Alt));
            writer.WriteValue(hotkey.Alt);
            writer.WritePropertyName(nameof(HotkeyNodeV2.HotkeyNodeValue.Win));
            writer.WriteValue(hotkey.Win);
            writer.WriteEndObject();
        }

        /// <summary>Reads either the modifier-carrying object or a bare key (number or name).</summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);

            switch (token.Type)
            {
                case JTokenType.Null:
                case JTokenType.Undefined:
                    return null;
                case JTokenType.Object:
                    var obj = (JObject) token;

                    return new HotkeyNodeV2.HotkeyNodeValue(
                        ParseKey(obj[nameof(HotkeyNodeV2.HotkeyNodeValue.Key)]),
                        obj.Value<bool?>(nameof(HotkeyNodeV2.HotkeyNodeValue.Shift)) ?? false,
                        obj.Value<bool?>(nameof(HotkeyNodeV2.HotkeyNodeValue.Ctrl)) ?? false,
                        obj.Value<bool?>(nameof(HotkeyNodeV2.HotkeyNodeValue.Alt)) ?? false,
                        obj.Value<bool?>(nameof(HotkeyNodeV2.HotkeyNodeValue.Win)) ?? false);
                default:
                    // A settings file written when the property was still a HotkeyNode.
                    return new HotkeyNodeV2.HotkeyNodeValue(ParseKey(token) ?? Keys.None);
            }
        }

        private static Keys? ParseKey(JToken token)
        {
            if (token == null) return null;

            switch (token.Type)
            {
                case JTokenType.Integer:
                    return (Keys) token.Value<long>();
                case JTokenType.String:
                    return Enum.TryParse<Keys>(token.Value<string>(), out var parsed) ? parsed : null;
                default:
                    return null;
            }
        }
    }
}
