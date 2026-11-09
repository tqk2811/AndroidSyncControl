#nullable enable
using System.Drawing;
using TqkLibrary.Scrcpy.Interfaces;

namespace AndroidSyncControl.Mcp.Interfaces;

/// <summary>A device with an open scrcpy session.</summary>
public interface IDeviceSession
{
    string DeviceId { get; }
    bool IsConnected { get; }

    /// <summary>Size of the current video frame. Touch coordinates are relative to it.</summary>
    Size ScreenSize { get; }

    /// <summary>Input channel of this device only, it never follows the Sync chain of the UI.</summary>
    IControl InputControl { get; }

    /// <summary>Returns a new bitmap owned by the caller, or null when the session cannot capture.</summary>
    Bitmap? CaptureScreen();
}
