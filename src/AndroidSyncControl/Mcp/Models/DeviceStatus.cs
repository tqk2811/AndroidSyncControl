#nullable enable

namespace AndroidSyncControl.Mcp.Models;

public sealed class DeviceStatus
{
    public required string DeviceId { get; init; }

    /// <summary>adb state, or null when adb no longer lists a device that still has a session.</summary>
    public required string? AdbState { get; init; }

    public required bool IsConnected { get; init; }
    public required int ScreenWidth { get; init; }
    public required int ScreenHeight { get; init; }
}
