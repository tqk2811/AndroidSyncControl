namespace AndroidSyncControl.Localization.Enums
{
    /// <summary>
    /// User-selectable UI language. On first run (no persisted value) the language
    /// follows the Windows display language via <see cref="LanguageManager.DetectSystem"/>.
    /// </summary>
    internal enum LanguageMode
    {
        English,
        Vietnamese,
    }
}
