using System;
using ExileCore.PoEMemory.MemoryObjects;
using SharpDX;

namespace ExileCore;

/// <summary>
/// Represents a concrete in-game area instance, identified by its area template and hash,
/// together with derived metadata such as town/hideout/waypoint flags and entry time.
/// </summary>
public sealed class AreaInstance
{
    /// <summary>The hash of the area the player is currently in.</summary>
    public static uint CurrentHash;

    /// <summary>Creates an area instance from its template, instance hash and effective level.</summary>
    public AreaInstance(AreaTemplate area, uint hash, int realLevel)
    {
        Area = area;
        Hash = hash;
        RealLevel = realLevel;
        Name = area.Name;
        Act = area.Act;
        IsTown = area.IsTown;
        IsHideout = Name.Contains("Hideout") && !Name.Contains("Syndicate Hideout");
        HasWaypoint = area.HasWaypoint || IsHideout;
    }

    public int RealLevel { get; }
    public string Name { get; }
    public int Act { get; }
    public bool IsTown { get; }
    public bool IsHideout { get; }
    public bool HasWaypoint { get; }
    public uint Hash { get; }
    public AreaTemplate Area { get; }
    public string DisplayName => string.Concat(Name, " (", RealLevel, ")");
    public DateTime TimeEntered { get; } = DateTime.UtcNow;
    public Color AreaColorName { get; set; } = Color.Aqua;

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Name} ({RealLevel}) #{Hash}";
    }

    /// <summary>Formats a time span as <c>h:mm:ss</c> (hours omitted when zero).</summary>
    public static string GetTimeString(TimeSpan timeSpent)
    {
        var allsec = (int) timeSpent.TotalSeconds;
        var secs = allsec % 60;
        var mins = allsec / 60;
        var hours = mins / 60;
        mins = mins % 60;
        return string.Format(hours > 0 ? "{0}:{1:00}:{2:00}" : "{1}:{2:00}", hours, mins, secs);
    }
}
