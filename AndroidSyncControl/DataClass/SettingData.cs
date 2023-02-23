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
        public int Timeout { get; set; } = 5000;
    }
}
