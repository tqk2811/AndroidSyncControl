using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TqkLibrary.WpfUi;
using TqkLibrary.Scrcpy.Wpf;
using System.Collections.ObjectModel;

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
