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

        public ObservableCollection<DeviceView> DeviceViews { get; } = new ObservableCollection<DeviceView>();


        public ObservableCollection<ComboboxVM> DeviceNameList { get; } = new ObservableCollection<ComboboxVM>();

        ComboboxVM _DeviceNameListSelected = null;
        public ComboboxVM DeviceNameListSelected
        {
            get { return _DeviceNameListSelected; }
            set { _DeviceNameListSelected = value; NotifyPropertyChange(); }
        }
    }
}
