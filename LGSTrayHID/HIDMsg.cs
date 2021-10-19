using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
    public static class HIDMsg
    {
        public static async Task<TransferResult?> WriteReadyTimeoutAsync(this IDevice device, byte[] payload, UInt16 timeout = 11500)
        {
            var updateTask = device.WriteAndReadAsync(payload);

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

        public static async Task<int> GetProtocolAsync(IDevice device, byte deviceId)
        {
            byte[] payload = CreateBlankHIDMsg();
            payload[1] = deviceId;
            payload[2] = 0x00;
            payload[3] = 0x10;

            Task<TransferResult> task = device.WriteAndReadAsync(payload);
            if (await Task.WhenAny(task, Task.Delay(500)) == task)
            {
                return ((HidData)task.Result).Param(0);
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

            var res = await device.WriteAndReadAsync(payload);

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
