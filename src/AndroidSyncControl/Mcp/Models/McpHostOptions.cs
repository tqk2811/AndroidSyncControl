#nullable enable

namespace AndroidSyncControl.Mcp.Models;

public sealed class McpHostOptions
{
    /// <summary>Loopback port to listen on, 0 lets the OS choose.</summary>
    public required int Port { get; init; }

    public required string AccessToken { get; init; }
}
