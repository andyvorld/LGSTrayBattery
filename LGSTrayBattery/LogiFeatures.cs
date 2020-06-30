using System;
using System.Collections.Generic;
using System.Linq;

namespace LGSTrayBattery
{
    static class LogiFeatures
    {
        private static Dictionary<string, UInt16> _featureDict;
        private static Dictionary<UInt16, string> _revDict;

        public static void LoadConfig()
        {
            _featureDict = new Dictionary<string, UInt16>
            {
                { "ROOT", 0x0000 },
                { "FEATURE_SET", 0x0001 },
                { "FEATURE_INFO", 0x0002 },
                { "DEVICE_NAME", 0x0005 },
                { "BATTERY_STATUS", 0x1000 },
                { "BATTERY_VOLTAGE", 0x1001 }
            };

            _revDict = _featureDict.ToDictionary((x) => x.Value, (x) => x.Key);
        }

        public static string GetName(UInt16 hexCode)
        {
            return _revDict.ContainsKey(hexCode) ? _revDict[hexCode] : "UNKNOWN";
        }

        public static string GetName(byte[] hexCodeBytes)
        {
            if (hexCodeBytes.Length != 2)
            {
                throw new Exception("Invalid byte array for hexcode");
            }


            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(hexCodeBytes);
            }

            UInt16 hexCode = BitConverter.ToUInt16(hexCodeBytes, 0);

            return GetName(hexCode);
        }

        public static byte[] GetHexCodeArray(string featureName)
        {
            if (!_featureDict.ContainsKey(featureName))
            {
                throw new Exception($"Invalid featureName: {featureName}");
            }

            byte[] byteArray = new byte[2];

            byteArray[0] = (byte)((_featureDict[featureName] >> 16) & 0xFF);
            byteArray[1] = (byte)(_featureDict[featureName] & 0xFF);

            return byteArray;
        }
    }
}