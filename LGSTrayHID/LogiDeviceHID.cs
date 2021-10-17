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
        private static IPowerModel powerModel = new PowerModel_3deg();
        private string _deviceName = "NOT_FOUND";
        public override string DeviceID { get => _hidDevice.DeviceId.GetHashCode().ToString(); set => throw new NotImplementedException(); }
        public override string DeviceName { get => _deviceName; set => _deviceName = value; }

        private double _batteryPercentage = double.NaN;
        public override double BatteryPercentage { get => _batteryPercentage; set => _batteryPercentage = value; }

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
            }
        }

        protected internal IDevice _hidDevice;


        private byte _deviceNameIdx = 0;
        private byte _batteryStatusIdx = 0;
        private byte _batteryVoltageIdx = 0;
        protected internal async Task<bool> InitializeDeviceAsync()
        {
            var version = await HIDMsg.GetProtocolAsync(_hidDevice, 0x01);

            // Magic number for HID++ 1.0, not supported
            if (version == -1)
            {
                Debug.WriteLine($"{_hidDevice.DeviceId} failed to response to GetProtocol");
                _hidDevice.Close();
                return false;
            }
            if (version == 0x8f)
            {
                Debug.WriteLine($"{_hidDevice.DeviceId} is HID++ 1.0, not supported");
                return false;
            }

            byte[] payload;
            _deviceNameIdx = await HIDMsg.GetFeatureIdx(_hidDevice, 0x01, HIDFeatureID.DEVICE_NAME);
            if (_deviceNameIdx != 0)
            {
                payload = HIDMsg.CreateHIDMsg(0x01, _deviceNameIdx, 0x02);
                DeviceType = (LGSTrayCore.DeviceType)((HIDMsg.HidData)(await _hidDevice.WriteAndReadAsync(payload))).Param(0);

                payload = HIDMsg.CreateHIDMsg(0x01, _deviceNameIdx, 0x00);
                int nameLength = ((HIDMsg.HidData)(await _hidDevice.WriteAndReadAsync(payload))).Param(0);
                byte[] nameBuffer = new byte[nameLength];
                for (byte i = 0; i < nameLength; i += 15)
                {
                    payload = HIDMsg.CreateHIDMsg(0x01, _deviceNameIdx, 0x01, new byte[] { i });
                    var res = await _hidDevice.WriteAndReadAsync(payload);

                    Buffer.BlockCopy(res.Data, 4, nameBuffer, i, Math.Min(nameLength - i, 15));
                }
                DeviceName = Encoding.ASCII.GetString(nameBuffer);
            }


            _batteryStatusIdx = await HIDMsg.GetFeatureIdx(_hidDevice, 0x01, HIDFeatureID.BATTERY_STATUS);
            if (_batteryStatusIdx == 0)
            {
                _batteryVoltageIdx = await HIDMsg.GetFeatureIdx(_hidDevice, 0x01, HIDFeatureID.BATTERY_VOLTAGE);
            }

            return true;
        }

        public async Task UpdateBattery()
        {
            if (_batteryVoltageIdx != 0)
            {
                await UpdateBatteryVoltage();
            }
            else if (_batteryStatusIdx != 0)
            {
                await UpdateBatteryStatus();
            }
        }

        private async Task UpdateBatteryStatus()
        {
            byte[] payload = HIDMsg.CreateHIDMsg(0x01, _batteryStatusIdx, 0x00);
            var resData = (HIDMsg.HidData)(await _hidDevice.WriteAndReadAsync(payload));

            BatteryPercentage = resData.Param(0);
        }

        private async Task UpdateBatteryVoltage()
        {
            byte[] payload = HIDMsg.CreateHIDMsg(0x01, _batteryVoltageIdx, 0x00);
            var resData = (HIDMsg.HidData)(await _hidDevice.WriteAndReadAsync(payload));

            BatteryVoltage = 0.001 * ((resData.Param(0) << 8) + resData.Param(1));
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
                $"</xml>"
                ;
        }
    }
}
