using ExileCore.Shared.Enums;
using SharpDX;

namespace ExileCore.Shared.Helpers;

/// <summary>
/// Helper methods for computing sprite-sheet UV rectangles for map icons.
/// </summary>
public static class SpriteHelper
{
    /// <summary>
    /// Returns the UV rectangle for the given custom map icon.
    /// </summary>
    /// <param name="index">The custom map icon index.</param>
    /// <returns>The UV rectangle.</returns>
    public static RectangleF GetUV(MyMapIconsIndex index)
    {
        return GetUV((int) index, Constants.MyMapIcons);
    }

    /// <summary>
    /// Returns the UV rectangle for the given map icon.
    /// </summary>
    /// <param name="index">The map icon index.</param>
    /// <returns>The UV rectangle.</returns>
    public static RectangleF GetUV(MapIconsIndex index)
    {
        return GetUV((int) index, Constants.MapIconsSize);
    }

    /// <summary>
    /// Computes the UV rectangle for a sprite at the given linear index within a sheet of the given size.
    /// </summary>
    /// <param name="index">The linear sprite index.</param>
    /// <param name="size">The sprite-sheet dimensions, in sprites.</param>
    /// <returns>The UV rectangle.</returns>
    public static RectangleF GetUV(int index, Size2F size)
    {
        if (index % (int) size.Width == 0)
        {
            return new RectangleF((size.Width - 1) / size.Width, ((int) (index / size.Width) - 1) / size.Height, 1 / size.Width,
                1 / size.Height);
        }

        return new RectangleF((index % size.Width - 1) / size.Width, index / (int) size.Width / size.Height, 1 / size.Width,
            1 / size.Height);
    }

    /// <summary>
    /// Computes the UV rectangle for a sprite at the given column/row within a sheet of the given size.
    /// </summary>
    /// <param name="index">The sprite column (Width) and row (Height).</param>
    /// <param name="size">The sprite-sheet dimensions, in sprites.</param>
    /// <returns>The UV rectangle.</returns>
    public static RectangleF GetUV(Size2 index, Size2F size)
    {
        return new RectangleF((index.Width - 1) / size.Width, (index.Height - 1) / size.Height, 1 / size.Width, 1 / size.Height);
    }

    /// <summary>
    /// Computes the UV rectangle for a sprite at the given column/row within a sheet of the given size.
    /// </summary>
    /// <param name="x">The sprite column (1-based).</param>
    /// <param name="y">The sprite row (1-based).</param>
    /// <param name="width">The sprite-sheet width, in sprites.</param>
    /// <param name="height">The sprite-sheet height, in sprites.</param>
    /// <returns>The UV rectangle.</returns>
    public static RectangleF GetUV(int x, int y, float width, float height)
    {
        return new RectangleF((x - 1) / width, (y - 1) / height, 1 / width, 1 / height);
    }
}
