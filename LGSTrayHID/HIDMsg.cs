using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Device.Net;
using Hid.Net;

namespace LGSTrayHID
{
    public enum HIDFeatureID : ushort
    {
        DEVICE_NAME = 0x0005,
        BATTERY_STATUS = 0x1000,
        BATTERY_VOLTAGE = 0x1001
    }

    public struct HIDPPProfile
    {
        public string deviceName;
        public LGSTrayCore.DeviceType deviceType;
        public byte deviceNameIdx;
        public byte batteryStatusIdx;
        public byte batteryVoltageIdx;
    }
    public static class HIDMsg
    {
        public static async Task<TransferResult?> WriteReadTimeoutAsync(this IDevice device, byte[] payload, UInt16 timeout = 5000)
        {
            Task<TransferResult?> updateTask = Task.Run(async () => {
                TransferResult? output = null;
                try
                {
                    output = await device.WriteAndReadAsync(payload);
                }
                catch
                {
                }
                return output;
            });

            await Task.WhenAny(updateTask, Task.Delay(timeout));

            if (updateTask.IsCompleted)
            {
                return updateTask.Result;
            }
            else
            {
                Debug.WriteLine("Device Failed to response in time");
            }

            return null;
        }

        public static async Task<HIDPPProfile?> InitializeHIDPPAsync(IDevice device)
        {
            HIDPPProfile output = new HIDPPProfile();

            int version;
            await Task.Delay(10000);
            version = await GetProtocolAsync(device, 0x01);

            // Magic number for HID++ 1.0, not supported
            if (version == -1)
            {
                Debug.WriteLine($"{device.DeviceId} failed to response to GetProtocol");
                device.Dispose();
                return null;
            }
            if (version == 0x8f)
            {
                Debug.WriteLine($"{device.DeviceId} is HID++ 1.0, not supported");
                return null;
            }

            byte[] payload;
            output.deviceNameIdx = await GetFeatureIdx(device, 0x01, HIDFeatureID.DEVICE_NAME);
            if (output.deviceNameIdx != 0)
            {
                payload = CreateHIDMsg(0x01, output.deviceNameIdx, 0x02);
                output.deviceType = (LGSTrayCore.DeviceType)((HidData)(await device.WriteReadTimeoutAsync(payload))).Param(0);

                payload = CreateHIDMsg(0x01, output.deviceNameIdx, 0x00);
                int nameLength = ((HIDMsg.HidData)(await device.WriteReadTimeoutAsync(payload))).Param(0);
                byte[] nameBuffer = new byte[nameLength];
                for (byte i = 0; i < nameLength; i += 15)
                {
                    payload = CreateHIDMsg(0x01, output.deviceNameIdx, 0x01, new byte[] { i });
                    var res = await device.WriteReadTimeoutAsync(payload);

                    Buffer.BlockCopy(res?.Data, 4, nameBuffer, i, Math.Min(nameLength - i, 15));
                }
                output.deviceName = Encoding.ASCII.GetString(nameBuffer);
            }

            output.batteryStatusIdx = await GetFeatureIdx(device, 0x01, HIDFeatureID.BATTERY_STATUS);
            if (output.batteryStatusIdx == 0)
            {
                output.batteryVoltageIdx = await GetFeatureIdx(device, 0x01, HIDFeatureID.BATTERY_VOLTAGE);
            }

            return output;
        }

        public static async Task<double> UpdateBattery(IDevice device, HIDPPProfile profile)
        {
            if (profile.batteryStatusIdx != 0)
            {
                return await UpdateBatteryStatus(device, profile);
            }
            else if (profile.batteryVoltageIdx != 0)
            {
                return await UpdateBatteryVoltage(device, profile);
            }

            return double.NaN;
        }

        private async static Task<double> UpdateBatteryStatus(IDevice device, HIDPPProfile profile)
        {
            byte[] payload = CreateHIDMsg(0x01, profile.batteryStatusIdx, 0x00);
            var resData = await device.WriteReadTimeoutAsync(payload);

            if (resData != null)
            {
                return ((HIDMsg.HidData)resData).Param(0);
            }

            return double.NaN;
        }

        private async static Task<double> UpdateBatteryVoltage(IDevice device, HIDPPProfile profile)
        {
            byte[] payload = CreateHIDMsg(0x01, profile.batteryVoltageIdx, 0x00);
            var resData = await device.WriteReadTimeoutAsync(payload);

            if (resData != null)
            {
                var tmp = (HIDMsg.HidData)resData;
                return 0.001 * ((tmp.Param(0) << 8) + tmp.Param(1));
            }

            return double.NaN;
        }

        public static async Task<int> GetProtocolAsync(IDevice device, byte deviceId)
        {
            byte[] payload = CreateBlankHIDMsg();
            payload[1] = deviceId;
            payload[2] = 0x00;
            payload[3] = 0x10;

            var version = await device.WriteReadTimeoutAsync(payload);
            if (version != null)
            {
                return ((HidData)version).Param(0);
            }

            return -1;
        }

        public static async Task<byte> GetFeatureIdx(IDevice device, byte deviceId, UInt16 featureId)
        {
            byte[] payload = CreateBlankHIDMsg();
            payload[1] = deviceId;
            payload[2] = 0x00;
            payload[3] = 0x00;
            payload[4] = (byte) ((featureId & 0xFF00) >> 8);
            payload[5] = (byte) ((featureId & 0x00FF));

            var res = await device.WriteReadTimeoutAsync(payload);

            return ((HidData)res).Param(0);
        }

        public static async Task<byte> GetFeatureIdx(IDevice device, byte deviceId, HIDFeatureID featureID)
        {
            return await GetFeatureIdx(device, deviceId, (UInt16)featureID);
        }

        public static byte[] CreateHIDMsg(byte deviceID, byte featureIdx, byte functionIdx, byte[] param = null)
        {
            byte[] payload = CreateBlankHIDMsg();
            payload[1] = deviceID;
            payload[2] = featureIdx;
            payload[3] = (byte) (functionIdx << 4);
            
            if (param != null)
            {
                for (int i = 0; i < param.Count(); i++)
                {
                    payload[i + 4] = param[i];
                }
            }

            return payload;
        }

        private static byte[] CreateBlankHIDMsg()
        {
            byte[] msg = new byte[20];

            msg[0] = 0x11;

            return msg;
        }

        public class HidData
        {
            private byte[] _data;

            // HID++ 2.0
            public byte FeatureIndex => _data[2];
            public byte FunctionId => (byte)(_data[3] & 0xF0);
            public byte SwId => (byte)(_data[3] & 0x0F);

            // Common
            public byte DeviceIndex => _data[1];
            public byte Param(int index) => _data[4 + index];

            public static implicit operator HidData(byte[] d) => new HidData() { _data = d };
            public static implicit operator HidData(TransferResult d) => new HidData() { _data = d.Data };

            public static implicit operator byte[](HidData d) => d._data;

            public bool CompareSignature(HidData other)
            {
                return (this._data[1] == other._data[1]) && (this._data[2] == other._data[2]) && (this._data[3] == other._data[3]);
            }
        }
    }
}
