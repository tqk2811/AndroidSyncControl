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
            this.ScrcpyUiView = scrcpy.InitScrcpyUiView();
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
            ScrcpyUiView.Dispose();
            scrcpy.Dispose();
            cancellationTokenSource.Dispose();
        }




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
            get { return IsSync ? _Control : RawControl; }
            set { _Control = value; NotifyPropertyChange(); }
        }


        bool _IsSync = false;
        public bool IsSync
        {
            get { return _IsSync; }
            set { _IsSync = value; NotifyPropertyChange(); NotifyPropertyChange(nameof(Control)); }
        }

        public ScrcpyUiView ScrcpyUiView { get; }
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
            if (!isStop)
            {
                try
                {
                    await adb.WaitFor(WaitForType.Device).ExecuteAsync(cancellationTokenSource.Token, true);
                    if (!isStop) _ = Start();
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {

                }
            }
        }


        public Task Start()
        {
            return Task.Run(() =>
            {
                if (scrcpy.Connect(new ScrcpyConfig()
                {
                    ClipboardAutosync = false,
                    HwType = FFmpegAVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA,
                    IsUseD3D11Shader = true,
                    MaxFps = 24,
                    IsControl = true,
                    PowerOn = true,
                    StayAwake = true,
                    ShowTouches = true,
                    ConnectionTimeout = 3000,
                    Orientation = Orientations.Natural,
                }))
                {
                    isStop = false;
                }
                else
                {

                }
            });
        }

        public void Stop()
        {
            isStop = true;
            scrcpy.Stop();
        }
    }
}
