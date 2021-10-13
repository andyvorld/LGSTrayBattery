using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LGSTrayCore
{
    public enum DeviceType
    {
        Mouse = 3,
        Keyboard = 0,
        Headset = 8
    }
    public abstract class LogiDevice : INotifyPropertyChanged
    {
        protected DeviceType _deviceType = DeviceType.Mouse;
        public DeviceType DeviceType { get; set; }
        public abstract string DeviceID { get; set; }
        public abstract string DeviceName { get; set; }
        public abstract double BatteryPercentage { get; set; }

        public abstract string TooltipString { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public abstract string GetXmlData();
    }
}
