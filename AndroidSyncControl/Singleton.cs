using AndroidSyncControl.DataClass;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TqkLibrary.WpfUi;

namespace AndroidSyncControl
{
    internal static class Singleton
    {
        internal static string ExeDir { get; } = Directory.GetCurrentDirectory();
        internal static string AppDataDir { get; } = ExeDir + "\\AppData";
        internal static string AdbDir { get; } = AppDataDir + "\\Adb";



        internal static SaveSettingData<SettingData> Setting { get; } = new SaveSettingData<SettingData>(ExeDir + "\\setting.json");
    }

}
