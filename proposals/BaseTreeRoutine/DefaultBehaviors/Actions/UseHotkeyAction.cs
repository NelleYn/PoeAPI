// EXPERIMENTAL candidate ported from exApiTools/BasicFlaskRoutine — see proposals/BaseTreeRoutine/README.md. Not part of the build.

using System;
using System.Windows.Forms;
using ExileCore.TreeRoutine.TreeSharp;
using Action = ExileCore.TreeRoutine.TreeSharp.Action;

namespace ExileCore.TreeRoutine.DefaultBehaviors.Actions;

/// <summary>
/// Leaf action that presses a bound key. Ported from upstream
/// <c>TreeRoutine/DefaultBehaviors/Actions/UseHotkeyAction.cs</c>, whose <c>Run</c> posted
/// <c>WM_KEYDOWN</c>/<c>WM_KEYUP</c> to the game window via a <c>SendMessage</c> P/Invoke. The
/// fork-native equivalent is to drive the engine <see cref="Input"/> API directly — no P/Invoke and
/// no external <c>WindowsInput.InputSimulator</c> dependency — via the rate-limited
/// <see cref="Input.KeyPressRelease(Keys)"/>.
/// </summary>
public class UseHotkeyAction : Action
{
    private readonly Func<Keys> _keyResolver;

    /// <summary>Press a fixed key.</summary>
    public UseHotkeyAction(Keys key)
    {
        _keyResolver = () => key;
    }

    /// <summary>Resolve the key lazily each run (e.g. from a <c>HotkeyNode</c> setting that may change).</summary>
    public UseHotkeyAction(Func<Keys> keyResolver)
    {
        _keyResolver = keyResolver;
    }

    protected override RunStatus Run(object context)
    {
        var key = _keyResolver?.Invoke() ?? Keys.None;
        if (key == Keys.None)
            return RunStatus.Failure;

        Input.KeyPressRelease(key); // fork: rate-limited (~10 ms) down/up toggle
        return RunStatus.Success;
    }
}
