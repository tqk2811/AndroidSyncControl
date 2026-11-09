#nullable enable

namespace AndroidSyncControl.Mcp.Models;

public sealed class AdbDeviceInfo
{
    public required string DeviceId { get; init; }

    /// <summary>adb state, "Device" means online.</summary>
    public required string State { get; init; }
}
