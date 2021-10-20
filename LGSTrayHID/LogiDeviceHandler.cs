using Device.Net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static LGSTrayHID.HIDMsg;

namespace LGSTrayHID
{
    class LogiDeviceHandler
    {
        // Magic number to prevent clashing with LGHUB's 0x0E
        private const byte MAGIC_SW_ID = 0X0A;

        private IDevice _hiddevice;
        public string HIDDeviceId { get => _hiddevice.DeviceId; }
        private CancellationTokenSource cancellationTokenSource = new();

        private static readonly Dictionary<string, LogiDeviceHID> LogiDeviceMap = new();

        private string deviceName;
        public string DeviceName { get => deviceName; }

        private byte deviceNameIdx;
        private byte batteryStatusIdx;
        private byte batteryVoltageIdx;

        private LogiDeviceHandler()
        {

        }

        public static async Task<LogiDeviceHandler> CreateNewHandler(IDevice device)
        {
            var output = new LogiDeviceHandler()
            {
                _hiddevice = device
            };

            if (!(await output.InitializeHIDPPAsync()))
            {
                return null;
            }

            return output;
        }

        public async Task<bool> InitializeHIDPPAsync()
        {
            int version;
            version = await GetProtocolAsync(0x01);

            // Magic number for HID++ 1.0, not supported
            if (version == -1)
            {
                Debug.WriteLine($"{_hiddevice.DeviceId} failed to response to GetProtocol");
                return false;
            }
            if (version == 0x8f)
            {
                Debug.WriteLine($"{_hiddevice.DeviceId} is HID++ 1.0, not supported");
                return false;
            }

            byte[] payload;
            deviceNameIdx = await GetFeatureIdx(0x01, HIDFeatureID.DEVICE_NAME);
            if (deviceNameIdx != 0)
            {

                payload = CreateHIDMsg(0x01, deviceNameIdx, 0x00);
                int nameLength = ((HidData)await WriteReadTimeoutAsync(payload)).Param(0);
                byte[] nameBuffer = new byte[nameLength];
                for (byte i = 0; i < nameLength; i += 15)
                {
                    payload = CreateHIDMsg(0x01, deviceNameIdx, 0x01, new byte[] { i });
                    var res = await WriteReadTimeoutAsync(payload);

                    Buffer.BlockCopy(res?.Data, 4, nameBuffer, i, Math.Min(nameLength - i, 15));
                }
                deviceName = Encoding.ASCII.GetString(nameBuffer);

                payload = CreateHIDMsg(0x01, deviceNameIdx, 0x02);
                var deviceType = (LGSTrayCore.DeviceType)((HidData)await WriteReadTimeoutAsync(payload)).Param(0);

                var logiDeviceHid = GetLogiDeviceHID();
                logiDeviceHid.DeviceName = deviceName;
                logiDeviceHid.DeviceType = deviceType;
                logiDeviceHid.DeviceID = deviceName.GetHashCode().ToString("X", NumberFormatInfo.InvariantInfo);
            }

            batteryStatusIdx = await GetFeatureIdx(0x01, HIDFeatureID.BATTERY_STATUS);
            if (batteryStatusIdx == 0)
            {
                batteryVoltageIdx = await GetFeatureIdx(0x01, HIDFeatureID.BATTERY_VOLTAGE);
            }

            return true;
        }

        public void StartRead()
        {
            if (cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource = new();
            }

            _ = ReadLoop();
        }

        private async Task ReadLoop()
        {
            var cancellationToken = cancellationTokenSource.Token;
            while (true)
            {
                var res = _hiddevice.ReadAsync(cancellationToken);
                await res;
                if (res.IsCanceled)
                {
                    break;
                }

                HidData hidData = res.Result;
                
                if (batteryStatusIdx != 0 && hidData.FeatureIndex == batteryStatusIdx)
                {
                    GetLogiDeviceHID().BatteryPercentage = hidData.Param(0);
                }

                if (batteryVoltageIdx != 0 && hidData.FeatureIndex == batteryVoltageIdx)
                {
                    GetLogiDeviceHID().BatteryVoltage = 0.001 * ((hidData.Param(0) << 8) + hidData.Param(1));
                }
            }
        }

        public LogiDeviceHID GetLogiDeviceHID()
        {
            if (!LogiDeviceMap.ContainsKey(deviceName))
            {
                LogiDeviceMap[deviceName] = new();
            }

            return LogiDeviceMap[deviceName];
        }

        public async Task UpdateBattery()
        {
            if (!GetLogiDeviceHID().BatteryStatExpired)
            {
                return;
            }

            if (batteryStatusIdx != 0)
            {
                await UpdateBatteryStatus();
            }
            else if (batteryVoltageIdx != 0)
            {
                await UpdateBatteryVoltage();
            }

            return;
        }

        private async Task UpdateBatteryStatus()
        {
            byte[] payload = CreateHIDMsg(0x01, batteryStatusIdx, 0x00);
            await _hiddevice.WriteAsync(payload);
        }

        private async Task UpdateBatteryVoltage()
        {
            byte[] payload = CreateHIDMsg(0x01, batteryVoltageIdx, 0x00);
            await _hiddevice.WriteAsync(payload);
        }

        // HID++ SW checking
        private async Task<TransferResult?> WriteReadTimeoutAsync(byte[] payload, UInt16 timeout = 5000)
        {
            Debug.Assert((payload[3] & 0x0F) == 0, "SW_ID field filled, payload might be HID++ 1.0");

            payload[3] = (byte)((payload[3] & 0xF0) + MAGIC_SW_ID);

            var cTokenSource = new CancellationTokenSource();
            cTokenSource.CancelAfter(timeout);
            _ = _hiddevice.WriteAsync(payload, cTokenSource.Token);
            TransferResult? res = null;
            do
            {
                try
                {
                    res = await _hiddevice.ReadAsync(cTokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                    Debug.WriteLine("Device Failed to response in time");
                    break;
                }
            } while (((HidData)res).SwId != MAGIC_SW_ID);

            return res;
        }

        private async Task<int> GetProtocolAsync(byte deviceId)
        {
            byte[] payload = CreateBlankHIDMsg();
            payload[1] = deviceId;
            payload[2] = 0x00;
            payload[3] = 0x10;

            var version = await WriteReadTimeoutAsync(payload);
            if (version != null)
            {
                return ((HidData)version).Param(0);
            }

            return -1;
        }

        public async Task<byte> GetFeatureIdx(byte deviceId, UInt16 featureId)
        {
            byte[] payload = CreateBlankHIDMsg();
            payload[1] = deviceId;
            payload[2] = 0x00;
            payload[3] = 0x00;
            payload[4] = (byte)((featureId & 0xFF00) >> 8);
            payload[5] = (byte)((featureId & 0x00FF));

            var res = await WriteReadTimeoutAsync(payload);

            return ((HidData)res).Param(0);
        }

        public async Task<byte> GetFeatureIdx(byte deviceId, HIDFeatureID featureID)
        {
            return await GetFeatureIdx(deviceId, (UInt16)featureID);
        }
    }
}
