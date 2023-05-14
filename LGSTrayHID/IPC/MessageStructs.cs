using LGSTrayCore;
using System.Text;

namespace LGSTrayHID.MessageStructs
{
    public enum MessageType : byte
    {
        HEARTBEAT = 0,
        INIT,
        UPDATE 
    }

    public struct InitStruct
    {
        public string deviceId;
        public string deviceName;
        public bool hasBattery;

        public static InitStruct FromByteArray(byte[] bytes)
        {
            return new()
            {
                deviceId = Encoding.ASCII.GetString(bytes.AsSpan(1, 256)).TrimEnd('\0'),
                deviceName = Encoding.ASCII.GetString(bytes.AsSpan(1 + 256, 256)).TrimEnd('\0'),
                hasBattery = bytes.ElementAt(1 + 256 + 256) != 0,
            };
        }

        public byte[] ToByteArray()
        {
            byte[] output = new byte[1 + 256 + 256 + 1];

            output[0] = (byte) MessageType.INIT;
            Encoding.ASCII.GetBytes(deviceId).CopyTo(output, 1);
            Encoding.ASCII.GetBytes(deviceName).CopyTo(output, 1 + 256);
            output[1 + 256 + 256] = (byte) (hasBattery ? 1 : 0);

            return output;
        }
    }

    public struct UpdateStruct
    {
        public string deviceId;
        public double batteryPercentage;
        public PowerSupplyStatus status;
        public int batteryMVolt;

        public static UpdateStruct FromByteArray(byte[] bytes)
        {
            return new()
            {
                deviceId = Encoding.ASCII.GetString(bytes.AsSpan(1, 256)).TrimEnd('\0'),
                batteryPercentage = BitConverter.ToDouble(bytes, 1 + 256),
                status = (PowerSupplyStatus)bytes[1 + 256 + 8],
                batteryMVolt = BitConverter.ToInt32(bytes, 1 + 256 + 8 + 1)
            };
        }

        public byte[] ToByteArray()
        {
            byte[] output = new byte[1 + 256 + 8 + 1 + 4];

            output[0] = (byte) MessageType.UPDATE;
            Encoding.ASCII.GetBytes(deviceId).CopyTo(output, 1);
            BitConverter.GetBytes(batteryPercentage).CopyTo(output, 1 + 256);
            output[1 + 256 + 8] = (byte) status;
            BitConverter.GetBytes(batteryMVolt).CopyTo(output, 1 + 256 + 8 +1);

            return output;
        }
    }
}
