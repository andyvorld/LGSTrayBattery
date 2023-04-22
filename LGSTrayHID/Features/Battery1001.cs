using LGSTrayHID.HidApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LGSTrayHID.Features
{
    public static class Battery1001
    {
        public enum PowerSupplyStatus : ushort
        {
            POWER_SUPPLY_STATUS_DISCHARGING = 0,
            POWER_SUPPLY_STATUS_CHARGING,
            POWER_SUPPLY_STATUS_FULL,
            POWER_SUPPLY_STATUS_NOT_CHARGING,
            POWER_SUPPLY_STATUS_UNKNOWN
        }

        static readonly int[] _mvLUT = new[] {
            4186, 4156, 4143, 4133, 4122, 4113, 4103, 4094, 4086, 4075,
            4067, 4059, 4051, 4043, 4035, 4027, 4019, 4011, 4003, 3997,
            3989, 3983, 3976, 3969, 3961, 3955, 3949, 3942, 3935, 3929,
            3922, 3916, 3909, 3902, 3896, 3890, 3883, 3877, 3870, 3865,
            3859, 3853, 3848, 3842, 3837, 3833, 3828, 3824, 3819, 3815,
            3811, 3808, 3804, 3800, 3797, 3793, 3790, 3787, 3784, 3781,
            3778, 3775, 3772, 3770, 3767, 3764, 3762, 3759, 3757, 3754,
            3751, 3748, 3744, 3741, 3737, 3734, 3730, 3726, 3724, 3720,
            3717, 3714, 3710, 3706, 3702, 3697, 3693, 3688, 3683, 3677,
            3671, 3666, 3662, 3658, 3654, 3646, 3633, 3612, 3579, 3537
        };

        private static double LookupBatPercent(int mv)
        {
            for (int i = 0; i < _mvLUT.Length; i++)
            {
                if (mv > _mvLUT[i])
                {
                    return _mvLUT.Length - i;
                }
            }

            return 0;
        }

        public static async Task<(double, int)> GetBatteryAsync(HidppDevices device, byte deviceIdx, byte featureId)
        {
            Hidpp20 buffer = new byte[7] { 0x10, deviceIdx, featureId, 0x00 | HidppDevices.SW_ID, 0x00, 0x00, 0x00 };
            Hidpp20 ret = await device.WriteRead20(device.DevShort, buffer, 10000);

            int mv = (ret.GetParam(0) << 8) + ret.GetParam(1);
            double batPercent = LookupBatPercent(mv);
            byte flags = ret.GetParam(2);

            PowerSupplyStatus status;
            if ((flags & 0x80) > 0)
            {
                status = (flags & 0x07) switch
                {
                    0 => PowerSupplyStatus.POWER_SUPPLY_STATUS_FULL,
                    1 => PowerSupplyStatus.POWER_SUPPLY_STATUS_CHARGING,
                    2 => PowerSupplyStatus.POWER_SUPPLY_STATUS_NOT_CHARGING,
                    _ => PowerSupplyStatus.POWER_SUPPLY_STATUS_UNKNOWN,
                };
            }
            else
            {
                status = PowerSupplyStatus.POWER_SUPPLY_STATUS_DISCHARGING;
            }

            Console.WriteLine(status.ToString());

            return (batPercent, mv);
        }
    }
}
