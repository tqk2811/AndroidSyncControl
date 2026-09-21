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
using System.Windows;
using TqkLibrary.Scrcpy.Interfaces;
using TqkLibrary.Scrcpy.Enums;
using TqkLibrary.Scrcpy.Configs;
using TqkLibrary.AudioPlayer.XAudio2;

namespace AndroidSyncControl.UI.ViewModels
{
    class DeviceView : BaseViewModel, IDisposable
    {
        readonly Scrcpy scrcpy;
        readonly Adb adb;
        readonly ScrcpyUiView scrcpyUiView; // owned instance, disposed in Dispose (property may be null'd on detach)
        readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        readonly object _audioLock = new object();
        CancellationTokenSource _audioCts;
        Task _audioTask;
        bool isStop = false;
        bool _isDetached = false;
        public DeviceView(string DeviceId)
        {
            this.scrcpy = new Scrcpy(DeviceId);
            this.adb = new Adb(DeviceId);
            this.Control = scrcpy.Control;
            this.scrcpyUiView = scrcpy.InitScrcpyUiView();
            this.ScrcpyUiView = this.scrcpyUiView;
            this.scrcpy.OnDisconnect += Scrcpy_OnDisconnect;
            this.scrcpy.Control.OnClipboardReceived += Control_OnClipboardReceived;
        }

        /// <summary>
        /// Nhận clipboard mà thiết bị Android đẩy về khi <see cref="ScrcpyServerConfig.ClipboardAutosync"/> bật.
        /// Core (Scrcpy) chỉ lưu vào LastClipboard, KHÔNG tự ghi vào clipboard Windows nên phải ghi thủ công ở đây.
        /// Callback chạy trên thread native của scrcpy → marshal về UI thread vì Clipboard yêu cầu STA;
        /// bọc try/catch vì Clipboard.SetText có thể ném COMException khi clipboard đang bị tiến trình khác khoá.
        /// </summary>
        void Control_OnClipboardReceived(IControl control, string data)
        {
            if (isStop || string.IsNullOrEmpty(data))
                return;
            Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
            {
                try { Clipboard.SetText(data); }
                catch { /* clipboard dang bi khoa/ban, bo qua */ }
            }));
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
            StopAudio();
            scrcpy.Control.OnClipboardReceived -= Control_OnClipboardReceived;
            scrcpyUiView?.Dispose();
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
            // After DetachView() the WPF ScrcpyControl must not touch the (about-to-be) disposed scrcpy,
            // so the bound Control resolves to null just like the main panel does via `DeviceView = null`.
            get { return _isDetached ? null : (IsSync ? _Control : RawControl); }
            set { _Control = value; NotifyPropertyChange(); }
        }


        bool _IsSync = false;
        public bool IsSync
        {
            get { return _IsSync; }
            set { _IsSync = value; NotifyPropertyChange(); NotifyPropertyChange(nameof(Control)); }
        }

        // Checkbox (2): true = phát âm thanh thiết bị ra loa PC; false = đọc & bỏ dòng byte (drain) không phát.
        bool _IsSpeaker = true;
        public bool IsSpeaker
        {
            get { return _IsSpeaker; }
            set { _IsSpeaker = value; NotifyPropertyChange(); }
        }

        ScrcpyUiView _ScrcpyUiView;
        public ScrcpyUiView ScrcpyUiView
        {
            get { return _ScrcpyUiView; }
            private set { _ScrcpyUiView = value; NotifyPropertyChange(); }
        }
        public IControl RawControl { get { return scrcpy.Control; } }

        /// <summary>
        /// Ngắt tham chiếu mà WPF ScrcpyControl đang bind (ScrcpyUiView, Control) TRƯỚC khi scrcpy bị dispose.
        /// Handler CompositionTarget.Rendering của control đọc <c>ScrcpyUiView?.Scrcpy.ScreenSize</c> mỗi frame;
        /// nếu scrcpy đã dispose mà control còn giữ ScrcpyUiView thì GetScreenSize() ném ObjectDisposedException
        /// ngay trong render loop của WPF (không bắt được → crash).
        /// Phải gọi khi item CÒN đang được bind (trước khi Remove khỏi ItemsSource) để binding lan null xuống
        /// control một cách đồng bộ — giống cách panel main làm qua <c>mainWVM.DeviceView = null</c>.
        /// </summary>
        public void DetachView()
        {
            _isDetached = true;
            NotifyPropertyChange(nameof(Control));
            ScrcpyUiView = null;
        }

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

        private async void Scrcpy_OnDisconnect(ScrcpyDisconnectSource scrcpyDisconnectSource)
        {
            if (scrcpyDisconnectSource != ScrcpyDisconnectSource.Video)
                return;
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
                        if (stdout.Contains("stopped")) break;
                        else await Task.Delay(500, cancellationTokenSource.Token);
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
                MessageBox.Show(ex.Message + ex.StackTrace, ex.GetType().FullName);
            }
        }

        public Task<bool> Start()
        {
            return Task.Factory.StartNew(() =>
            {
#if DEBUG
                Debug.WriteLine($"scrcpy.Connect");
#endif
                if (scrcpy.Connect(new ScrcpyConfig()
                {
                    ClientConfig = new ClientConfig()
                    {
                        HwType = Singleton.Setting.Setting.UseGpu
                            ? FFmpegAVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA
                            : FFmpegAVHWDeviceType.AV_HWDEVICE_TYPE_NONE,
                        IsUseD3D11ForConvert = false,
                        IsUseD3D11ForUiRender = true,
                        Filter = D3D11Filter.D3D11_FILTER_MIN_MAG_LINEAR_MIP_POINT,
                    },
                    DeployConfig = new ScrcpyDeployConfig()
                    {
                        ScrcpyServerAndroidPath = "/sdcard/scrcpy-server-AndroidSyncControl-{ver}.jar",
                        ConnectionTimeout = Singleton.Setting.Setting.Timeout,
                    },
                    ServerConfig = new ScrcpyServerConfig()
                    {
                        AudioConfig = new AudioConfig()
                        {
                            IsAudio = Singleton.Setting.Setting.IsAudio,
                        },
                        IsControl = true,
                        AndroidConfig = new()
                        {
                            PowerOn = true,
                            PowerOffOnClose = false,
                            ShowTouches = true,
                            StayAwake = true,
                        },
                        VideoConfig = new()
                        {
                            MaxFps = Math.Max(1, Singleton.Setting.Setting.MaxFps),
                            CaptureOrientation = CaptureOrientations.Orient0,
                            CaptureOrientationLock = CaptureOrientationLock.LockedValue,
                        },
                        Cleanup = false,
                        VideoSource = VideoSource.Display,
                        LogLevel = LogLevel.Debug,
                        ClipboardAutosync = true,
                        MaxSize = Singleton.Setting.Setting.MaxSize,
                    },
                }))
                {
                    //this.ScrcpyUiView = scrcpy.InitScrcpyUiView();
                    isStop = false;
                    StartAudio();
                    return true;
                }
                else
                {
                    return false;
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void Stop()
        {
            isStop = true;
            StopAudio();
            scrcpy.Stop();
        }

        /// <summary>
        /// Bắt đầu luồng phát âm thanh khi checkbox (1) IsAudio đang bật.
        /// Gọi sau mỗi lần connect thành công (kể cả reconnect).
        /// </summary>
        void StartAudio()
        {
            if (!Singleton.Setting.Setting.IsAudio)
                return;
            lock (_audioLock)
            {
                StopAudio_NoLock();
                var cts = new CancellationTokenSource();
                _audioCts = cts;
                _audioTask = Task.Factory.StartNew(
                    () => AudioLoop(cts.Token),
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);
            }
        }

        void StopAudio()
        {
            lock (_audioLock)
                StopAudio_NoLock();
        }

        void StopAudio_NoLock()
        {
            if (_audioCts != null)
            {
                try { _audioCts.Cancel(); } catch { }
                _audioCts = null;
            }
            _audioTask = null;
        }

        /// <summary>
        /// Đọc PCM đã giải mã từ scrcpy và phát ra loa PC qua XAudio2.
        /// - IsSpeaker bật (mặc định): queue dữ liệu vào source voice (phát ra loa).
        /// - IsSpeaker tắt: vẫn đọc để tiêu thụ (drain) dòng byte, nhưng không phát.
        /// Thoát khi bị hủy hoặc scrcpy mất kết nối (Read trả 0).
        /// </summary>
        void AudioLoop(CancellationToken token)
        {
            const int channels = 2;
            const int sampleRate = 48000;
            const int bitsPerSample = 16;

            XAudio2Engine engine = null;
            XAudio2MasterVoice masterVoice = null;
            XAudio2SourceVoice sourceVoice = null;
            try
            {
                engine = new XAudio2Engine();
                masterVoice = engine.CreateMasterVoice(channels, sampleRate);
                sourceVoice = masterVoice.CreateSourceVoice(channels, sampleRate, bitsPerSample, WaveFormatTag.WAVE_FORMAT_PCM);
                sourceVoice.Start();

                ScrcpyAudioStream audioStream = scrcpy.GetAudioStream(AVSampleFormat.S16, sampleRate, channels);
                byte[] buffer = new byte[sampleRate * channels * (bitsPerSample / 8) / 10]; // ~100ms
                bool speakerOn = true;

                while (!token.IsCancellationRequested)
                {
                    int read = audioStream.Read(buffer, 0, buffer.Length);
                    if (read == 0)
                        break; // scrcpy mất kết nối

                    if (IsSpeaker)
                    {
                        if (!speakerOn)
                        {
                            sourceVoice.Start();
                            speakerOn = true;
                        }

                        byte[] frame = new byte[read];
                        Array.Copy(buffer, 0, frame, 0, read);

                        QueueResult queueResult;
                        do
                        {
                            queueResult = sourceVoice.QueueFrame(frame);
                            if (queueResult == QueueResult.QueueFull)
                            {
                                if (token.IsCancellationRequested || !IsSpeaker)
                                    break;
                                Thread.Sleep(10);
                            }
                        }
                        while (queueResult == QueueResult.QueueFull);

                        if (queueResult == QueueResult.Failed)
                            break;
                    }
                    else if (speakerOn)
                    {
                        // Vừa tắt loa: dừng và xóa buffer đang chờ để cắt tiếng ngay.
                        sourceVoice.Stop();
                        sourceVoice.FlushSourceBuffers();
                        speakerOn = false;
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"AudioLoop error: {ex}");
#endif
            }
            finally
            {
                try { sourceVoice?.Dispose(); } catch { }
                try { masterVoice?.Dispose(); } catch { }
                try { engine?.Dispose(); } catch { }
            }
        }
    }
}
