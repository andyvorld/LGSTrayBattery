using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PropertyChanged;

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
        private const double MIN_UPDATE_PERIOD_S = 60;

        protected DeviceType _deviceType = DeviceType.Mouse;
        public DeviceType DeviceType { get; set; }
        public string DeviceID { get; set; } = "NOT_FOUND";
        public string DeviceName { get; set; } = "NOT_FOUND";
        public bool HasBattery { get; set; } = true;
        public abstract double BatteryPercentage { get; set; }
        public DateTime LastUpdate { get; private set; } = DateTime.MinValue;
        public bool BatteryStatExpired
        {
            get
            {
#if DEBUG
                return true;
#else
                return DateTime.Now > LastUpdate.AddSeconds(MIN_UPDATE_PERIOD_S);
#endif
            }
        }

        [DependsOn("DeviceName", "BatteryPercentage", "LastUpdate")]
        public string TooltipString
        {
            get
            {
#if DEBUG
                return $"{DeviceName}, {BatteryPercentage:f2}% at {LastUpdate}";
#else
                return $"{DeviceName}, {BatteryPercentage:f2}%";
#endif
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void InvokePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(sender, e);
        }

        public abstract string GetXmlData();
        public void UpdateLastUpdateTimestamp()
        {
            LastUpdate = DateTime.Now;
        }
    }
}
