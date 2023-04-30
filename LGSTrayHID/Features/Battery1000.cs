using LGSTrayCore;
using static LGSTrayCore.PowerSupplyStatus;

namespace LGSTrayHID.Features
{
    public static class Battery1000
    {
        public static async Task<BatteryUpdateReturn?> GetBatteryAsync(HidppDevice device)
        {
            Hidpp20 buffer = new byte[7] { 0x10, device.DeviceIdx, device.FeatureMap[0x1000], 0x00 | HidppDevices.SW_ID, 0x00, 0x00, 0x00 };
            Hidpp20 ret = await device.Parent.WriteRead20(device.Parent.DevShort, buffer);

            if (ret.Length == 0) { return null; }

            int mv = -1;
            double batPercent = ret.GetParam(0);

            PowerSupplyStatus status;
            switch (ret.GetParam(2))
            {
                case 0:
                    status = POWER_SUPPLY_STATUS_DISCHARGING;
                    break;
                case 1:
                case 2:
                    status = POWER_SUPPLY_STATUS_CHARGING;
                    break;
                case 3:
                    status = POWER_SUPPLY_STATUS_FULL;
                    break;
                case 4:
                    status = POWER_SUPPLY_STATUS_CHARGING;
                    break;
                default:
                    status = POWER_SUPPLY_STATUS_NOT_CHARGING;
                    break;
            }

            return new BatteryUpdateReturn(batPercent, status, mv);
        }
    }
}
