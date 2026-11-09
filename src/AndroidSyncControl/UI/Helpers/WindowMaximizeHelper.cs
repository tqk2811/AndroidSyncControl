using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace AndroidSyncControl.UI.Helpers
{
    /// <summary>
    /// A borderless (WindowChrome) window overflows the screen edges by the resize
    /// border when maximized, pushing content (incl. the caption buttons) off-screen.
    /// This compensates by insetting the window content with a margin equal to the
    /// measured overflow (window rect vs. monitor work area), converted to DIUs so it
    /// is correct at any DPI and on any monitor.
    /// </summary>
    internal static class WindowMaximizeHelper
    {
        public static void Enable(Window window)
        {
            window.StateChanged += (s, e) => Defer(window);
            window.Loaded += (s, e) => Defer(window);
        }

        static void Defer(Window window)
            => window.Dispatcher.BeginInvoke(new Action(() => Apply(window)), DispatcherPriority.Loaded);

        static void Apply(Window window)
        {
            if (window.Content is not FrameworkElement root)
                return;

            if (window.WindowState != WindowState.Maximized)
            {
                root.Margin = new Thickness(0);
                return;
            }

            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero || !GetWindowRect(handle, out RECT win))
                return;

            IntPtr monitor = MonitorFromWindow(handle, MONITOR_DEFAULTTONEAREST);
            var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref mi))
                return;

            RECT work = mi.rcWork;
            DpiScale dpi = VisualTreeHelper.GetDpi(window);

            double left = Math.Max(0, work.left - win.left) / dpi.DpiScaleX;
            double top = Math.Max(0, work.top - win.top) / dpi.DpiScaleY;
            double right = Math.Max(0, win.right - work.right) / dpi.DpiScaleX;
            double bottom = Math.Max(0, win.bottom - work.bottom) / dpi.DpiScaleY;

            root.Margin = new Thickness(left, top, right, bottom);
        }

        const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [StructLayout(LayoutKind.Sequential)]
        struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }
    }
}
