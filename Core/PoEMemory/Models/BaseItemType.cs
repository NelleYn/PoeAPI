using System.Text;

namespace ExileCore.PoEMemory.Models;

/// <summary>
/// Describes a base item type as defined in the game's "BaseItemTypes.dat" file,
/// including its class, inventory dimensions, drop level and associated tags.
/// </summary>
public class BaseItemType
{
    /// <summary>Gets or sets the metadata path identifying this base item type.</summary>
    public string Metadata { get; set; }

    /// <summary>Gets or sets the item class name (e.g. "Body Armour", "Bow").</summary>
    public string ClassName { get; set; }

    /// <summary>Gets or sets the inventory width occupied by the item, in cells.</summary>
    public int Width { get; set; }

    /// <summary>Gets or sets the inventory height occupied by the item, in cells.</summary>
    public int Height { get; set; }

    /// <summary>Gets or sets the minimum area level at which the item can drop.</summary>
    public int DropLevel { get; set; }

    /// <summary>Gets or sets the display name of the base item.</summary>
    public string BaseName { get; set; }

    /// <summary>Gets or sets the tags declared on the base item type.</summary>
    public string[] Tags { get; set; }

    /// <summary>Gets or sets additional tags derived from the item's metadata path.</summary>
    public string[] MoreTagsFromPath { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var str = new StringBuilder();
        str.Append("Tags: ");

        foreach (var tag in Tags)
        {
            str.Append(tag);
            str.Append(" ");
        }

        str.Append("More Tags: ");

        foreach (var s in MoreTagsFromPath)
        {
            str.Append(s);
            str.Append(" ");
        }

        return str.ToString();
    }
}
