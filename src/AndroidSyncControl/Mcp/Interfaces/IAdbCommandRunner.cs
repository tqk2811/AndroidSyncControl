#nullable enable
using AndroidSyncControl.Mcp.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AndroidSyncControl.Mcp.Interfaces;

public interface IAdbCommandRunner
{
    Task<IReadOnlyList<AdbDeviceInfo>> ListDevicesAsync(CancellationToken cancellationToken);

    /// <param name="deviceId">Must come from <see cref="ListDevicesAsync"/>, it is passed to adb unquoted.</param>
    /// <param name="arguments">Command line that follows <c>adb -s deviceId</c>.</param>
    Task<AdbCommandResult> ExecuteAsync(string deviceId, string arguments, int timeoutMs, CancellationToken cancellationToken);
}
