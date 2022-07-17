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

    public static class HIDMsg
    {
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

        public static byte[] CreateBlankHIDMsg()
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
