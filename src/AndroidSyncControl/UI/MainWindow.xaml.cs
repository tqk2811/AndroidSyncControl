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
using AndroidSyncControl.UI.Helpers;
using AndroidSyncControl.Themes;
using AndroidSyncControl.Themes.Enums;
using AndroidSyncControl.Localization;
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
            // Attach the WM_GETMINMAXINFO hook first, then maximize once it is active so
            // the initial maximized bounds are constrained to the work area (no overflow).
            WindowMaximizeHelper.Enable(this);
            this.SourceInitialized += (s, e) => this.WindowState = WindowState.Maximized;
            this.Loaded += MainWindow_Loaded;
            this.Closed += MainWindow_Closed;

            timer = new Timer();
            timer.AutoReset = false;
            timer.Interval = 500;
            timer.Elapsed += Timer_Elapsed;
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            LanguageManager.LanguageChanged -= OnLanguageChanged;
            mainWVM.DeviceView?.DetachView();
            using (var v = mainWVM.DeviceView) v?.Stop();
            foreach (var item in mainWVM.DeviceViews)
            {
                item.DetachView();
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

            // Bind the title-bar language dropdown to the current selection. Setting
            // SelectedItem raises SelectionChanged, but Set() is a no-op for the active
            // language so no re-entrancy occurs.
            cbo_language.ItemsSource = LanguageManager.Items;
            cbo_language.SelectedItem = LanguageManager.Items.FirstOrDefault(x => x.Mode == LanguageManager.CurrentMode);
            LanguageManager.LanguageChanged += OnLanguageChanged;

            UpdateThemeGlyph();
            UpdateMaximizeGlyph();
        }

        // Refresh the strings that are set from code-behind (theme tooltip). Every other
        // string is a DynamicResource and updates automatically when the table is swapped.
        private void OnLanguageChanged() => UpdateThemeGlyph();

        private void cbo_language_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbo_language.SelectedItem is LanguageItem item)
                LanguageManager.Set(item.Mode);
        }

        #region Title bar / window chrome

        // Window dragging, drag-from-maximized-to-restore and double-click-to-maximize
        // are handled natively by WindowChrome.CaptionHeight (see MainWindow.xaml).

        private void btn_min_Click(object sender, RoutedEventArgs e)
            => this.WindowState = WindowState.Minimized;

        private void btn_max_Click(object sender, RoutedEventArgs e)
            => ToggleMaximize();

        private void btn_close_Click(object sender, RoutedEventArgs e)
            => this.Close();

        private void ToggleMaximize()
            => this.WindowState = this.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;

        private void Window_StateChanged(object sender, EventArgs e)
            => UpdateMaximizeGlyph();

        private void UpdateMaximizeGlyph()
        {
            // E922 = Maximize, E923 = Restore (Segoe MDL2 Assets)
            if (maxGlyph != null)
                maxGlyph.Text = this.WindowState == WindowState.Maximized ? "" : "";
        }

        private void btn_theme_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Cycle(); // System -> Light -> Dark -> System
            UpdateThemeGlyph();
        }

        private void UpdateThemeGlyph()
        {
            if (btn_theme == null || themeGlyph == null)
                return;

            // Segoe MDL2 Assets: E713 Settings (System), E706 Brightness (Light), E708 QuietHours (Dark).
            switch (ThemeManager.CurrentMode)
            {
                case ThemeMode.Light:
                    themeGlyph.Text = "";
                    btn_theme.ToolTip = LanguageManager.GetString("Str.Theme.Light");
                    break;
                case ThemeMode.Dark:
                    themeGlyph.Text = "";
                    btn_theme.ToolTip = LanguageManager.GetString("Str.Theme.Dark");
                    break;
                default:
                    themeGlyph.Text = "";
                    btn_theme.ToolTip = LanguageManager.GetString("Str.Theme.System");
                    break;
            }
        }

        private void btn_about_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                LanguageManager.GetString("Str.About.Body"),
                LanguageManager.GetString("Str.About.Title"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        #endregion Title bar / window chrome

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
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ex.StackTrace, ex.GetType().FullName);
            }
        }

        private async void btn_showListView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (mainWVM.DeviceNameListSelected != null)
                {
                    string deviceid = mainWVM.DeviceNameListSelected.Name;
                    DeviceView deviceView = new DeviceView(deviceid);
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
                    // Null the bound ScrcpyUiView/Control BEFORE removing from the collection so the
                    // binding propagates null to the ScrcpyControl synchronously (same as the main panel).
                    // Otherwise Remove() freezes the binding, the control keeps the ScrcpyUiView, and the
                    // next CompositionTarget.Rendering tick calls GetScreenSize() on the disposed scrcpy → crash.
                    deviceView.DetachView();
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
