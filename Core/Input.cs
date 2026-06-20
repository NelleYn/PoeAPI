#define DebugKeys
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using ExileCore.Shared;
using ExileCore.Shared.Helpers;
using MoreLinq.Extensions;
using SharpDX;

namespace ExileCore;

/// <summary>
/// Static input helper that tracks registered key states each frame and provides synthetic
/// mouse and keyboard input via the Win32 API. Keys must be registered with <see cref="RegisterKey"/>
/// before being queried with <see cref="IsKeyDown(Keys)"/>.
/// </summary>
public class Input
{
    private const int KEYEVENTF_EXTENDEDKEY = 0x0001;
    private const int KEYEVENTF_KEYUP = 0x0002;
    private const int ACTION_DELAY = 1;
    private const int MOVEMENT_DELAY = 7;
    private const int CLICK_DELAY = 5;
    private const int KEY_PRESS_DELAY = 10;
    public const int MOUSEEVENTF_MOVE = 0x0001;
    public const int MOUSEEVENTF_LEFTDOWN = 0x02;
    public const int MOUSEEVENTF_LEFTUP = 0x04;
    public const int MOUSEEVENTF_MIDDOWN = 0x0020;
    public const int MOUSEEVENTF_MIDUP = 0x0040;
    public const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
    public const int MOUSEEVENTF_RIGHTUP = 0x0010;
    public const int MOUSE_EVENT_WHEEL = 0x800;
    private static readonly Dictionary<Keys, bool> Keys = new Dictionary<Keys, bool>();
    private static readonly HashSet<Keys> RegisteredKeys = new HashSet<Keys>();
    private static readonly object locker = new object();
    private static readonly WaitTime cursorPositionSmooth = new WaitTime(1);
    private static readonly WaitTime keyPress = new WaitTime(ACTION_DELAY);
    private static readonly Dictionary<Keys, bool> KeysPressed = new Dictionary<Keys, bool>();
    private static readonly Stopwatch sw = Stopwatch.StartNew();

    static Input()
    {
        foreach (Keys key in Enum.GetValues(typeof(Keys)))
        {
            KeysPressed[key] = false;
        }
    }

    /// <summary>The current cursor position read directly from the OS (not the per-frame cached value).</summary>
    public static Vector2 ForceMousePosition => WinApi.GetCursorPositionPoint();

    /// <summary>The cursor position captured during the last <see cref="Update"/> call.</summary>
    public static Vector2 MousePosition { get; private set; }

    /// <summary>The cached <see cref="MousePosition"/> as a <see cref="System.Numerics.Vector2"/>.</summary>
    public static System.Numerics.Vector2 MousePositionNum => MousePosition.ToVector2Num();

    /// <summary>Returns whether the registered key (by virtual-key code) was down at the last update.</summary>
    public static bool IsKeyDown(int nVirtKey)
    {
        return IsKeyDown((Keys) nVirtKey);
    }

    /// <summary>Returns whether the registered key was down at the last update.</summary>
    public static bool IsKeyDown(Keys nVirtKey)
    {
#if DebugKeys
        if (!Keys.ContainsKey(nVirtKey))
            DebugWindow.LogError($"Key '{nVirtKey}' is not registered. Use {nameof(Input)}.{nameof(RegisterKey)}(Settings.MyKey) in Initialize function for registration.", 10);
#endif

        return Keys[nVirtKey];
    }

    /// <summary>Queries the live key state directly from the OS.</summary>
    public static bool GetKeyState(Keys key)
    {
        return WinApi.GetKeyState(key) < 0;
    }

    /// <summary>Registers a key so its state is tracked each frame and can be queried with <see cref="IsKeyDown(Keys)"/>.</summary>
    public static void RegisterKey(Keys key)
    {
        if (!Keys.TryGetValue(key, out _))
        {
            lock (locker)
            {
                Keys[key] = false;
                RegisteredKeys.Add(key);
            }
        }
    }

    /// <summary>Raised when a tracked key transitions from down to up during <see cref="Update"/>.</summary>
    public static event EventHandler<Keys> ReleaseKey;

    /// <summary>Refreshes the cached mouse position and all tracked key states; raises <see cref="ReleaseKey"/> on releases.</summary>
    public static void Update(IntPtr windowPtr)
    {
        MousePosition = WinApi.GetCursorPosition(windowPtr);

        try
        {
            RegisteredKeys.ForEach(key =>
            {
                var keyState = GetKeyState(key);
                if (keyState == false && Keys[key]) ReleaseKey?.Invoke(null, key);

                Keys[key] = keyState;
            });
        }
        catch (Exception e)
        {
            DebugWindow.LogMsg($"{nameof(Input)} {e}");
        }
    }

    /// <summary>Coroutine that moves the cursor smoothly towards the target position.</summary>
    public static IEnumerator SetCursorPositionSmooth(Vector2 vec)
    {
        var step = Math.Max(vec.Distance(ForceMousePosition) / 100, 4);

        if (step > 6)
        {
            for (var i = 0; i < step; i++)
            {
                var vector2 = Vector2.SmoothStep(ForceMousePosition, vec, i / step);
                SetCursorPos(vector2);
                MouseMove();
                yield return cursorPositionSmooth;
            }
        }
        else
            SetCursorPos(vec);
    }

    /// <summary>Scrolls the mouse wheel the given number of clicks, forward or backward.</summary>
    public static void VerticalScroll(bool forward, int clicks)
    {
        if (forward)
            WinApi.mouse_event(MOUSE_EVENT_WHEEL, 0, 0, clicks * 120, 0);
        else
            WinApi.mouse_event(MOUSE_EVENT_WHEEL, 0, 0, -(clicks * 120), 0);
    }

    /// <summary>Moves the cursor to the given screen position.</summary>
    public static void SetCursorPos(Vector2 vec)
    {
        WinApi.SetCursorPos((int) vec.X, (int) vec.Y);
        MouseMove();
    }

    /// <summary>Performs a synthetic click with the given mouse button.</summary>
    public static void Click(MouseButtons buttons)
    {
        switch (buttons)
        {
            case MouseButtons.Left:
                MouseMove();
                WinApi.mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                break;
            case MouseButtons.Right:
                MouseMove();
                WinApi.mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                break;
        }
    }

    /// <summary>Presses the left mouse button down.</summary>
    public static void LeftDown()
    {
        WinApi.mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
    }

    /// <summary>Releases the left mouse button.</summary>
    public static void LeftUp()
    {
        WinApi.mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
    }

    /// <summary>Sends a synthetic mouse-move event at the current position (used to wake the cursor).</summary>
    public static void MouseMove()
    {
        WinApi.mouse_event(MOUSEEVENTF_MOVE, 0, 0, 0, 0);
    }

    /// <summary>Coroutine that presses and releases the given key with a short delay.</summary>
    public static IEnumerator KeyPress(Keys key)
    {
        KeyDown(key);
        yield return keyPress;
        KeyUp(key);
    }

    /// <summary>Presses the given key down via a synthetic keyboard event.</summary>
    public static void KeyDown(Keys key)
    {
        WinApi.keybd_event((byte) key, 0, KEYEVENTF_EXTENDEDKEY | 0, 0);
    }

    /// <summary>Releases the given key via a synthetic keyboard event.</summary>
    public static void KeyUp(Keys key)
    {
        WinApi.keybd_event((byte) key, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
    }

    /// <summary>Posts a key-down message to the given window handle.</summary>
    public static void KeyDown(Keys key, IntPtr handle)
    {
        WinApi.SendMessage(handle, 0x100, (int) key, 0);
    }

    /// <summary>Posts a key-up message to the given window handle.</summary>
    public static void KeyUp(Keys key, IntPtr handle)
    {
        WinApi.SendMessage(handle, 0x101, (int) key, 0);
    }

    /// <summary>Toggles the key down/up for the given window handle, rate-limited by the press delay.</summary>
    public static void KeyPressRelease(Keys key, IntPtr handle)
    {
        if (sw.ElapsedMilliseconds >= KEY_PRESS_DELAY && KeysPressed[key])
        {
            KeyUp(key, handle);

            lock (locker)
            {
                KeysPressed[key] = false;
            }

            sw.Restart();
        }
        else if (!KeysPressed[key])
        {
            KeyDown(key, handle);

            lock (locker)
            {
                KeysPressed[key] = true;
            }

            sw.Restart();
        }
    }

    /// <summary>Toggles the key down/up via synthetic events, rate-limited by the press delay.</summary>
    public static void KeyPressRelease(Keys key)
    {
        if (sw.ElapsedMilliseconds >= KEY_PRESS_DELAY && KeysPressed[key])
        {
            KeyUp(key);

            lock (locker)
            {
                KeysPressed[key] = false;
            }

            sw.Restart();
        }
        else if (!KeysPressed[key])
        {
            KeyDown(key);

            lock (locker)
            {
                KeysPressed[key] = true;
            }

            sw.Restart();
        }
    }
}
