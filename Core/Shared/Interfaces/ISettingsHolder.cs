using System;
using System.Collections.Generic;

namespace ExileCore.Shared.Interfaces;

/// <summary>
/// A node in the settings menu tree, holding a draw delegate and any child settings holders.
/// </summary>
public interface ISettingsHolder
{
    /// <summary>Gets or sets the display name of this settings entry.</summary>
    string Name { get; set; }

    /// <summary>Gets or sets the tooltip shown for this settings entry.</summary>
    string Tooltip { get; set; }

    /// <summary>Gets a value that uniquely identifies this settings entry.</summary>
    string Unique { get; }

    /// <summary>Gets or sets the numeric identifier of this settings entry.</summary>
    int ID { get; set; }

    /// <summary>Gets or sets the delegate invoked to render this settings entry.</summary>
    Action DrawDelegate { get; set; }

    /// <summary>Gets the child settings entries nested under this one.</summary>
    IList<ISettingsHolder> Children { get; }

    /// <summary>Renders this settings entry and its children.</summary>
    void Draw();
}
