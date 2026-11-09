#nullable enable
using ModelContextProtocol;
using System;
using System.Drawing;

namespace AndroidSyncControl.Mcp.Helpers;

public static class ScreenPointMapper
{
    /// <summary>
    /// Converts a point to screen coordinates. Pass the size of a downscaled screenshot when the
    /// point was read from one; leave both null when the point is already in screen coordinates.
    /// </summary>
    /// <exception cref="McpException">The sizes are invalid or the point is outside the screen.</exception>
    public static Point Map(int x, int y, int? imageWidth, int? imageHeight, Size screen)
    {
        if (screen.Width <= 0 || screen.Height <= 0)
            throw new McpException("The screen size is not available yet, take a screenshot and retry.");
        if (imageWidth.HasValue != imageHeight.HasValue)
            throw new McpException("imageWidth and imageHeight must be passed together.");

        int width = imageWidth ?? screen.Width;
        int height = imageHeight ?? screen.Height;
        if (width <= 0 || height <= 0)
            throw new McpException("imageWidth and imageHeight must be positive.");
        if (x < 0 || y < 0 || x >= width || y >= height)
            throw new McpException($"Point ({x}, {y}) is outside the {width}x{height} area.");

        return new Point(Scale(x, width, screen.Width), Scale(y, height, screen.Height));
    }

    static int Scale(int value, int from, int to)
        => Math.Min(to - 1, (int)((long)value * to / from));
}
