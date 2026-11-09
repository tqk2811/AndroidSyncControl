#nullable enable
using AndroidSyncControl.Mcp.Interfaces;
using ModelContextProtocol;
using System.Threading;
using System.Threading.Tasks;

namespace AndroidSyncControl.Mcp.Extensions;

public static class DeviceHostExtensions
{
    /// <exception cref="McpException">The device has no live session.</exception>
    public static async Task<IDeviceSession> GetConnectedAsync(this IDeviceHost host, string deviceId, CancellationToken cancellationToken)
    {
        IDeviceSession? session = await host.FindAsync(deviceId, cancellationToken).ConfigureAwait(false);
        if (session is null)
            throw new McpException($"Device '{deviceId}' is not connected, call device_connect first.");
        if (!session.IsConnected)
            throw new McpException($"Device '{deviceId}' is open but its scrcpy session is not live yet, retry shortly.");
        return session;
    }
}
