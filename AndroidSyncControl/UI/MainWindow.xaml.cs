using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Timers;
using TqkLibrary.AdbDotNet;
using AndroidSyncControl.UI.ViewModels;
using TqkLibrary.Scrcpy;

namespace AndroidSyncControl.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly MainWVM mainWVM;
        readonly Timer timer;
        public MainWindow()
        {
            InitializeComponent();
            this.mainWVM = this.DataContext as MainWVM;
            this.Loaded += MainWindow_Loaded;
            this.Closed += MainWindow_Closed;

            timer = new Timer();
            timer.AutoReset = false;
            timer.Interval = 500;
            timer.Elapsed += Timer_Elapsed;
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            using (var v = mainWVM.DeviceView) v?.Stop();
            foreach (var item in mainWVM.DeviceViews)
            {
                using (var v = item) v?.Stop();
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            parentMainGrid.Width = 0;
            mainGrid.Width = 0;
            mainWVM.DeviceViews.CollectionChanged += DeviceViews_CollectionChanged;
            timer.Start();
            mainGrid.Width = 0;
        }

        private void DeviceViews_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            mainWVM.DeviceView?.SetControlChain(mainWVM.DeviceViews.Select(x => x.RawControl));
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                var devices = (await Adb.DevicesAsync()).Where(x => x.DeviceState == DeviceState.Device).Select(x => x.DeviceId);

                var avalable_devices = mainWVM.DeviceViews.Select(x => x?.DeviceId).ToList();
                avalable_devices.Add(mainWVM?.DeviceView?.DeviceId);
                avalable_devices = avalable_devices.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

                var new_devices = devices.Except(avalable_devices).ToList();

                var current_show_devices = mainWVM.DeviceNameList.Select(x => x.Name);
                var need_show_devices = new_devices.Except(current_show_devices).ToList();
                var not_show_devices = current_show_devices.Intersect(avalable_devices).ToList();

                await this.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var device in need_show_devices)
                    {
                        mainWVM.DeviceNameList.Add(new ComboboxVM(device));
                    }
                    foreach (var device in not_show_devices)
                    {
                        var vm = mainWVM.DeviceNameList.FirstOrDefault(x => device.Equals(x.Name));
                        mainWVM.DeviceNameList.Remove(vm);
                    }
                });
            }
            catch
            {

            }
            finally
            {
                timer.Start();
            }
        }

        private async void btn_showMain_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (mainWVM.DeviceNameListSelected != null)
                {
                    string deviceid = mainWVM.DeviceNameListSelected.Name;
                    if (mainWVM.DeviceView != null)
                    {
                        btn_mainRemove_Click(null, null);
                    }

                    DeviceView deviceView = new DeviceView(deviceid);
                    mainWVM.DeviceView = deviceView;
                    await deviceView.Start();
                    parentMainGrid.Width = deviceView.MainView(mainGrid.ActualHeight);
                    mainGrid.Width = parentMainGrid.Width;
                    deviceView.SetControlChain(mainWVM.DeviceViews.Select(x => x.RawControl));
                }
            }
            catch
            {

            }
        }

        private async void btn_showListView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (mainWVM.DeviceNameListSelected != null)
                {
                    string deviceid = mainWVM.DeviceNameListSelected.Name;
#if DEBUG
                    for (int i = 0; i < 3; i++)
                    {
                        DeviceView deviceView = new DeviceView(deviceid);
                        deviceView.OnConencted += DeviceView_OnConencted;
                        mainWVM.DeviceViews.Add(deviceView);
                        await deviceView.Start();
                        deviceView.SliderChange(mainWVM.ViewPercent);
                    }
#else
                    DeviceView deviceView = new DeviceView(deviceid);
                    mainWVM.DeviceViews.Add(deviceView);
                    await deviceView.Start();
                    deviceView.SliderChange(mainWVM.ViewPercent);
#endif
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ex.StackTrace, ex.GetType().FullName);
            }
        }

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mainWVM != null)
            {
                foreach (var item in mainWVM.DeviceViews)
                {
                    item.SliderChange(mainWVM.ViewPercent);
                }
            }
        }

        private void stackPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (mainWVM?.DeviceView != null)
            {
                parentMainGrid.Width = mainWVM.DeviceView.MainView(mainGrid.ActualHeight);
                mainGrid.Width = parentMainGrid.Width;
            }
        }

        private void btn_Remove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button button = sender as Button;
                using (DeviceView deviceView = button.DataContext as DeviceView)
                {
                    mainWVM.DeviceViews.Remove(deviceView);
                    deviceView.Stop();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ex.StackTrace, ex.GetType().FullName);
            }
        }

        private void btn_mainRemove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (mainWVM?.DeviceView != null)
                    using (var view = mainWVM.DeviceView)
                    {
                        mainWVM.DeviceView = null;
                        view.Stop();
                        parentMainGrid.Width = 0;
                        mainGrid.Width = 0;
                    }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ex.StackTrace, ex.GetType().FullName);
            }
        }

        private async void btn_showAllListView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var item in mainWVM.DeviceNameList.ToList())
                {
                    DeviceView deviceView = new DeviceView(item.Name);
                    mainWVM.DeviceViews.Add(deviceView);
                    await deviceView.Start();
                    deviceView.SliderChange(mainWVM.ViewPercent);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ex.StackTrace, ex.GetType().FullName);
            }
        }
    }
}
