using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TqkLibrary.WpfUi;
using TqkLibrary.Scrcpy.Wpf;
using System.Collections.ObjectModel;
using System.Net;
using System.Security.Cryptography;

namespace AndroidSyncControl.UI.ViewModels
{
    class MainWVM : BaseViewModel
    {
        DeviceView _DeviceView = null;
        public DeviceView DeviceView
        {
            get { return _DeviceView; }
            set { _DeviceView = value; NotifyPropertyChange(); }
        }

        public double ViewPercent
        {
            get { return Singleton.Setting.Setting.ViewPercent; }
            set { Singleton.Setting.Setting.ViewPercent = value; NotifyPropertyChange(); Singleton.Setting.Save(); }
        }
        public int MaxFps
        {
            get { return Singleton.Setting.Setting.MaxFps; }
            set { Singleton.Setting.Setting.MaxFps = value; NotifyPropertyChange(); Singleton.Setting.Save(); }
        }
        public int MaxSize
        {
            get { return Singleton.Setting.Setting.MaxSize; }
            set
            {
                if(value < 240)
                {
                    Singleton.Setting.Setting.MaxSize = 0;
                }
                else
                {
                    Singleton.Setting.Setting.MaxSize = value / 8 * 8;
                }
                NotifyPropertyChange();
                Singleton.Setting.Save();
            }
        }
        public bool UseGpu
        {
            get { return Singleton.Setting.Setting.UseGpu; }
            set { Singleton.Setting.Setting.UseGpu = value; NotifyPropertyChange(); Singleton.Setting.Save(); }
        }
        public bool IsAudio
        {
            get { return Singleton.Setting.Setting.IsAudio; }
            set { Singleton.Setting.Setting.IsAudio = value; NotifyPropertyChange(); Singleton.Setting.Save(); }
        }

        const int McpTokenBytes = 32;

        /// <summary>Raised when the MCP server must be started, stopped or restarted.</summary>
        public event Action McpConfigurationChanged;

        public bool McpEnabled
        {
            get { return Singleton.Setting.Setting.McpEnabled; }
            set
            {
                Singleton.Setting.Setting.McpEnabled = value;
                NotifyPropertyChange();
                Singleton.Setting.Save();
                McpConfigurationChanged?.Invoke();
            }
        }

        /// <summary>A hand-edited setting that is not an IP falls back to loopback, the private choice.</summary>
        public IPAddress McpBindAddress
        {
            get { return IPAddress.TryParse(Singleton.Setting.Setting.McpHost, out IPAddress address) ? address : IPAddress.Loopback; }
        }

        public bool McpIsExposed
        {
            get { return !IPAddress.IsLoopback(McpBindAddress); }
        }

        public string McpHost
        {
            get { return McpBindAddress.ToString(); }
            set
            {
                // The ComboBox writes back null or the same value while it is being populated.
                if (string.IsNullOrEmpty(value) || value == McpHost)
                    return;
                Singleton.Setting.Setting.McpHost = value;
                NotifyPropertyChange();
                NotifyPropertyChange(nameof(McpIsExposed));
                Singleton.Setting.Save();
                if (McpEnabled)
                    McpConfigurationChanged?.Invoke();
            }
        }

        public int McpPort
        {
            get { return Singleton.Setting.Setting.McpPort; }
            set
            {
                Singleton.Setting.Setting.McpPort = value;
                NotifyPropertyChange();
                Singleton.Setting.Save();
                if (McpEnabled)
                    McpConfigurationChanged?.Invoke();
            }
        }

        public string McpToken
        {
            get { return Singleton.Setting.Setting.McpToken; }
        }

        /// <summary>Returns the access token, creating it the first time it is needed.</summary>
        public string EnsureMcpToken()
        {
            if (string.IsNullOrEmpty(McpToken))
                StoreNewMcpToken();
            return McpToken;
        }

        /// <summary>Replaces the token and restarts a running server, which then rejects the old token.</summary>
        public void RegenerateMcpToken()
        {
            StoreNewMcpToken();
            if (McpEnabled)
                McpConfigurationChanged?.Invoke();
        }

        void StoreNewMcpToken()
        {
            Singleton.Setting.Setting.McpToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(McpTokenBytes)).ToLowerInvariant();
            NotifyPropertyChange(nameof(McpToken));
            Singleton.Setting.Save();
        }

        string _McpStatus = string.Empty;
        /// <summary>Endpoint address while running, the error text after a failed start, otherwise empty.</summary>
        public string McpStatus
        {
            get { return _McpStatus; }
            set { _McpStatus = value; NotifyPropertyChange(); }
        }

        public ObservableCollection<DeviceView> DeviceViews { get; } = new ObservableCollection<DeviceView>();

        /// <summary>Adds a device to the list view and starts it; check IsConnected for the outcome.</summary>
        public async Task<DeviceView> ShowInListAsync(string deviceId)
        {
            // The device can be opened by an MCP request between the click and this call.
            DeviceView existing = DeviceView?.DeviceId == deviceId ? DeviceView : DeviceViews.FirstOrDefault(x => x.DeviceId == deviceId);
            if (existing != null)
                return existing;

            DeviceView deviceView = new DeviceView(deviceId);
            DeviceViews.Add(deviceView);
            await deviceView.Start();
            deviceView.SliderChange(ViewPercent);
            return deviceView;
        }

        public void RemoveFromList(DeviceView deviceView)
        {
            using (deviceView)
            {
                // Null the bound ScrcpyUiView/Control BEFORE removing from the collection so the
                // binding propagates null to the ScrcpyControl synchronously (same as the main panel).
                // Otherwise Remove() freezes the binding, the control keeps the ScrcpyUiView, and the
                // next CompositionTarget.Rendering tick calls GetScreenSize() on the disposed scrcpy → crash.
                deviceView.DetachView();
                DeviceViews.Remove(deviceView);
                deviceView.Stop();
            }
        }


        public ObservableCollection<ComboboxVM> DeviceNameList { get; } = new ObservableCollection<ComboboxVM>();

        ComboboxVM _DeviceNameListSelected = null;
        public ComboboxVM DeviceNameListSelected
        {
            get { return _DeviceNameListSelected; }
            set { _DeviceNameListSelected = value; NotifyPropertyChange(); }
        }
    }
}
