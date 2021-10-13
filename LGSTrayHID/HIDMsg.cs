using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Device.Net;
using Hid.Net;

namespace LGSTrayHID
{
    public static class HIDMsg
    {
        public static async Task<int> GetProtocolAsync(IDevice device)
        {
            byte[] payload = new byte[7];
            payload[0] = 0x10;
            payload[1] = 0x01;
            payload[2] = 0x00;
            payload[3] = 0x03;
            payload[4] = 0x00;
            payload[5] = 0x00;
            payload[6] = 0x00;

            await device.InitializeAsync();
            device.WriteAsync(payload).Wait();


            return 0;
        }

        private static byte[] BlankMsg()
        {
            byte[] msg = new byte[7];

            msg[0] = 0x10;

            return msg;
        }

        private class HidData
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
            //public static implicit operator HidData(TransferResult d) => new HidData() { _data = d.Data };

            public static implicit operator byte[](HidData d) => d._data;

            public bool CompareSignature(HidData other)
            {
                return (this._data[1] == other._data[1]) && (this._data[2] == other._data[2]) && (this._data[3] == other._data[3]);
            }
        }
    }
}
