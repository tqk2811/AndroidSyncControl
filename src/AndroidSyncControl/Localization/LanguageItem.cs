using AndroidSyncControl.Localization.Enums;

namespace AndroidSyncControl.Localization
{
    /// <summary>
    /// One entry of the title-bar language dropdown. <see cref="Display"/> is the
    /// language's native name and is intentionally the same in every UI language
    /// (a user always recognizes their own language by its native name).
    /// </summary>
    internal class LanguageItem
    {
        public LanguageItem(LanguageMode mode, string display)
        {
            Mode = mode;
            Display = display;
        }

        public LanguageMode Mode { get; }
        public string Display { get; }
    }
}
