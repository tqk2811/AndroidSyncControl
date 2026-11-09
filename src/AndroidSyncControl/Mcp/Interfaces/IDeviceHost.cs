#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AndroidSyncControl.Mcp.Interfaces;

/// <summary>Owner of the device sessions. MCP tools open and close sessions only through it.</summary>
public interface IDeviceHost
{
    Task<IReadOnlyList<IDeviceSession>> GetSessionsAsync(CancellationToken cancellationToken);

    Task<IDeviceSession?> FindAsync(string deviceId, CancellationToken cancellationToken);

    /// <summary>Opens a session, or returns the existing one when the device is already open.</summary>
    Task<IDeviceSession> ConnectAsync(string deviceId, CancellationToken cancellationToken);

    /// <returns>false when the device has no session.</returns>
    Task<bool> DisconnectAsync(string deviceId, CancellationToken cancellationToken);
}
