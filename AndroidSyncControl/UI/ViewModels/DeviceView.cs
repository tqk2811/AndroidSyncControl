using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TqkLibrary.WpfUi;
using TqkLibrary.Scrcpy;
using TqkLibrary.Scrcpy.Wpf;
using TqkLibrary.AdbDotNet;
using System.Threading;
using System.Diagnostics;

namespace AndroidSyncControl.UI.ViewModels
{
    class DeviceView : BaseViewModel, IDisposable
    {
        readonly Scrcpy scrcpy;
        readonly Adb adb;
        readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        bool isStop = false;
        public DeviceView(string DeviceId)
        {
            this.scrcpy = new Scrcpy(DeviceId);
            this.adb = new Adb(DeviceId);
            this.Control = scrcpy.Control;
            this.scrcpy.OnDisconnect += Scrcpy_OnDisconnect;
        }

        ~DeviceView()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        void Dispose(bool disposing)
        {
            isStop = true;
            cancellationTokenSource.Cancel();
            ScrcpyUiView?.Dispose();
            scrcpy.Dispose();
            cancellationTokenSource.Dispose();
        }


        bool isConnecting = false;

        public string DeviceId { get { return scrcpy.DeviceId; } }


        bool _IsControl = true;
        public bool IsControl
        {
            get { return _IsControl; }
            set { _IsControl = value; NotifyPropertyChange(); }
        }

        IControl _Control;
        public IControl Control
        {
            get { return isConnecting ? null : (IsSync ? _Control : RawControl); }
            set { _Control = value; NotifyPropertyChange(); }
        }


        bool _IsSync = false;
        public bool IsSync
        {
            get { return _IsSync; }
            set { _IsSync = value; NotifyPropertyChange(); NotifyPropertyChange(nameof(Control)); }
        }

        ScrcpyUiView _ScrcpyUiView;
        public ScrcpyUiView ScrcpyUiView
        {
            get { return _ScrcpyUiView; }
            set { _ScrcpyUiView = value; NotifyPropertyChange(); }
        }
        public IControl RawControl { get { return scrcpy.Control; } }

        double _Width = 250;
        double _Height = 500;
        public double Width
        {
            get { return _Width; }
            set { _Width = value; NotifyPropertyChange(); }
        }
        public double Height
        {
            get { return _Height; }
            set { _Height = value; NotifyPropertyChange(); }
        }


        public void SliderChange(double ViewPercent)
        {
            var size = scrcpy?.ScreenSize;
            if (size != null && !double.IsNaN(ViewPercent))
            {
                Width = ViewPercent / 100 * size.Value.Width;
                Height = ViewPercent / 100 * size.Value.Height;
            }
        }

        public double MainView(double height)
        {
            var size = scrcpy?.ScreenSize;
            if (size != null && !double.IsNaN(height) && Height != height)
            {
                Height = height;
                Width = (height / size.Value.Height) * size.Value.Width;
            }
            return Width;
        }

        public void SetControlChain(IEnumerable<IControl> controls)
        {
            ControlChain controlChains = new ControlChain(scrcpy.Control, controls);
            Control = controlChains;
        }

        private async void Scrcpy_OnDisconnect()
        {
            try
            {
                while (!isStop)
                {
                    await adb.WaitFor(WaitForType.Device).ExecuteAsync(cancellationTokenSource.Token, true);
#if DEBUG
                    Debug.WriteLine("adb wait-for-device success");
#endif
                    while (true)
                    {
                        var r = await adb.Shell.BuildShellCommand("getprop init.svc.bootanim").ExecuteAsync(cancellationTokenSource.Token, true);
                        string stdout = r.Stdout();
#if DEBUG
                        Debug.WriteLine($"getprop init.svc.bootanim: {stdout}");
#endif
                        if (stdout.StartsWith("stopped")) break;
                        else await Task.Delay(200, cancellationTokenSource.Token);
                    }
                    if (await Start())
                    {
                        break;
                    }
                    else
                    {
                        await Task.Delay(1000, cancellationTokenSource.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {

            }
        }

        static readonly object _lock = new object();
        public Task<bool> Start()
        {
            return Task.Factory.StartNew(() =>
            {
#if DEBUG
                Debug.WriteLine($"scrcpy.Connect");
#endif
                //lock (_lock)
                {
                    try
                    {
                        isConnecting = true;
                        NotifyPropertyChange(nameof(Control));

                        using (var temp = ScrcpyUiView) ScrcpyUiView = null;

                        if (scrcpy.Connect(new ScrcpyConfig()
                        {
                            ClipboardAutosync = false,
                            HwType = FFmpegAVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA,
                            IsUseD3D11Shader = true,
                            MaxFps = Singleton.Setting.Setting.MaxFps,
                            IsControl = true,
                            PowerOn = true,
                            StayAwake = true,
                            ShowTouches = true,
                            ConnectionTimeout = 3000,
                            Orientation = Orientations.Natural,
                        }))
                        {
                            this.ScrcpyUiView = scrcpy.InitScrcpyUiView();
                            isStop = false;
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    finally
                    {
                        isConnecting = false;
                        NotifyPropertyChange(nameof(Control));
                    }
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void Stop()
        {
            isStop = true;
            scrcpy.Stop();
        }
    }
}
