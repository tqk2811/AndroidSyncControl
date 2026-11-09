#nullable enable

namespace AndroidSyncControl.Mcp.Models;

public sealed class AdbCommandResult
{
    public required int ExitCode { get; init; }
    public required string Stdout { get; init; }
    public required string Stderr { get; init; }
    public required bool TimedOut { get; init; }
}
