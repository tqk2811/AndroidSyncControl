using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndroidSyncControl.DataClass
{
    internal class SettingData
    {
        public double ViewPercent { get; set; } = 30;
        public int MaxFps { get; set; } = 24;
        public int MaxSize { get; set; } = 0;
        public int Timeout { get; set; } = 5000;
        public bool UseGpu { get; set; } = true;
        public bool IsAudio { get; set; } = false;

        /// <summary>UI theme mode: "System" (follow OS), "Light" or "Dark".</summary>
        public string Theme { get; set; } = "System";

        /// <summary>UI language: "English" or "Vietnamese". Empty = follow the Windows display language on first run.</summary>
        public string Language { get; set; } = "";
    }
}
