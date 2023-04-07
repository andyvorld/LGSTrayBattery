using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LGSTrayCore;
using Device.Net;
using Hid.Net;
using System.Diagnostics;

namespace LGSTrayHID
{
    public class LogiDeviceHID : LogiDevice
    {
        private static readonly IPowerModel powerModel = new PowerModel_3deg();
        //private string _deviceName = "NOT_FOUND";
        //private string _deviceId = "NOT_FOUND";
        //public override string DeviceID { get => _deviceId; set => _deviceId = value; }
        //public override string DeviceName { get => _deviceName; set => _deviceName = value; }

        private double _batteryPercentage = double.NaN;
        public override double BatteryPercentage
        {
            get
            {
                return _batteryPercentage;
            }

            set
            {
                _batteryPercentage = value;
                Debug.WriteLine($"batt updated - {BatteryPercentage}%");
                UpdateLastUpdateTimestamp();
            }
        }

        private double _batteryVoltage = double.NaN;
        public double BatteryVoltage
        {
            get
            {
                return _batteryVoltage;
            }
            set
            {
                _batteryVoltage = value;
                BatteryPercentage = 100*powerModel.GetCapacity(_batteryVoltage);
                UpdateLastUpdateTimestamp(); ;
            }
        }
        public override string GetXmlData()
        {
            return
                $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                $"<xml>" +
                $"<device_id>{DeviceID}</device_id>" +
                $"<device_name>{DeviceName}</device_name>" +
                $"<device_type>{DeviceType}</device_type>" +
                $"<battery_voltage>{BatteryVoltage:f2}</battery_voltage>" +
                $"<battery_percent>{BatteryPercentage:f2}</battery_percent>" +
                $"<data_source>LogiDeviceHID</data_source>" +
                $"</xml>"
                ;
        }
    }
}
