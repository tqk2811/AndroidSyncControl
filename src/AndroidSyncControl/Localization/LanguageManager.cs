using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using AndroidSyncControl.Localization.Enums;

namespace AndroidSyncControl.Localization
{
    /// <summary>
    /// Resolves and applies the active UI language at runtime and persists the chosen
    /// <see cref="LanguageMode"/>. Works exactly like the theme system: only a single
    /// string <see cref="ResourceDictionary"/> is swapped in the app's merged dictionaries;
    /// XAML references every string via DynamicResource so they update live, and
    /// code-behind reads the current value through <see cref="GetString"/>.
    /// </summary>
    internal static class LanguageManager
    {
        const string EnglishUri = "pack://application:,,,/Localization/Strings.en.xaml";
        const string VietnameseUri = "pack://application:,,,/Localization/Strings.vi.xaml";

        // Key that only exists inside the string dictionaries (not in Controls.xaml or
        // the color palettes), used to locate the swappable string dictionary.
        const string StringMarkerKey = "Str.Btn.About";

        public static LanguageMode CurrentMode { get; private set; } = LanguageMode.English;

        /// <summary>
        /// Raised after the language has been applied so code-behind strings
        /// (theme tooltip, dialogs) can be refreshed.
        /// </summary>
        public static event Action LanguageChanged;

        /// <summary>The languages offered in the title-bar dropdown (native names).</summary>
        public static IReadOnlyList<LanguageItem> Items { get; } = new[]
        {
            new LanguageItem(LanguageMode.English, "English"),
            new LanguageItem(LanguageMode.Vietnamese, "Tiếng Việt"),
        };

        /// <summary>
        /// Parses a persisted value; falls back to the Windows display language when
        /// empty or unrecognized (first run).
        /// </summary>
        public static LanguageMode Parse(string value)
            => Enum.TryParse(value, ignoreCase: true, out LanguageMode mode) ? mode : DetectSystem();

        /// <summary>Vietnamese when the OS display language is Vietnamese, otherwise English.</summary>
        public static LanguageMode DetectSystem()
            => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
                .Equals("vi", StringComparison.OrdinalIgnoreCase)
                ? LanguageMode.Vietnamese
                : LanguageMode.English;

        /// <summary>Applies a language to the running application (without persisting it).</summary>
        public static void Apply(LanguageMode mode)
        {
            CurrentMode = mode;
            SwapDictionary(mode);
            LanguageChanged?.Invoke();
        }

        /// <summary>Applies and persists a language; no-op when it is already active.</summary>
        public static void Set(LanguageMode mode)
        {
            if (mode == CurrentMode)
                return;
            Apply(mode);
            Singleton.Setting.Setting.Language = mode.ToString();
            Singleton.Setting.Save();
        }

        /// <summary>Looks up a localized string by key from the active dictionary.</summary>
        public static string GetString(string key)
            => Application.Current?.TryFindResource(key) as string ?? key;

        static void SwapDictionary(LanguageMode mode)
        {
            var app = Application.Current;
            if (app == null)
                return;

            var newDict = new ResourceDictionary
            {
                Source = new Uri(mode == LanguageMode.Vietnamese ? VietnameseUri : EnglishUri, UriKind.Absolute)
            };

            var dicts = app.Resources.MergedDictionaries;
            var existing = dicts.FirstOrDefault(d => d.Contains(StringMarkerKey));
            if (existing != null)
                dicts[dicts.IndexOf(existing)] = newDict;
            else
                dicts.Add(newDict);
        }
    }
}
