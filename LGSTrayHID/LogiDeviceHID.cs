using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LGSTrayCore;
using Device.Net;
using Hid.Net;

namespace LGSTrayHID
{
    public class LogiDeviceHID : LogiDevice
    {
        public override string DeviceID { get => _hidDevice.DeviceId.GetHashCode().ToString(); set => throw new NotImplementedException(); }
        public override string DeviceName { get => "Test"; set => throw new NotImplementedException(); }
        public override double BatteryPercentage { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override string TooltipString { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        protected internal IDevice _hidDevice;

        protected internal bool InitializeDevice()
        {
            HIDMsg.GetProtocolAsync(_hidDevice).Wait();

            return true;
        }
        public override string GetXmlData()
        {
            return
                $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                $"<xml>" +
                $"<device_id>{DeviceID}</device_id>" +
                $"<device_name>{DeviceName}</device_name>" +
                //$"<device_type>{DeviceType}</device_type>" +
                //$"<battery_percent>{BatteryPercentage:f2}</battery_percent>" +
                $"</xml>"
                ;
        }
    }
}
