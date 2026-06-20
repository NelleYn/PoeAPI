using ImGuiNET;

namespace ExileCore.RenderQ;

/// <summary>
/// Holds a loaded ImGui font atlas together with its display name and pixel size.
/// </summary>
public unsafe struct FontContainer
{
    /// <summary>The native ImGui font atlas pointer.</summary>
    public ImFont* Atlas { get; }

    /// <summary>The font name.</summary>
    public string Name { get; }

    /// <summary>The font size in pixels.</summary>
    public int Size { get; }

    /// <summary>
    /// Initializes a new <see cref="FontContainer"/> wrapping the given atlas, name and size.
    /// </summary>
    public FontContainer(ImFont* atlas, string Name, int Size)
    {
        Atlas = atlas;
        this.Name = Name;
        this.Size = Size;
    }
}
