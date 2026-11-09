#nullable enable
using AndroidSyncControl.Mcp.Extensions;
using AndroidSyncControl.Mcp.Helpers;
using AndroidSyncControl.Mcp.Interfaces;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AndroidSyncControl.Mcp.Tools;

[McpServerToolType]
public sealed class ScreenTools
{
    const int CaptureAttempts = 20;
    const int CaptureRetryDelayMs = 100;

    readonly IDeviceHost _deviceHost;

    public ScreenTools(IDeviceHost deviceHost)
    {
        _deviceHost = deviceHost;
    }

    [McpServerTool(Name = "screenshot", ReadOnly = true), Description("Capture the current screen of a connected device. The result includes the image size and the screen size used by the input tools.")]
    public async Task<CallToolResult> CaptureAsync(
        [Description("Device id of a connected device.")] string deviceId,
        [Description("Shrink the longest edge to this many pixels, 0 keeps the full size. When shrunk, pass imageWidth and imageHeight to the input tools.")] int maxSize = 0,
        [Description("Image format: jpeg or png.")] string format = "jpeg",
        CancellationToken cancellationToken = default)
    {
        bool usePng = ParseFormat(format);
        if (maxSize < 0)
            throw new McpException("maxSize must not be negative.");

        IDeviceSession session = await _deviceHost.GetConnectedAsync(deviceId, cancellationToken).ConfigureAwait(false);
        using Bitmap bitmap = await CaptureFrameAsync(session, cancellationToken).ConfigureAwait(false);
        bool unwritten = ScreenshotEncoder.LooksUnwritten(bitmap);

        Size imageSize = ScreenshotEncoder.FitWithin(bitmap.Size, maxSize);
        byte[] image = ScreenshotEncoder.Encode(bitmap, imageSize, usePng);
        string description = JsonSerializer.Serialize(new
        {
            imageWidth = imageSize.Width,
            imageHeight = imageSize.Height,
            screenWidth = bitmap.Width,
            screenHeight = bitmap.Height,
            warning = unwritten ? "No video frame arrived yet, this image may be blank." : null,
        });

        return new CallToolResult
        {
            Content = new List<ContentBlock>
            {
                ImageContentBlock.FromBytes(image, usePng ? "image/png" : "image/jpeg"),
                new TextContentBlock { Text = description },
            },
        };
    }

    /// <summary>Retries while no frame has been decoded, then returns the last capture as it is.</summary>
    static async Task<Bitmap> CaptureFrameAsync(IDeviceSession session, CancellationToken cancellationToken)
    {
        for (int attempt = 1; ; attempt++)
        {
            Bitmap? bitmap = await Task.Run(session.CaptureScreen, cancellationToken).ConfigureAwait(false);
            bool usable = bitmap is not null && !ScreenshotEncoder.LooksUnwritten(bitmap);
            if (usable || (attempt >= CaptureAttempts && bitmap is not null))
                return bitmap!;
            bitmap?.Dispose();
            if (attempt >= CaptureAttempts)
                throw new McpException($"Device '{session.DeviceId}' returned no screen image.");
            await Task.Delay(CaptureRetryDelayMs, cancellationToken).ConfigureAwait(false);
        }
    }

    static bool ParseFormat(string format)
    {
        if (string.Equals(format, "png", StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(format, "jpeg", StringComparison.OrdinalIgnoreCase) || string.Equals(format, "jpg", StringComparison.OrdinalIgnoreCase))
            return false;
        throw new McpException("format must be jpeg or png.");
    }
}
