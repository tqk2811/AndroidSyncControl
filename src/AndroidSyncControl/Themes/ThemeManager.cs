using System;
using System.Linq;
using System.Windows;
using AndroidSyncControl.Themes.Enums;
using Microsoft.Win32;

namespace AndroidSyncControl.Themes
{
    /// <summary>
    /// Resolves and applies the active color palette (Dark/Light) at runtime and
    /// persists the chosen <see cref="ThemeMode"/>. In <see cref="ThemeMode.System"/>
    /// the palette follows the OS light/dark setting and updates live when it changes.
    /// Only the color <see cref="ResourceDictionary"/> is swapped; control styles in
    /// Controls.xaml reference every brush via DynamicResource so they update live.
    /// </summary>
    internal static class ThemeManager
    {
        const string DarkUri = "pack://application:,,,/Themes/Colors.Dark.xaml";
        const string LightUri = "pack://application:,,,/Themes/Colors.Light.xaml";

        // Key that only exists inside the color dictionaries (not in Controls.xaml),
        // used to locate the swappable palette dictionary in the merged collection.
        const string PaletteMarkerKey = "Brush.Window.Background";

        const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

        static bool _systemEventsHooked;

        public static ThemeMode CurrentMode { get; private set; } = ThemeMode.System;

        public static ThemeMode Parse(string value)
            => Enum.TryParse(value, ignoreCase: true, out ThemeMode mode) ? mode : ThemeMode.System;

        /// <summary>
        /// Applies a theme mode to the running application (without persisting it).
        /// </summary>
        public static void Apply(ThemeMode mode)
        {
            CurrentMode = mode;
            ApplyResolvedPalette();
            HookSystemEvents(mode == ThemeMode.System);
        }

        /// <summary>
        /// Advances System -> Light -> Dark -> System, applies and persists it.
        /// </summary>
        public static ThemeMode Cycle()
        {
            ThemeMode next = CurrentMode switch
            {
                ThemeMode.System => ThemeMode.Light,
                ThemeMode.Light => ThemeMode.Dark,
                _ => ThemeMode.System,
            };
            Apply(next);
            Singleton.Setting.Setting.Theme = next.ToString();
            Singleton.Setting.Save();
            return next;
        }

        static void ApplyResolvedPalette()
        {
            bool light = CurrentMode switch
            {
                ThemeMode.Light => true,
                ThemeMode.Dark => false,
                _ => IsSystemLight(),
            };
            SwapPalette(light);
        }

        static void SwapPalette(bool light)
        {
            var app = Application.Current;
            if (app == null)
                return;

            var newDict = new ResourceDictionary
            {
                Source = new Uri(light ? LightUri : DarkUri, UriKind.Absolute)
            };

            var dicts = app.Resources.MergedDictionaries;
            var existing = dicts.FirstOrDefault(d => d.Contains(PaletteMarkerKey));
            if (existing != null)
                dicts[dicts.IndexOf(existing)] = newDict;
            else
                dicts.Insert(0, newDict);
        }

        /// <summary>Reads the OS "apps use light theme" preference (defaults to light).</summary>
        static bool IsSystemLight()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
                if (key?.GetValue("AppsUseLightTheme") is int v)
                    return v != 0;
            }
            catch
            {
                // Registry unavailable -> fall back to light.
            }
            return true;
        }

        static void HookSystemEvents(bool enable)
        {
            if (enable && !_systemEventsHooked)
            {
                SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
                _systemEventsHooked = true;
            }
            else if (!enable && _systemEventsHooked)
            {
                SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
                _systemEventsHooked = false;
            }
        }

        static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category != UserPreferenceCategory.General || CurrentMode != ThemeMode.System)
                return;

            // Event may arrive on a non-UI thread; marshal to the dispatcher.
            Application.Current?.Dispatcher.Invoke(ApplyResolvedPalette);
        }
    }
}
