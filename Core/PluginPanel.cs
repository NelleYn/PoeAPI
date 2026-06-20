using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.Shared.Enums;
using SharpDX;

namespace ExileCore;

/// <summary>
/// A drawing anchor that plugins can register against (e.g. the left or under map corner).
/// Tracks whether any registered plugin currently wants to draw on the panel.
/// </summary>
public class PluginPanel
{
    private readonly Direction direction;
    private readonly List<Func<bool>> settings = new List<Func<bool>>();

    /// <summary>Creates a panel anchored at the supplied draw point.</summary>
    public PluginPanel(Vector2 startDrawPoint, Direction direction = Direction.Down) : this(direction)
    {
        StartDrawPoint = startDrawPoint;
    }

    /// <summary>Creates a panel laid out in the supplied direction.</summary>
    public PluginPanel(Direction direction = Direction.Down)
    {
        this.direction = direction;
        Margin = new Vector2(0, 0);
    }

    /// <summary>True when at least one registered plugin currently wants to use the panel.</summary>
    public bool Used => settings.Any(x => x.Invoke());

    /// <summary>The point at which drawing on the panel begins.</summary>
    public Vector2 StartDrawPoint { get; set; }

    /// <summary>The margin applied around panel content.</summary>
    public Vector2 Margin { get; }

    /// <summary>Registers a predicate indicating whether a plugin wants to use the panel.</summary>
    public void WantUse(Func<bool> enabled)
    {
        settings.Add(enabled);
    }
}
