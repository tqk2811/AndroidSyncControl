#nullable enable
using AndroidSyncControl.Mcp.Helpers;
using AndroidSyncControl.Mcp.Interfaces;
using AndroidSyncControl.Mcp.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TqkLibrary.AdbDotNet;

namespace AndroidSyncControl.Mcp;

public sealed class AdbCommandRunner : IAdbCommandRunner
{
    public async Task<IReadOnlyList<AdbDeviceInfo>> ListDevicesAsync(CancellationToken cancellationToken)
    {
        ProcessResult result = await Adb.BuildAdbCommand("devices").ExecuteAsync(cancellationToken, true).ConfigureAwait(false);
        return AdbDevicesParser.Parse(result.Stdout());
    }

    public async Task<AdbCommandResult> ExecuteAsync(string deviceId, string arguments, int timeoutMs, CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(timeoutMs);

        // A cancelled token kills adb and the partial output is still returned.
        ProcessResult result = await new Adb(deviceId)
            .BuildAdbDeviceCommand(arguments)
            .ExecuteAsync(timeout.Token, false)
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        return new AdbCommandResult
        {
            ExitCode = result.ExitCode,
            Stdout = result.Stdout(),
            Stderr = result.Stderr(),
            TimedOut = timeout.IsCancellationRequested,
        };
    }
}
