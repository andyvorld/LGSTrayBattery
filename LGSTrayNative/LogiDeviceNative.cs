using LGSTrayCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LGSTrayGHUB
{
    public class LogiDeviceNative : LogiDevice
    {
        private double _batteryPercentage;
        public override double BatteryPercentage
        {
            get
            {
                return _batteryPercentage;
            }

            set
            {
                _batteryPercentage = value;
                UpdateLastUpdateTimestamp();
            }
        }
        public bool Charging { get; set; }

        public override string GetXmlData()
        {
            return
                $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                $"<xml>" +
                $"<device_id>{DeviceID}</device_id>" +
                $"<device_name>{DeviceName}</device_name>" +
                $"<device_type>{DeviceType}</device_type>" +
                $"<battery_percent>{BatteryPercentage:f2}</battery_percent>" +
                $"<charging>{Charging}</charging>" +
                $"</xml>"
                ;
        }
    }
}
