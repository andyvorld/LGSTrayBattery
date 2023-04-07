using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LGSTrayCore;

namespace LGSTrayGHUB
{
    public class LogiDeviceGHUB : LogiDevice
    {
        //private string _deviceID;

        //private string _deviceName;

        private double _batteryPercentage;
        //public override string DeviceID { get => _deviceID; set => _deviceID = value; }
        //public override string DeviceName { get => _deviceName; set => _deviceName = value; }
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
        public double Mileage { get; set; }
        public override string GetXmlData()
        {
            return
                $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                $"<xml>" +
                $"<device_id>{DeviceID}</device_id>" +
                $"<device_name>{DeviceName}</device_name>" +
                $"<device_type>{DeviceType}</device_type>" +
                $"<battery_percent>{BatteryPercentage:f2}</battery_percent>" +
                $"<mileage>{Mileage:f2}</mileage>" +
                $"<charging>{Charging}</charging>" +
                $"<data_source>LogiDeviceGHUB</data_source>" +
                $"</xml>"
                ;
        }
    }
}
