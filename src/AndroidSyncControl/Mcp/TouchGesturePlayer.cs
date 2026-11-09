#nullable enable
using AndroidSyncControl.Mcp.Interfaces;
using ModelContextProtocol;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using TqkLibrary.Scrcpy.Enums;

namespace AndroidSyncControl.Mcp;

/// <summary>Plays touch gestures, one at a time per device so their events never interleave.</summary>
public sealed class TouchGesturePlayer
{
    /// <summary>scrcpy's id for a touch that is not tied to a mouse.</summary>
    const long FingerPointerId = -2;
    const int StepDelayMs = 16;
    const int MaxDurationMs = 10000;

    readonly ConcurrentDictionary<string, SemaphoreSlim> _deviceLocks = new();

    public Task TapAsync(IDeviceSession session, Point point, int holdMs, CancellationToken cancellationToken)
        => SwipeAsync(session, point, point, holdMs, cancellationToken);

    public async Task SwipeAsync(IDeviceSession session, Point from, Point to, int durationMs, CancellationToken cancellationToken)
    {
        if (durationMs < 0 || durationMs > MaxDurationMs)
            throw new McpException($"The duration must be between 0 and {MaxDurationMs} ms.");

        SemaphoreSlim deviceLock = _deviceLocks.GetOrAdd(session.DeviceId, _ => new SemaphoreSlim(1, 1));
        await deviceLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Size screen = session.ScreenSize;
            Send(session, AndroidMotionEventAction.ACTION_DOWN, from, screen, 1f);
            Point liftPoint = from;
            try
            {
                // Position follows the clock: a Task.Delay step can last twice as long as asked.
                Stopwatch clock = Stopwatch.StartNew();
                while (clock.ElapsedMilliseconds < durationMs)
                {
                    await Task.Delay(StepDelayMs, cancellationToken).ConfigureAwait(false);
                    liftPoint = Interpolate(from, to, Math.Min(clock.ElapsedMilliseconds, durationMs), durationMs);
                    Send(session, AndroidMotionEventAction.ACTION_MOVE, liftPoint, screen, 1f);
                }
                if (liftPoint != to)
                {
                    liftPoint = to;
                    Send(session, AndroidMotionEventAction.ACTION_MOVE, to, screen, 1f);
                }
            }
            catch
            {
                // Lift the finger or the device keeps seeing a held touch, but report the first failure.
                TryLift(session, liftPoint, screen);
                throw;
            }
            Send(session, AndroidMotionEventAction.ACTION_UP, liftPoint, screen, 0f);
        }
        finally
        {
            deviceLock.Release();
        }
    }

    static void Send(IDeviceSession session, AndroidMotionEventAction action, Point point, Size screen, float pressure)
    {
        Rectangle position = new Rectangle(point, screen);
        if (!session.InputControl.InjectTouchEvent(action, FingerPointerId, position, pressure, AndroidMotionEventButton.None, AndroidMotionEventButton.None))
            throw new McpException($"Device '{session.DeviceId}' did not accept the touch event.");
    }

    static void TryLift(IDeviceSession session, Point point, Size screen)
    {
        try { Send(session, AndroidMotionEventAction.ACTION_UP, point, screen, 0f); }
        catch (Exception) { /* the session is already gone, nothing is left to release */ }
    }

    static Point Interpolate(Point from, Point to, long elapsed, long total)
        => new Point(
            from.X + (int)((to.X - from.X) * elapsed / total),
            from.Y + (int)((to.Y - from.Y) * elapsed / total));
}
