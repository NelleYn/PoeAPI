// NOTE: full implementation not recoverable from the protected DLL; emitted as a signature-only stub.
namespace ExileCore.Shared.Nodes;
public partial class HotkeyNodeV2
{
    public static System.Collections.Generic.IReadOnlyCollection<System.Windows.Forms.Keys> AlwaysExcludedKeys;
    public static System.Collections.Generic.IReadOnlyCollection<System.Windows.Forms.Keys> ModifierVirtualKeys;
    public ExileCore.Shared.Nodes.HotkeyNodeV2.HotkeyNodeValue _value;
    public System.Boolean _pressed;
    public System.Boolean _unPressed;
    public System.Boolean _changingKey;
    public System.Boolean _tempShift;
    public System.Boolean _tempCtrl;
    public System.Boolean _tempAlt;
    public System.Boolean _tempWin;
    public static System.Collections.Generic.Dictionary<System.String, ExileCore.Shared.Nodes.HotkeyNodeV2.ControllerKey> SelectableControllerKeys;
    public System.Action OnValueChanged
    {
        get
        {
            throw new global::System.NotImplementedException();
        }
    }

    public System.Windows.Forms.Keys LegacyValue
    {
        get
        {
            throw new global::System.NotImplementedException();
        }
    }

    public System.Boolean AllowControllerKeys
    {
        get
        {
            throw new global::System.NotImplementedException();
        }
    }

    public System.Boolean IgnoreFocusedInput
    {
        get
        {
            throw new global::System.NotImplementedException();
        }
    }

    public ExileCore.Shared.Nodes.HotkeyNodeV2.HotkeyNodeValue Value
    {
        get
        {
            throw new global::System.NotImplementedException();
        }
    }

    public System.Boolean ShouldSerializeLegacyValue()
    {
        throw new global::System.NotImplementedException();
    }

    public System.Boolean DrawPickerButton(System.String id)
    {
        throw new global::System.NotImplementedException();
    }

    public System.Boolean PressedOnce()
    {
        throw new global::System.NotImplementedException();
    }

    public System.Boolean IsPressed()
    {
        throw new global::System.NotImplementedException();
    }

    public System.Boolean UnpressedOnce()
    {
        throw new global::System.NotImplementedException();
    }
}