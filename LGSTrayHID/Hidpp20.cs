using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LGSTrayHID
{
    public readonly struct Hidpp20
    {
        private readonly byte[] _data;

        public Hidpp20(byte[] data)
        {
            this._data = data;
        }

        public static explicit operator byte[](Hidpp20 msg) => msg._data;

        public static implicit operator Hidpp20(byte[] data) => new(data);

        public byte this[int index] => _data[index];

        public int Length => _data.Length;

        public byte GetDeviceIdx()
        {
            return _data[1];
        }

        public byte GetFeatureIndex()
        {
            return _data[2];
        }

        public byte GetFunctionId()
        {
            return (byte)((_data[3] & 0xF0) >> 4);
        }

        public byte GetSoftwareId()
        {
            return (byte)(_data[3] & 0x0F);
        }

        public Span<byte> GetParams()
        {
            return _data.AsSpan(4);
        }

        public byte GetParam(int paramIdx)
        {
            return _data[4 + paramIdx];
        }
    }
}
