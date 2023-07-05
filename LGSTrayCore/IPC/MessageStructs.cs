using MessagePack;

namespace LGSTrayCore.MessageStructs
{
    public enum IPCMessageType : byte
    {
        HEARTBEAT = 0,
        INIT,
        UPDATE,
    }

    [Union(0, typeof(InitMessage))]
    [Union(1, typeof(UpdateMessage))]
    public abstract class IPCMessage
    {
        [Key(0)]
        public string deviceId;

        public IPCMessage(string deviceId)
        {
            this.deviceId = deviceId;
        }
    }

    [MessagePackObject]
    public class InitMessage : IPCMessage
    {
        [Key(1)]
        public string deviceName;

        [Key(2)]
        public bool hasBattery;

        [Key(3)]
        public DeviceType deviceType;

        public InitMessage(string deviceId, string deviceName, bool hasBattery, DeviceType deviceType) : base(deviceId)
        {
            this.deviceName = deviceName;
            this.hasBattery = hasBattery;
            this.deviceType = deviceType;
        }
    }

    [MessagePackObject]
    public class UpdateMessage : IPCMessage
    {
        [Key(1)]
        public double batteryPercentage;

        [Key(2)]
        public PowerSupplyStatus powerSupplyStatus;

        [Key(3)]
        public int batteryMVolt;

        [Key(4)]
        public DateTime updateTime;

        public UpdateMessage(
            string deviceId, 
            double batteryPercentage, 
            PowerSupplyStatus powerSupplyStatus, 
            int batteryMVolt, 
            DateTime updateTime
        ) : base(deviceId)
        {
            this.batteryPercentage = batteryPercentage;
            this.powerSupplyStatus = powerSupplyStatus;
            this.batteryMVolt = batteryMVolt;
            this.updateTime = updateTime;
        }
    }
}
