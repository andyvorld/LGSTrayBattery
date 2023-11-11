using LGSTrayPrimitives;

namespace LGSTrayHID.Features
{
    public readonly struct BatteryUpdateReturn
    {
        public readonly double batteryPercentage;
        public readonly PowerSupplyStatus status;
        public readonly int batteryMVolt;

        public BatteryUpdateReturn()
        {
            this.batteryPercentage = 0;
            this.status = PowerSupplyStatus.POWER_SUPPLY_STATUS_UNKNOWN;
            this.batteryMVolt = -1;
        }

        public BatteryUpdateReturn(double batteryPercentage, PowerSupplyStatus status, int batteryMVolt)
        {
            this.batteryPercentage = batteryPercentage;
            this.status = status;
            this.batteryMVolt = batteryMVolt;
        }
    }
}
