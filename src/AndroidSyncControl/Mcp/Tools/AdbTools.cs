#nullable enable
using AndroidSyncControl.Mcp.Helpers;
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
public sealed class AdbTools
{
    const int DefaultTimeoutMs = 30000;
    const int MaxTimeoutMs = 600000;
    const int MaxOutputChars = 100000;

    /// <summary>These act on the adb server, not on one device, and would break every open session.</summary>
    static readonly string[] ServerCommands = { "kill-server", "start-server", "reconnect", "connect", "disconnect" };

    readonly IAdbCommandRunner _adbCommandRunner;

    public AdbTools(IAdbCommandRunner adbCommandRunner)
    {
        _adbCommandRunner = adbCommandRunner;
    }

    [McpServerTool(Name = "adb_execute"), Description("Run an adb command on one device. The device does not need a scrcpy session. Output is text and is truncated when very long. push, pull and install read or write files on this PC.")]
    public async Task<AdbCommandResult> ExecuteAsync(
        [Description("Device id from device_list.")] string deviceId,
        [Description("Arguments after 'adb -s <device>', for example [\"shell\", \"pm\", \"list\", \"packages\"]. After 'shell', adb joins the items and the device shell splits them again, so quote paths with spaces for that shell.")] string[] arguments,
        [Description("Timeout in milliseconds, the command is killed after it.")] int timeoutMs = DefaultTimeoutMs,
        CancellationToken cancellationToken = default)
    {
        ValidateArguments(arguments);
        if (timeoutMs <= 0 || timeoutMs > MaxTimeoutMs)
            throw new McpException($"timeoutMs must be between 1 and {MaxTimeoutMs}.");

        // The id reaches adb unquoted, so only an id adb itself reported may be used.
        IReadOnlyList<AdbDeviceInfo> devices = await _adbCommandRunner.ListDevicesAsync(cancellationToken).ConfigureAwait(false);
        if (devices.All(x => x.DeviceId != deviceId))
            throw new McpException($"Device '{deviceId}' was not found by adb.");
        if (deviceId.Any(char.IsWhiteSpace))
            throw new McpException($"Device id '{deviceId}' contains spaces, which is not supported.");

        AdbCommandResult result = await _adbCommandRunner
            .ExecuteAsync(deviceId, WindowsCommandLine.Join(arguments), timeoutMs, cancellationToken)
            .ConfigureAwait(false);

        return new AdbCommandResult
        {
            ExitCode = result.ExitCode,
            Stdout = Truncate(result.Stdout),
            Stderr = Truncate(result.Stderr),
            TimedOut = result.TimedOut,
        };
    }

    static void ValidateArguments(string[] arguments)
    {
        if (arguments is null || arguments.Length == 0 || string.IsNullOrWhiteSpace(arguments[0]))
            throw new McpException("arguments must start with an adb command.");
        // A leading option such as -s would silently redirect the command to another device.
        if (arguments[0].StartsWith("-", StringComparison.Ordinal))
            throw new McpException("arguments must start with an adb command, not an option.");
        if (ServerCommands.Contains(arguments[0], StringComparer.OrdinalIgnoreCase))
            throw new McpException($"'{arguments[0]}' affects the adb server and is not allowed.");
    }

    static string Truncate(string text)
        => text.Length <= MaxOutputChars ? text : text.Substring(0, MaxOutputChars) + "\n[output truncated]";
}
