#nullable enable
using System.Net;

namespace AndroidSyncControl.Mcp.Models;

public sealed class McpHostOptions
{
    /// <summary>Address to listen on. Anything but loopback exposes the server to the network.</summary>
    public IPAddress BindAddress { get; init; } = IPAddress.Loopback;

    /// <summary>Port to listen on, 0 lets the OS choose.</summary>
    public required int Port { get; init; }

    public required string AccessToken { get; init; }
}
