using System.Windows.Forms;
using ExileCore.Shared.Nodes;

namespace ExileCore.Shared.Helpers;

/// <summary>
/// Sends a <see cref="HotkeyNodeV2.HotkeyNodeValue"/> to the game as synthetic input, holding the
/// hotkey's modifiers around the key itself.
/// </summary>
/// <remarks>
/// <para>
/// This is the counterpart of <see cref="HotkeyNodeV2"/>'s reading side: a plugin that lets the
/// user configure "Ctrl+F" needs the modifier held while the key is pressed, which
/// <c>Input.KeyDown</c>/<c>Input.KeyUp</c> alone do not do.
/// </para>
/// <para>
/// Mouse buttons are routed through the mouse API rather than <c>keybd_event</c>: a mouse button's
/// virtual-key code is not a keyboard scan code, so sending it as a key press would do nothing.
/// </para>
/// <para>
/// All three methods return whether anything was sent, so an unbound hotkey is a no-op rather than
/// a stray keystroke.
/// </para>
/// </remarks>
public class InputHelper
{
    /// <summary>Presses and releases the hotkey, with its modifiers held around it.</summary>
    public static bool SendInputPress(HotkeyNodeV2.HotkeyNodeValue value)
    {
        if (!TryGetKey(value, out var key)) return false;

        SendModifiers(value, true);
        SendKey(key, true);
        SendKey(key, false);
        SendModifiers(value, false);
        return true;
    }

    /// <summary>Presses the hotkey down (modifiers first) and leaves it held.</summary>
    public static bool SendInputDown(HotkeyNodeV2.HotkeyNodeValue value)
    {
        if (!TryGetKey(value, out var key)) return false;

        SendModifiers(value, true);
        SendKey(key, true);
        return true;
    }

    /// <summary>Releases the hotkey and then its modifiers.</summary>
    public static bool SendInputUp(HotkeyNodeV2.HotkeyNodeValue value)
    {
        if (!TryGetKey(value, out var key)) return false;

        SendKey(key, false);
        SendModifiers(value, false);
        return true;
    }

    private static bool TryGetKey(HotkeyNodeV2.HotkeyNodeValue value, out Keys key)
    {
        key = value?.Key ?? Keys.None;
        return key != Keys.None;
    }

    private static void SendModifiers(HotkeyNodeV2.HotkeyNodeValue value, bool down)
    {
        if (value.Ctrl) SendKey(Keys.ControlKey, down);
        if (value.Shift) SendKey(Keys.ShiftKey, down);
        if (value.Alt) SendKey(Keys.Menu, down);
        if (value.Win) SendKey(Keys.LWin, down);
    }

    private static void SendKey(Keys key, bool down)
    {
        switch (key)
        {
            case Keys.LButton:
                WinApi.mouse_event(down ? Input.MOUSEEVENTF_LEFTDOWN : Input.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                break;
            case Keys.RButton:
                WinApi.mouse_event(down ? Input.MOUSEEVENTF_RIGHTDOWN : Input.MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                break;
            case Keys.MButton:
                WinApi.mouse_event(down ? Input.MOUSEEVENTF_MIDDOWN : Input.MOUSEEVENTF_MIDUP, 0, 0, 0, 0);
                break;
            default:
                if (down)
                    Input.KeyDown(key);
                else
                    Input.KeyUp(key);

                break;
        }
    }
}
