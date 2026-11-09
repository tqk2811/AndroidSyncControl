#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AndroidSyncControl.Mcp.Helpers;

/// <summary>Builds a command line that CommandLineToArgvW splits back into the same arguments.</summary>
public static class WindowsCommandLine
{
    public static string Join(IEnumerable<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        return string.Join(" ", arguments.Select(Quote));
    }

    static string Quote(string argument)
    {
        if (argument.Length > 0 && !argument.Any(c => char.IsWhiteSpace(c) || c == '"'))
            return argument;

        StringBuilder result = new StringBuilder("\"");
        int backslashes = 0;
        foreach (char c in argument)
        {
            if (c == '\\')
            {
                backslashes++;
                continue;
            }
            // Backslashes are literal unless they precede a quote.
            result.Append('\\', c == '"' ? backslashes * 2 + 1 : backslashes);
            result.Append(c);
            backslashes = 0;
        }
        // The closing quote added below must not be escaped by a trailing backslash.
        result.Append('\\', backslashes * 2);
        result.Append('"');
        return result.ToString();
    }
}
