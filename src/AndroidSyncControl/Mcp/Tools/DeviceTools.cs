#nullable enable
using AndroidSyncControl.Mcp.Interfaces;
using AndroidSyncControl.Mcp.Models;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AndroidSyncControl.Mcp.Tools;

[McpServerToolType]
public sealed class DeviceTools
{
    const string OnlineState = "Device";

    readonly IDeviceHost _deviceHost;
    readonly IAdbCommandRunner _adbCommandRunner;

    public DeviceTools(IDeviceHost deviceHost, IAdbCommandRunner adbCommandRunner)
    {
        _deviceHost = deviceHost;
        _adbCommandRunner = adbCommandRunner;
    }

    [McpServerTool(Name = "device_list"), Description("Scan the Android devices visible to adb and report which ones have an open scrcpy session.")]
    public async Task<IReadOnlyList<DeviceStatus>> ListAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<AdbDeviceInfo> devices = await _adbCommandRunner.ListDevicesAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<IDeviceSession> sessions = await _deviceHost.GetSessionsAsync(cancellationToken).ConfigureAwait(false);

        List<DeviceStatus> result = devices
            .Select(device => ToStatus(device.DeviceId, device.State, sessions.FirstOrDefault(x => x.DeviceId == device.DeviceId)))
            .ToList();
        result.AddRange(sessions
            .Where(session => devices.All(device => device.DeviceId != session.DeviceId))
            .Select(session => ToStatus(session.DeviceId, null, session)));
        return result;
    }

    [McpServerTool(Name = "device_connect"), Description("Open a scrcpy session on a device, which also shows it in the app window. Required before screenshot and input tools. Does nothing if the device is already open.")]
    public async Task<DeviceStatus> ConnectAsync(
        [Description("Device id from device_list.")] string deviceId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AdbDeviceInfo> devices = await _adbCommandRunner.ListDevicesAsync(cancellationToken).ConfigureAwait(false);
        AdbDeviceInfo? device = devices.FirstOrDefault(x => x.DeviceId == deviceId);
        if (device is null)
            throw new McpException($"Device '{deviceId}' was not found by adb.");
        if (!string.Equals(device.State, OnlineState, StringComparison.OrdinalIgnoreCase))
            throw new McpException($"Device '{deviceId}' is not ready, its adb state is {device.State}.");
        if (deviceId.Any(char.IsWhiteSpace))
            throw new McpException($"Device id '{deviceId}' contains spaces, which is not supported.");

        IDeviceSession session = await _deviceHost.ConnectAsync(deviceId, cancellationToken).ConfigureAwait(false);
        return ToStatus(deviceId, device.State, session);
    }

    [McpServerTool(Name = "device_disconnect"), Description("Close the scrcpy session of a device and remove it from the app window.")]
    public async Task<string> DisconnectAsync(
        [Description("Device id of a connected device.")] string deviceId,
        CancellationToken cancellationToken)
    {
        bool closed = await _deviceHost.DisconnectAsync(deviceId, cancellationToken).ConfigureAwait(false);
        return closed ? $"Device '{deviceId}' disconnected." : $"Device '{deviceId}' was not connected.";
    }

    static DeviceStatus ToStatus(string deviceId, string? adbState, IDeviceSession? session)
    {
        bool isConnected = session?.IsConnected ?? false;
        var screen = isConnected ? session!.ScreenSize : System.Drawing.Size.Empty;
        return new DeviceStatus
        {
            DeviceId = deviceId,
            AdbState = adbState,
            IsConnected = isConnected,
            ScreenWidth = screen.Width,
            ScreenHeight = screen.Height,
        };
    }
}
