using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Device.Net;
using Device.Net.Exceptions;
using Hid.Net.Windows;
using PropertyChanged;

namespace LGSTrayBattery
{
    public class LogiDevice : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string UsbSerialId { get; private set; }

        public bool IsChecked { get; set; } = false;

        public string DeviceName { get; private set; }


        [DependsOn("DeviceName", "BatteryVoltage", "BatteryPercentage")]
        public string ToolTipName
        {
            get
            {
                var temp = $"{DeviceName}, {BatteryPercentage:f2}%";
                temp += (BatteryVoltage > 0) ? $" ({BatteryVoltage}V)" : "";

                return temp;
            }
        }

        private double _batteryVoltage;

        public double BatteryVoltage
        {
            get => _batteryVoltage;
            private set
            {
                if (_powermodel == null)
                {
                    _powermodel = new PowerModel(_wpid);
                }

                _batteryVoltage = value;
                BatteryPercentage = _powermodel.GetBatteryPercent(BatteryVoltage);
            }
        }

        public double BatteryPercentage { get; private set; } = Double.NaN;

        private readonly Dictionary<int, IDevice> _hidDevices = new Dictionary<int, IDevice>();
        private readonly Dictionary<string, byte> _featureList = new Dictionary<string, byte>();
        private readonly Dictionary<byte, string> _featureListR = new Dictionary<byte, string>();

        private readonly Random _rand = new Random();

        private readonly double _protocolVer;

        private PowerModel _powermodel;

        private ushort _wpid;

        private bool _listen = false;

        private const int HidTimeOut = 500;

        private LogiDeviceException _lastException;

        private readonly BlockingCollection<HidData> _resDataQueue = new BlockingCollection<HidData>(new ConcurrentQueue<HidData>());

        private Thread _shortListener;
        private Thread _longListener;

        public LogiDevice(IEnumerable<IDevice> devices, string usbSerialId, byte deviceIdx, out bool valid)
        {
            valid = false;
            this.UsbSerialId = $"{usbSerialId}_{deviceIdx}";

            foreach (var device in devices)
            {
                try
                {
                    device.InitializeAsync().Wait();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }

                int outLength = device.ConnectedDeviceDefinition?.ReadBufferSize ?? 0;

                if (_hidDevices.ContainsKey(outLength))
                {
                    break;
                }
                else
                {
                    _hidDevices.Add(outLength, device);
                }

                device.Close();
            }

            if (_hidDevices.ContainsKey((int) HidLength.Short) && _hidDevices.ContainsKey((int) HidLength.Long))
            {
                Listen().Wait();
                _protocolVer = GetProtocolVer().Result;

                if (_protocolVer >= 2.0)
                {
                    valid = true;
                }
                else
                {
                    Debug.WriteLine("HID++ 1.0 not supported");
                }
            }
        }

        ~LogiDevice()
        {
            foreach (var hidDevice in _hidDevices.Values)
            {
                hidDevice.Close();
            }

            _shortListener?.Abort();
            _longListener?.Abort();
        }

        public async Task UpdateBatteryPercentage()
        {
            if (_featureList.ContainsKey("BATTERY_STATUS"))
            {
                (RequestStatus requestStatus, HidData resData) = await WriteReadRequestAsync(HidLength.Short, 0x01, _featureList["BATTERY_STATUS"]).ConfigureAwait(false);

                if (requestStatus == RequestStatus.Success)
                {
                    BatteryPercentage = resData.Param(0);
                }
                else
                {
                    BatteryPercentage = Double.NaN;
                }
            }
            else
            {
                await UpdateBatteryVoltage().ConfigureAwait(false);
            }
        }

        private async Task UpdateBatteryVoltage()
        {
            if (_featureList.ContainsKey("BATTERY_VOLTAGE"))
            {
                (RequestStatus requestStatus, HidData resData) = await WriteReadRequestAsync(HidLength.Short, 0x01, _featureList["BATTERY_VOLTAGE"]).ConfigureAwait(false);

                if (requestStatus == RequestStatus.Success)
                {
                    BatteryVoltage = 0.001 * ((resData.Param(0) << 8) + resData.Param(1));
                }
                else
                {
                    BatteryVoltage = Double.NaN;
                }
            }
        }

        public string XmlData()
        {
            string output = "";
            output += "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n";
            output += "<xml>\n";
            output += $"<device_name>{DeviceName}</device_name>\n";
            output += $"<battery_voltage>{BatteryVoltage:f2}</battery_voltage>\n";
            output += $"<battery_percent>{BatteryPercentage:f2}</battery_percent>\n";
            output += "</xml>";

            return output;
        }

        public async Task Listen()
        {
            if (_listen)
            {
                return;
            }

            try
            {
                await _hidDevices[(int) HidLength.Short].InitializeAsync();
                await _hidDevices[(int) HidLength.Long].InitializeAsync();
            }
            catch (ApiException)
            {
                Debug.WriteLine("Device not connected");
            }
            finally
            {
                _listen = _hidDevices[(int)HidLength.Short].IsInitialized;
                _listen &= _hidDevices[(int)HidLength.Long].IsInitialized;
            }

            if (_shortListener?.IsAlive != true)
            {
                _shortListener = new Thread(async () => await ReadLoopAsync((int) HidLength.Short));
                _shortListener.Start();
            }

            if (_longListener?.IsAlive != true)
            {
                _longListener = new Thread(async () => await ReadLoopAsync((int) HidLength.Long));
                _longListener.Start();
            }
        }

        public async Task LoadDevice()
        {
            await GetFeaturesAsync();
            await GetDeviceNameAsync();
            await GetWPidAsync();
        }

        private async Task ReadLoopAsync(int hidNum)
        {
            while (_listen)
            {
                if (!_hidDevices[hidNum].IsInitialized)
                {
                    _hidDevices[hidNum].InitializeAsync().Wait();
                }

                try
                {
                    HidData resData = await _hidDevices[hidNum].ReadAsync();
                    _resDataQueue.Add(resData);
                }
                catch (IOException)
                {
                    _lastException = new LogiDeviceException($"Device disconnected");
                    _resDataQueue.Add(null);
                    break;
                }
            }

            _listen = false;
        }

        private async Task GetFeaturesAsync()
        {
            (RequestStatus resRequestStatus, HidData _resData) =
                await WriteReadRequestAsync(HidLength.Short, 0x01, 0x00, 0x00, LogiFeatures.GetHexCodeArray("FEATURE_SET"))
                    .ConfigureAwait(false);
            if (resRequestStatus == RequestStatus.Errored)
            {
                throw _lastException;
            }

            if (resRequestStatus == RequestStatus.TimedOut)
            {
                throw new LogiDeviceException("Device Timedout");
            }

            byte featureIndex = _resData.Param(0);

            (resRequestStatus, _resData) = await WriteReadRequestAsync(HidLength.Short, 0x01, featureIndex).ConfigureAwait(false);
            if (resRequestStatus == RequestStatus.Errored)
            {
                throw _lastException;
            }

            if (resRequestStatus== RequestStatus.TimedOut)
            {
                throw new LogiDeviceException("Device Timedout");
            }

            int numFeatures = 1 + _resData.Param(0);

            Debug.WriteLine("");
            Debug.WriteLine($"Checking device: {UsbSerialId}");
            Debug.WriteLine("Found {0:d} features.", numFeatures);

            for (byte ii = 0; ii < numFeatures; ii++)
            {
                (resRequestStatus, _resData) = await WriteReadRequestAsync(HidLength.Short, 0x01, featureIndex, 0x10, new byte[] { ii }).ConfigureAwait(false);
                if (resRequestStatus == RequestStatus.Errored)
                {
                    throw _lastException;
                }

                if (resRequestStatus == RequestStatus.TimedOut)
                {
                    throw new LogiDeviceException("Device Timedout");
                }

                byte featureIdmsb = _resData.Param(0);
                byte featureIdlsb = _resData.Param(1);
                byte featureType  = _resData.Param(2);

                string featureName = LogiFeatures.GetName(new byte[] { featureIdmsb, featureIdlsb });

                string outString = "";

                if (featureName != "UNKNOWN")
                {
                    _featureList.Add(featureName, ii);
                    _featureListR.Add(ii, featureName);
                }

                outString += $"\t{ii:d2} {featureName,-20:s}";
                outString += $"\t [0x{featureIdmsb:X2}{featureIdlsb:X2}] ";

                if ((featureType & 0b10000000) > 0)
                {
                    outString += "obsolete  ";
                }

                if ((featureType & 0b01000000) > 0)
                {
                    outString += "hidden  ";
                }

                if ((featureType & 0b00100000) > 0)
                {
                    outString += "internal  ";
                }

                Debug.WriteLine(outString);
            }
        }

        private async Task GetDeviceNameAsync()
        {
            if (!_featureList.ContainsKey("DEVICE_NAME"))
            {
                DeviceName = "NO Device Name";
            }

            byte deviceNameIdx = _featureList["DEVICE_NAME"];
            (RequestStatus resRequestStatus, HidData resData) = await WriteReadRequestAsync(HidLength.Short, 0x01, deviceNameIdx);
            if (resRequestStatus == RequestStatus.Errored)
            {
                throw _lastException;
            }

            if (resRequestStatus == RequestStatus.TimedOut)
            {
                throw new LogiDeviceException("Device Timedout");
            }

            int nameLength = resData.Param(0);
            byte[] nameBuffer = new byte[nameLength];

            for (byte ii = 0; ii < nameLength; ii += 15)
            {
                (resRequestStatus, resData) = await WriteReadRequestAsync(HidLength.Short, 0x01, deviceNameIdx, 0x10, new byte[] { ii, 00, 00 });
                if (resRequestStatus == RequestStatus.Errored)
                {
                    throw _lastException;
                }

                if (resRequestStatus == RequestStatus.TimedOut)
                {
                    throw new LogiDeviceException("Device Timedout");
                }

                Buffer.BlockCopy(resData, 4, nameBuffer, ii, Math.Min(nameLength - ii, 15));
            }

            DeviceName = Encoding.ASCII.GetString(nameBuffer);
        }

        private async Task GetWPidAsync()
        {
            try
            {
                (RequestStatus resRequestStatus, HidData resData) = await WriteReadRequestAsync(HidLength.Short, 0xFF, 0x83, 0xB5, new byte[] {0x20,});
                if (resRequestStatus == RequestStatus.Errored)
                {
                    throw _lastException;
                }

                if (resRequestStatus == RequestStatus.TimedOut)
                {
                    throw new LogiDeviceException("Device Timedout");
                }

                _wpid = (ushort) ((resData.Param(3) << 8) + resData.Param(4));
            }
            catch (LogiDeviceException e)
            {
                _wpid = (ushort) (_hidDevices[(int) HidLength.Long].ConnectedDeviceDefinition.ProductId ?? 0);
                Debug.WriteLine(e.Message);
            }
        }

        private async Task<double> GetProtocolVer()
        {
            try
            {
                (RequestStatus resRequestStatus, HidData resData) = await WriteReadRequestAsync(HidLength.Short, 0x01, 0x00, 0x10, new byte[] {0x00, 0x00, 0x88});

                if (resRequestStatus == RequestStatus.Errored)
                {
                    throw _lastException;
                }

                if (resRequestStatus == RequestStatus.TimedOut)
                {
                    throw new LogiDeviceException("Device Timedout");
                }

                string verStr = $"{resData.Param(0):X}.{resData.Param(1):X}";

                return double.Parse(verStr, NumberStyles.Any, CultureInfo.InvariantCulture);
            }
            catch (LogiDeviceException)
            {
                return 1.0;
            }
        }

        private async Task<(RequestStatus, HidData)> WriteRequestAsync(HidLength length, byte deviceId, byte featureIndex, byte functionId = 0x00, byte[] paramsBytes = null)
        {
            byte randSwid = (byte)_rand.Next(1, 16);

            if (featureIndex < 0x80)
            {
                functionId |= randSwid;
            }

            HidData senData = CreatePacket(length, deviceId, featureIndex, functionId, paramsBytes);

            DebugParse(senData, 1);

            await Listen();
            try
            {
                await _hidDevices[(int) HidLength.Short].WriteAsync(senData).ConfigureAwait(false);
            }
            catch (NotInitializedException)
            {
                return (RequestStatus.TimedOut, senData);
            }

            return (RequestStatus.Success, senData);
        }

        private async Task<(RequestStatus, HidData)> WriteReadRequestAsync(HidLength length, byte deviceId, byte featureIndex, byte functionId = 0x00, byte[] paramsBytes = null)
        {
            (RequestStatus resStatus, HidData senData) = await WriteRequestAsync(length, deviceId, featureIndex, functionId, paramsBytes);
            HidData resData = null;

            if (resStatus == RequestStatus.TimedOut)
            {
                return (resStatus, null);
            }

            while (true)
            {
                CancellationTokenSource timeoutCt = new CancellationTokenSource();
                timeoutCt.CancelAfter(HidTimeOut);

                try
                {
                    resData = _resDataQueue.Take(timeoutCt.Token);
                }
                catch (OperationCanceledException)
                {
                    resStatus = RequestStatus.TimedOut;
                    return (resStatus, resData);
                }

                if (resData == null)
                {
                    continue;
                }

                bool valid = resData.CompareSignature(senData);

                if (valid)
                {
                    DebugParse(resData, 2);
                    break;
                }
                else if ((resData.SubId == 0x8F) && (resData.Address == senData.SubId) && (resData.Param(0) == senData.Address))
                {
                    _lastException = new LogiDeviceException($"HID++ 1.0 Error (x{resData.Param(1):X2})");
                    resStatus = RequestStatus.Errored;
                    DebugParse(resData, 2);
                    break;
                }
            }

            return (resStatus, resData);
        }

        private byte[] CreatePacket(HidLength length, byte deviceId, byte featureIndex, byte functionId = 0x00, byte[] paramsBytes = null)
        {
            byte[] output;

            switch (length)
            {
                case HidLength.Short:
                    output = new byte[(int) HidLength.Short];
                    output[0] = (byte) HidLengthFlag.Short;
                    break;
                case HidLength.Long:
                    output = new byte[(int) HidLength.Long];
                    output[0] = (byte) HidLengthFlag.Long;
                    break;
                default:
                    throw new LogiDeviceException("Invalid HID packet length.");
            }

            output[1] = deviceId;
            output[2] = featureIndex;
            output[3] = functionId;

            if (paramsBytes != null)
            {
                for (int ii = 0; ii < paramsBytes.Length; ii++)
                {
                    output[4 + ii] = paramsBytes[ii];
                }
            }

            return output;
        }

        private static void DebugParse(byte[] byteArray, int mode = 0)
        {
#if DEBUG
#else
            return;
#endif

            int i;

            string outString = "";

            if (mode == 1)
            {
                outString += "W: ";
            }
            else if (mode == 2)
            {
                outString += "R: ";
            }

            for (i = 0; i < 4; i++)
            {
                outString += $"x{byteArray[i]:X2} ";
            }

            for (; i < byteArray.Length; i++)
            {
                outString += $"x{byteArray[i]:X2}";
            }

            Debug.WriteLine(outString);
        }

        #region  Helper classes

        private enum RequestStatus : byte
        {
            Success = 0,
            TimedOut = 1,
            Errored = 2
        }

        private enum HidLength : int
        {
            Short = 7,
            Long = 20
        }

        private enum HidLengthFlag : byte
        {
            Short = 0x10,
            Long = 0x11
        }

        private class HidData
        {
            private byte[] _data;

            // HID++ 2.0
            public byte FeatureIndex => _data[2];
            public byte FunctionId => (byte) (_data[3] & 0xF0);
            public byte SwId => (byte) (_data[3] & 0x0F);

            // HID++ 1.0
            public byte SubId => _data[2];
            public byte Address => _data[3];

            // Common
            public byte DeviceIndex => _data[1];
            public byte Param(int index) => _data[4 + index];

            public static implicit operator HidData(byte[] d) => new HidData() {_data = d};
            public static implicit operator HidData(ReadResult d) => new HidData() {_data = d.Data};

            public static implicit operator byte[](HidData d) => d._data;

            public bool CompareSignature(HidData other)
            {
                return (this._data[1] == other._data[1]) && (this._data[2] == other._data[2]) && (this._data[3] == other._data[3]);
            }
        }
        #endregion
    }
}