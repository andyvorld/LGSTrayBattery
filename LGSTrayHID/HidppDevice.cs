using LGSTrayHID.HidApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static LGSTrayHID.HidppDevices;

namespace LGSTrayHID
{
    public class HidppDevice
    {
        private readonly byte _deviceIdx;
        private readonly HidppDevices _parent;
        private readonly Dictionary<ushort, byte> _featureMap = new();

        public string DeviceName { get; private set;} = string.Empty;
        public int DeviceType { get; private set; } = 3;
        public string Identifier { get; private set; } = string.Empty;

        public HidppDevice(HidppDevices parent, byte deviceIdx)
        {
            _parent = parent;
            _deviceIdx = deviceIdx;
        }

        public void ProcessMessage(byte[] buffer)
        {
            // STUB
        }

        //public async Task<byte[]> WriteRead20(HidDevicePtr hidDevicePtr, byte[] buffer)
        //{
        //    if (_tcs != null)
        //    {
        //        throw new Exception("Tried to WriteRead20 while another WriteRead20 is in flight");
        //    }
        //    _tcs = new();

        //    try
        //    {
        //        await hidDevicePtr.WriteAsync(buffer);

        //        if (_tcs.Task != await Task.WhenAny(_tcs.Task, Task.Delay(1000)))
        //        {
        //            _tcs.SetCanceled();
        //            return Array.Empty<byte>();
        //        }

        //        return await _tcs.Task;
        //    }
        //    finally
        //    {
        //        _tcs = null;
        //    }
        //}

        public async Task InitAsync()
        {
            Hidpp20 ret;

            // Find 0x0001 IFeatureSet
            ret = await _parent.WriteRead20(_parent.DevShort, new byte[7] { 0x10, _deviceIdx, 0x00, 0x00 | SW_ID, 0x00, 0x01, 0x00 });
            _featureMap[0x0001] = ret.GetParam(0);

            // Get Feature Count
            ret = await _parent.WriteRead20(_parent.DevShort, new byte[7] { 0x10, _deviceIdx, _featureMap[0x0001], 0x00 | SW_ID, 0x00, 0x00, 0x00 });
            int featureCount = ret.GetParam(0);

            // Enumerate Features
            for (byte i = 0; i <= featureCount; i++)
            {
                ret = await _parent.WriteRead20(_parent.DevShort, new byte[7] { 0x10, _deviceIdx, _featureMap[0x0001], 0x10 | SW_ID, i, 0x00, 0x00 });
                ushort featureId = (ushort) ((ret.GetParam(0) << 8) + ret.GetParam(1));

                _featureMap[featureId] = i;
            }

            await InitPopulateAsync();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0018:Inline variable declaration")]
        private async Task InitPopulateAsync()
        {
            Hidpp20 ret;
            byte featureId;

            // Device name
            if (_featureMap.TryGetValue(0x0005, out featureId))
            {
                ret = await _parent.WriteRead20(_parent.DevShort, new byte[7] { 0x10, _deviceIdx, featureId, 0x00 | SW_ID, 0x00, 0x00, 0x00 });
                int nameLength = ret.GetParam(0);

                string name = "";

                while (name.Length < nameLength)
                {
                    ret = await _parent.WriteRead20(_parent.DevShort, new byte[7] { 0x10, _deviceIdx, featureId, 0x10 | SW_ID, (byte)name.Length, 0x00, 0x00 });
                    name += Encoding.UTF8.GetString(ret.GetParams());
                }

                DeviceName = name.TrimEnd('\0');

                ret = await _parent.WriteRead20(_parent.DevShort, new byte[7] { 0x10, _deviceIdx, featureId, 0x20 | SW_ID, 0x00, 0x00, 0x00 });
                DeviceType = ret.GetParam(0);
            }

            if (_featureMap.TryGetValue(0x0003, out featureId))
            {
                ret = await _parent.WriteRead20(_parent.DevShort, new byte[7] { 0x10, _deviceIdx, featureId, 0x00 | SW_ID, 0x00, 0x00, 0x00 });

                string unitId = BitConverter.ToString(ret.GetParams().ToArray(), 1, 4).Replace("-", string.Empty);
                string modelId = BitConverter.ToString(ret.GetParams().ToArray(), 7, 5).Replace("-", string.Empty);

                bool serialNumberSupported = (ret.GetParam(14) & 0x1) == 0x1;
                string? serialNumber = null;
                if (serialNumberSupported)
                {
                    ret = await _parent.WriteRead20(_parent.DevShort, new byte[7] { 0x10, _deviceIdx, featureId, 0x20 | SW_ID, 0x00, 0x00, 0x00 });
                    serialNumber = BitConverter.ToString(ret.GetParams().ToArray(), 0, 11).Replace("-", string.Empty);
                }

                Identifier = serialNumber ?? $"{unitId}-{modelId}";

            }
            Console.WriteLine("---");
            Console.WriteLine(DeviceName + " Ready");
            Console.WriteLine(Identifier + " Ready");
            Console.WriteLine("---");

        }
    }
}
