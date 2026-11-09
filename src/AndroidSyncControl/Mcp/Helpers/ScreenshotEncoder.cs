#nullable enable
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace AndroidSyncControl.Mcp.Helpers;

public static class ScreenshotEncoder
{
    const long JpegQuality = 85;
    const int SampleGrid = 8;

    /// <summary>
    /// A bitmap the native capture did not write to is still fully zeroed, even its alpha channel.
    /// </summary>
    public static bool LooksUnwritten(Bitmap bitmap)
    {
        for (int row = 0; row < SampleGrid; row++)
        {
            for (int column = 0; column < SampleGrid; column++)
            {
                int x = bitmap.Width * column / SampleGrid;
                int y = bitmap.Height * row / SampleGrid;
                if (bitmap.GetPixel(x, y).ToArgb() != 0)
                    return false;
            }
        }
        return true;
    }

    /// <summary>Size after shrinking the longest edge to <paramref name="maxSize"/>, 0 keeps the size.</summary>
    public static Size FitWithin(Size size, int maxSize)
    {
        int longest = Math.Max(size.Width, size.Height);
        if (maxSize <= 0 || longest <= maxSize)
            return size;
        return new Size(
            Math.Max(1, (int)((long)size.Width * maxSize / longest)),
            Math.Max(1, (int)((long)size.Height * maxSize / longest)));
    }

    public static byte[] Encode(Bitmap bitmap, Size outputSize, bool usePng)
    {
        if (outputSize == bitmap.Size)
            return Save(bitmap, usePng);

        using Bitmap resized = new Bitmap(outputSize.Width, outputSize.Height, PixelFormat.Format24bppRgb);
        using (Graphics graphics = Graphics.FromImage(resized))
        {
            graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
            graphics.DrawImage(bitmap, new Rectangle(Point.Empty, outputSize));
        }
        return Save(resized, usePng);
    }

    static byte[] Save(Bitmap bitmap, bool usePng)
    {
        using MemoryStream stream = new MemoryStream();
        if (usePng)
        {
            bitmap.Save(stream, ImageFormat.Png);
        }
        else
        {
            ImageCodecInfo codec = ImageCodecInfo.GetImageEncoders().First(x => x.FormatID == ImageFormat.Jpeg.Guid);
            using EncoderParameters parameters = new EncoderParameters(1);
            parameters.Param[0] = new EncoderParameter(Encoder.Quality, JpegQuality);
            bitmap.Save(stream, codec, parameters);
        }
        return stream.ToArray();
    }
}
