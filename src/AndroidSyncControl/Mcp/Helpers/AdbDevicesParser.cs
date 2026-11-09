#nullable enable
using AndroidSyncControl.Mcp.Models;
using System;
using System.Collections.Generic;

namespace AndroidSyncControl.Mcp.Helpers;

public static class AdbDevicesParser
{
    /// <summary>
    /// Parses <c>adb devices</c>. States stay plain text because adb has more of them than the
    /// library's enum, whose parser throws on values such as "connecting" for the whole list.
    /// </summary>
    public static IReadOnlyList<AdbDeviceInfo> Parse(string output)
    {
        List<AdbDeviceInfo> devices = new List<AdbDeviceInfo>();
        foreach (string line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            // Device lines are "serial<TAB>state"; a serial may contain spaces.
            int separator = line.IndexOf('\t');
            if (separator <= 0)
                continue;
            devices.Add(new AdbDeviceInfo
            {
                DeviceId = line.Substring(0, separator),
                State = line.Substring(separator + 1).Trim(),
            });
        }
        return devices;
    }
}
