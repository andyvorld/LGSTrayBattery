using MessagePack;

namespace LGSTrayPrimitives.MessageStructs;

public enum IPCMessageType : byte
{
    HEARTBEAT = 0,
    INIT,
    UPDATE
}

public enum IPCMessageRequestType : byte
{
    BATTERY_UPDATE_REQUEST = 0
}

[Union(0, typeof(InitMessage))]
[Union(1, typeof(UpdateMessage))]
public abstract class IPCMessage(string deviceId)
{
    [Key(0)]
    public string deviceId = deviceId;
}

[MessagePackObject]
public class InitMessage(string deviceId, string deviceName, bool hasBattery, DeviceType deviceType) : IPCMessage(deviceId)
{
    [Key(1)]
    public string deviceName = deviceName;

    [Key(2)]
    public bool hasBattery = hasBattery;

    [Key(3)]
    public DeviceType deviceType = deviceType;
}

[MessagePackObject]
public class UpdateMessage(
    string deviceId,
    double batteryPercentage,
    PowerSupplyStatus powerSupplyStatus,
    int batteryMVolt,
    DateTimeOffset updateTime
) : IPCMessage(deviceId)
{
    [Key(1)]
    public double batteryPercentage = batteryPercentage;

    [Key(2)]
    public PowerSupplyStatus powerSupplyStatus = powerSupplyStatus;

    [Key(3)]
    public int batteryMVolt = batteryMVolt;

    [Key(4)]
    public DateTimeOffset updateTime = updateTime;
}

[MessagePackObject]
public class BatteryUpdateRequestMessage()
{
    [Key(0)]
    public int id;
}
