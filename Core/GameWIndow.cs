using System;
using System.Diagnostics;
using ExileCore.Shared;
using ExileCore.Shared.Cache;
using SharpDX;
using Rectangle = System.Drawing.Rectangle;

namespace ExileCore;

/// <summary>
/// Wraps the Path of Exile game window, exposing its client rectangle (cached and uncached)
/// and helpers for foreground/coordinate queries.
/// </summary>
public class GameWindow
{
    private readonly IntPtr handle;
    private readonly CachedValue<RectangleF> _getWindowRectangle;
    private Rectangle _lastValid = Rectangle.Empty;

    /// <summary>Creates a window wrapper around the supplied game process.</summary>
    public GameWindow(Process process)
    {
        Process = process;
        handle = process.MainWindowHandle;
        _getWindowRectangle = new TimeCache<RectangleF>(GetWindowRectangleReal, 200);
    }

    /// <summary>The underlying game process.</summary>
    public Process Process { get; }

    /// <summary>The window client rectangle, served from a short-lived time cache.</summary>
    public RectangleF GetWindowRectangleTimeCache => _getWindowRectangle.Value;

    /// <summary>Returns the window client rectangle from the time cache.</summary>
    public RectangleF GetWindowRectangle()
    {
        return _getWindowRectangle.Value;
    }

    /// <summary>Reads the window client rectangle directly from the OS, falling back to the last valid value.</summary>
    public RectangleF GetWindowRectangleReal()
    {
        var rectangle = WinApi.GetClientRectangle(handle);

        if (rectangle.Width < 0 && rectangle.Height < 0)
            rectangle = _lastValid;
        else
            _lastValid = rectangle;

        return new RectangleF(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
    }

    /// <summary>Returns whether the game window is currently the foreground window.</summary>
    public bool IsForeground()
    {
        return WinApi.IsForegroundWindow(handle);
    }

    /// <summary>Converts a screen-space point to window client coordinates.</summary>
    public Vector2 ScreenToClient(int x, int y)
    {
        var point = new Point(x, y);
        WinApi.ScreenToClient(handle, ref point);
        return point;
    }
}
