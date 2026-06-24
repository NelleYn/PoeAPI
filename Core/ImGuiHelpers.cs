// NOTE: full implementation not recoverable from the protected DLL; emitted as a signature-only stub.
namespace ExileCore;
public partial class ImGuiHelpers
{
    public static System.Runtime.CompilerServices.ConditionalWeakTable<System.Object, System.Collections.Concurrent.ConcurrentDictionary<System.UInt32, ExileCore.ImGuiHelpers.ComboboxState>> _comboboxState;
    public static System.Runtime.CompilerServices.ConditionalWeakTable<System.Object, System.Collections.Generic.List<System.String>> _comboboxItemMapping;
    public static nint IconsImageId;
    public static ExileCore.CoreSettings CoreSettings;
    public static System.Func<System.String, System.String, System.Boolean> WhitespaceSeparatedContains
    {
        get
        {
            throw new global::System.NotImplementedException();
        }
    }

    public static System.IDisposable UseStyleVar(ImGuiNET.ImGuiStyleVar idx, System.Single val)
    {
        throw new global::System.NotImplementedException();
    }

    public static System.IDisposable UseStyleVar(ImGuiNET.ImGuiStyleVar idx, System.Numerics.Vector2 val)
    {
        throw new global::System.NotImplementedException();
    }

    public static System.IDisposable UseStyleColor(ImGuiNET.ImGuiCol idx, System.Numerics.Vector4 col)
    {
        throw new global::System.NotImplementedException();
    }

    public static System.IDisposable UseStyleColor(ImGuiNET.ImGuiCol idx, System.UInt32 col)
    {
        throw new global::System.NotImplementedException();
    }

    public static System.Boolean SetDragDropPayload<T>(System.String id, T payload)
    {
        throw new global::System.NotImplementedException();
    }

    public static System.Nullable<T> AcceptDragDropPayload<T>(System.String id)
    {
        throw new global::System.NotImplementedException();
    }

    public static System.Boolean DrawAllColumnsBox(System.String id, System.Numerics.Vector2 start)
    {
        throw new global::System.NotImplementedException();
    }

    public static System.Boolean SearchCombobox<T>(System.String id, ref System.String input, ref T selectedItem, System.Collections.Generic.IReadOnlyCollection<T> items, System.Func<T, System.String, System.Boolean> filter, System.Func<T, System.String> stringProvider)
    {
        throw new global::System.NotImplementedException();
    }

    public static System.Boolean SearchCombobox(System.String id, ref System.String input, ref System.String selectedItem, System.Collections.Generic.IReadOnlyCollection<System.String> items, System.Func<System.String, System.String, System.Boolean> filter)
    {
        throw new global::System.NotImplementedException();
    }

    public static System.Boolean SearchCombobox(System.String id, ref System.String input, ref System.String selectedItem, System.Collections.Generic.IReadOnlyCollection<System.String> items, System.Func<System.String, System.String, System.Boolean> filter, System.Boolean allowCustomValues)
    {
        throw new global::System.NotImplementedException();
    }

    public static System.Boolean IconPickerWindow(System.String iconName, ref ExileCore.Shared.Enums.MapIconsIndex icon, System.Numerics.Vector4 tintColor, ref System.String filter)
    {
        throw new global::System.NotImplementedException();
    }

    public static System.Boolean IconButton(System.String id, System.Numerics.Vector2 size, ExileCore.Shared.Enums.MapIconsIndex icon, System.Numerics.Vector4 tintColor)
    {
        throw new global::System.NotImplementedException();
    }
}