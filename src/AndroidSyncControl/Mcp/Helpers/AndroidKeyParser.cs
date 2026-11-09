#nullable enable
using ModelContextProtocol;
using System;
using TqkLibrary.Scrcpy.Enums;

namespace AndroidSyncControl.Mcp.Helpers;

public static class AndroidKeyParser
{
    const string Prefix = "AKEYCODE_";

    /// <param name="name">Key name with or without the AKEYCODE_ prefix, in any case.</param>
    public static AndroidKeyCode Parse(string name)
    {
        string trimmed = (name ?? string.Empty).Trim();
        string fullName = trimmed.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) ? trimmed : Prefix + trimmed;
        // IsDefined is on the parsed name: Enum.TryParse alone also accepts arbitrary numbers.
        if (Enum.TryParse(fullName, ignoreCase: true, out AndroidKeyCode key) && Enum.IsDefined(key))
            return key;
        throw new McpException($"Unknown Android key '{name}'. Use a name such as HOME, BACK or ENTER.");
    }
}
