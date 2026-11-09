#nullable enable
using AndroidSyncControl.Mcp.Extensions;
using AndroidSyncControl.Mcp.Helpers;
using AndroidSyncControl.Mcp.Interfaces;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TqkLibrary.Scrcpy.Enums;

namespace AndroidSyncControl.Mcp.Tools;

[McpServerToolType]
public sealed class InputTools
{
    const int MaxInjectTextLength = 300;
    const string DeviceIdHelp = "Device id of a connected device.";
    const string ImageWidthHelp = "Width of the shrunk screenshot the point was read from. Omit for full-size coordinates.";
    const string ImageHeightHelp = "Height of the shrunk screenshot the point was read from. Omit for full-size coordinates.";

    readonly IDeviceHost _deviceHost;
    readonly TouchGesturePlayer _gesturePlayer;

    public InputTools(IDeviceHost deviceHost, TouchGesturePlayer gesturePlayer)
    {
        _deviceHost = deviceHost;
        _gesturePlayer = gesturePlayer;
    }

    [McpServerTool(Name = "tap"), Description("Tap one point on the screen.")]
    public async Task<string> TapAsync(
        [Description(DeviceIdHelp)] string deviceId,
        int x,
        int y,
        [Description(ImageWidthHelp)] int? imageWidth = null,
        [Description(ImageHeightHelp)] int? imageHeight = null,
        CancellationToken cancellationToken = default)
    {
        IDeviceSession session = await GetSessionAsync(deviceId, cancellationToken).ConfigureAwait(false);
        Point point = ScreenPointMapper.Map(x, y, imageWidth, imageHeight, session.ScreenSize);
        await _gesturePlayer.TapAsync(session, point, 0, cancellationToken).ConfigureAwait(false);
        return "ok";
    }

    [McpServerTool(Name = "long_press"), Description("Press and hold one point on the screen.")]
    public async Task<string> LongPressAsync(
        [Description(DeviceIdHelp)] string deviceId,
        int x,
        int y,
        [Description("How long to hold, in milliseconds.")] int durationMs = 800,
        [Description(ImageWidthHelp)] int? imageWidth = null,
        [Description(ImageHeightHelp)] int? imageHeight = null,
        CancellationToken cancellationToken = default)
    {
        IDeviceSession session = await GetSessionAsync(deviceId, cancellationToken).ConfigureAwait(false);
        Point point = ScreenPointMapper.Map(x, y, imageWidth, imageHeight, session.ScreenSize);
        await _gesturePlayer.TapAsync(session, point, durationMs, cancellationToken).ConfigureAwait(false);
        return "ok";
    }

    [McpServerTool(Name = "swipe"), Description("Drag a finger from one point to another.")]
    public async Task<string> SwipeAsync(
        [Description(DeviceIdHelp)] string deviceId,
        int fromX,
        int fromY,
        int toX,
        int toY,
        [Description("Gesture length in milliseconds.")] int durationMs = 300,
        [Description(ImageWidthHelp)] int? imageWidth = null,
        [Description(ImageHeightHelp)] int? imageHeight = null,
        CancellationToken cancellationToken = default)
    {
        IDeviceSession session = await GetSessionAsync(deviceId, cancellationToken).ConfigureAwait(false);
        Size screen = session.ScreenSize;
        Point from = ScreenPointMapper.Map(fromX, fromY, imageWidth, imageHeight, screen);
        Point to = ScreenPointMapper.Map(toX, toY, imageWidth, imageHeight, screen);
        await _gesturePlayer.SwipeAsync(session, from, to, durationMs, cancellationToken).ConfigureAwait(false);
        return "ok";
    }

    [McpServerTool(Name = "scroll"), Description("Send a scroll wheel event at one point.")]
    public async Task<string> ScrollAsync(
        [Description(DeviceIdHelp)] string deviceId,
        int x,
        int y,
        [Description("Vertical amount between -1 and 1, positive scrolls up.")] float vertical,
        [Description("Horizontal amount between -1 and 1.")] float horizontal = 0,
        [Description(ImageWidthHelp)] int? imageWidth = null,
        [Description(ImageHeightHelp)] int? imageHeight = null,
        CancellationToken cancellationToken = default)
    {
        if (vertical < -1 || vertical > 1 || horizontal < -1 || horizontal > 1)
            throw new McpException("vertical and horizontal must be between -1 and 1.");

        IDeviceSession session = await GetSessionAsync(deviceId, cancellationToken).ConfigureAwait(false);
        Size screen = session.ScreenSize;
        Point point = ScreenPointMapper.Map(x, y, imageWidth, imageHeight, screen);
        Ensure(session.InputControl.InjectScrollEvent(new Rectangle(point, screen), vertical, horizontal, AndroidMotionEventButton.None), deviceId);
        return "ok";
    }

    [McpServerTool(Name = "key"), Description("Press and release an Android key.")]
    public async Task<string> KeyAsync(
        [Description(DeviceIdHelp)] string deviceId,
        [Description("Key name such as HOME, BACK, ENTER, DEL, APP_SWITCH, POWER.")] string key,
        CancellationToken cancellationToken = default)
    {
        AndroidKeyCode keyCode = AndroidKeyParser.Parse(key);
        IDeviceSession session = await GetSessionAsync(deviceId, cancellationToken).ConfigureAwait(false);
        Ensure(session.InputControl.InjectKeycode(AndroidKeyEventAction.ACTION_DOWN, keyCode, 0, AndroidKeyEventMeta.META_NONE), deviceId);
        Ensure(session.InputControl.InjectKeycode(AndroidKeyEventAction.ACTION_UP, keyCode, 0, AndroidKeyEventMeta.META_NONE), deviceId);
        return "ok";
    }

    [McpServerTool(Name = "text"), Description("Type text into the focused field. Long text or text with non-ASCII characters is pasted through the device clipboard, replacing its content.")]
    public async Task<string> TextAsync(
        [Description(DeviceIdHelp)] string deviceId,
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(text))
            throw new McpException("text must not be empty.");

        IDeviceSession session = await GetSessionAsync(deviceId, cancellationToken).ConfigureAwait(false);
        // scrcpy's inject_text only supports short ASCII text.
        bool sent = text.Length <= MaxInjectTextLength && text.All(char.IsAscii)
            ? session.InputControl.InjectText(text)
            : session.InputControl.SetClipboard(text, true);
        Ensure(sent, deviceId);
        return "ok";
    }

    [McpServerTool(Name = "start_app"), Description("Start an Android app.")]
    public async Task<string> StartAppAsync(
        [Description(DeviceIdHelp)] string deviceId,
        [Description("Package name, for example com.android.settings.")] string packageName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            throw new McpException("packageName must not be empty.");

        IDeviceSession session = await GetSessionAsync(deviceId, cancellationToken).ConfigureAwait(false);
        Ensure(session.InputControl.StartApp(packageName), deviceId);
        return "ok";
    }

    Task<IDeviceSession> GetSessionAsync(string deviceId, CancellationToken cancellationToken)
        => _deviceHost.GetConnectedAsync(deviceId, cancellationToken);

    static void Ensure(bool sent, string deviceId)
    {
        if (!sent)
            throw new McpException($"Device '{deviceId}' did not accept the input.");
    }
}
