namespace AndroidSyncControl.Themes.Enums
{
    /// <summary>
    /// User-selectable theme mode. <see cref="System"/> follows the OS light/dark
    /// setting and is the default; <see cref="Dark"/> / <see cref="Light"/> force a palette.
    /// </summary>
    internal enum ThemeMode
    {
        System,
        Light,
        Dark,
    }
}
