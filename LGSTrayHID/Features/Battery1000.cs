using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LGSTrayHID.Features
{
    public static class Battery1000
    {
        public static async Task<(double, int)> GetBatteryAsync(HidppDevice device)
        {
            Hidpp20 buffer = new byte[7] { 0x10, device.DeviceIdx, device.FeatureMap[0x1000], 0x00 | HidppDevices.SW_ID, 0x00, 0x00, 0x00 };
            Hidpp20 ret = await device.Parent.WriteRead20(device.Parent.DevShort, buffer);

            int mv = -1;
            double batPercent = ret.GetParam(0);

            Console.WriteLine($"Bat status {ret.GetParam(2)}");

            return (batPercent, mv);
        }
    }
}
