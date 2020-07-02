using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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

        public string UsbSerialId { get; set; }

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

        private byte _subId;
        private byte _randSwid;

        private readonly Random _rand = new Random();

        private readonly double _protocolVer;

        private PowerModel _powermodel;

        private ushort _wpid;

        private bool _listen = false;

        private AutoResetEvent _readSync = new AutoResetEvent(false);
        private ResData _resData;

        private Task _shortListener;
        private Task _longListener;

        public LogiDevice(IEnumerable<IDevice> devices, string usbSerialId, out bool valid)
        {
            valid = false;

            this.UsbSerialId = usbSerialId;

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

            if (_hidDevices.ContainsKey(7) && _hidDevices.ContainsKey(20))
            {
                _protocolVer = GetProtocolVer();

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

            StopListen();
        }

        public async Task UpdateBatteryPercentage()
        {
            try
            {
                if (_featureList.ContainsKey("BATTERY_STATUS"))
                {
                    await WriteRequestAsync(7, 0x01, _featureList["BATTERY_STATUS"]).ConfigureAwait(false);
                }
                else
                {
                    _ = UpdateBatteryVoltage();
                }
            }
            catch (ApiException)
            {
                BatteryPercentage = Double.NaN;
                BatteryVoltage = Double.NaN;
            }
        }

        private async Task UpdateBatteryVoltage()
        {
            if (_featureList.ContainsKey("BATTERY_VOLTAGE"))
            {
                await WriteRequestAsync(7, 0x01, _featureList["BATTERY_VOLTAGE"]).ConfigureAwait(false);
            }
        }

        public void LoadDevice()
        {
            Listen();
            GetFeaturesAsync();
            GetDeviceNameAsync();
            GetWPidAsync();
            StopListen();
        }

        public void Listen()
        {
            if (_listen)
            {
                return;
            }

            _hidDevices[(int) HidLength.Short].InitializeAsync().Wait();
            _hidDevices[(int) HidLength.Long].InitializeAsync().Wait();

            _listen = true;

            if (_shortListener == null)
            {
                _shortListener = ReadLoopAsync(7);
            }

            if (_longListener == null)
            {
                _longListener = ReadLoopAsync(20);
            }
        }

        public void StopListen()
        {
            _listen = false;
        }

        private async Task ReadLoopAsync(int hidNum)
        {
            while (_listen)
            {
                _hidDevices[hidNum].InitializeAsync().Wait();
                _resData = await _hidDevices[hidNum].ReadAsync();

                if (_resData.SubId != _subId )
                {
                    continue;
                }

                DebugParse(_resData, 2);
                _readSync.Set();
            }

            switch (hidNum)
            {
                case 7:
                    _shortListener = null;
                    break;
                case 20:
                    _longListener = null;
                    break;
            }
        }

        private void ParseReport(byte[] resData)
        {
            byte deviceId = resData[1];
            byte functionIdx = resData[2];

            DebugParse(resData, 2);

            // Magic disconnect string
            if (deviceId == 0x01 & functionIdx == 0x8F)
            {
                if (_featureList.ContainsValue(resData[3]))
                {
                    Debug.WriteLine("Device disconnected");
                    BatteryVoltage = Double.NaN;
                }
            }

            if ((resData[3] & 0x0F) != _randSwid)
            {
                Debug.WriteLine("^^^Not our SWID^^^");
                return;
            }
            else
            {
                _randSwid = 0xFF;
            }

            if ((_featureList.ContainsKey("BATTERY_VOLTAGE")) && (functionIdx == _featureList["BATTERY_VOLTAGE"]))
            {
                BatteryVoltage = 0.001 * ((resData[4] << 8) + resData[5]);
                Debug.WriteLine($"Battery Updated V = {BatteryVoltage}");
            }
            else if ((_featureList.ContainsKey("BATTERY_STATUS")) && (functionIdx == _featureList["BATTERY_STATUS"]))
            {
                BatteryPercentage = resData[4];
                Debug.WriteLine($"Battery Percentage Updated, {BatteryPercentage:f2}%");
            }
        }

        private void GetFeaturesAsync()
        {
            _ = WriteRequestAsync(7, 0x01, 0x00, 0x00, LogiFeatures.GetHexCodeArray("FEATURE_SET"));
            _readSync.WaitOne();

            byte featureIndex = _resData.Param(0);

            _ = WriteRequestAsync(7, 0x01, featureIndex);
            _readSync.WaitOne();

            int numFeatures = 1 + _resData.Param(0);

            Debug.WriteLine("Found {0:d} features.", numFeatures);

            for (byte ii = 0; ii < numFeatures; ii++)
            {
                _ = WriteRequestAsync(7, 0x01, featureIndex, 0x10, new byte[] { ii });
                _readSync.WaitOne();

                byte featureIdmsb = _resData.Param(0);
                byte featureIdlsb = _resData.Param(1);
                byte featureType  = _resData.Param(2);

                string featureName = LogiFeatures.GetName(new byte[] { featureIdmsb, featureIdlsb });

                string outString = "";

                if (featureName != "UNKNOWN")
                {
                    _featureList.Add(featureName, ii);
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

        private void GetDeviceNameAsync()
        {
            if (!_featureList.ContainsKey("DEVICE_NAME"))
            {
                DeviceName = "NO Device Name";
            }

            byte deviceNameIdx = _featureList["DEVICE_NAME"];
            _ = WriteRequestAsync(7, 0x01, deviceNameIdx);
            _readSync.WaitOne();

            int nameLength = _resData.Param(0);
            byte[] nameBuffer = new byte[nameLength];

            for (byte ii = 0; ii < nameLength; ii += 15)
            {
                _ = WriteRequestAsync(7, 0x01, deviceNameIdx, 0x10, new byte[] { ii, 00, 00 });
                _readSync.WaitOne();

                Buffer.BlockCopy(_resData, 4, nameBuffer, ii, Math.Min(nameLength - ii, 15));
            }

            DeviceName = Encoding.ASCII.GetString(nameBuffer);
        }

        private void GetWPidAsync()
        {
            try
            {
                _ = WriteRequestAsync(7, 0xFF, 0x83, 0xB5, new byte[] {0x20,});
                _readSync.WaitOne();
                _wpid = (UInt16)(((UInt16) _resData.Param(3) << 8) + _resData.Param(4));
            }
            catch (LogiDeviceException e)
            {
                _wpid = (ushort) (_hidDevices[20].ConnectedDeviceDefinition.ProductId ?? 0);
                Debug.WriteLine(e.Message);
            }
        }

        private double GetProtocolVer()
        {
            try
            {
                Listen();
                _ = WriteRequestAsync(7, 0x01, 0x00, 0x10, new byte[] {0x00, 0x00, 0x88});
                _readSync.WaitOne();

                string verStr = $"{_resData.Param(0):X}.{_resData.Param(1):X}";

                return double.Parse(verStr);
            }
            catch (LogiDeviceException e)
            {
                return 1.0;
            }
            finally
            {
                StopListen();
            }
        }

        private async Task WriteRequestAsync(int length, byte deviceId, byte featureIndex, byte functionId = 0x00, byte[] paramsBytes = null)
        {
            _randSwid = (byte)_rand.Next(1, 16);
            _subId = featureIndex;

            if (featureIndex < 0x80)
            {
                functionId |= _randSwid;
            }

            byte[] request = CreatePacket(length, deviceId, featureIndex, functionId, paramsBytes);
            DebugParse(request, 1);

            try
            {
                await _hidDevices[7].InitializeAsync();
                await _hidDevices[7].WriteAsync(request);
            }
            catch (ApiException e)
            {
                BatteryPercentage = Double.NaN;
                BatteryVoltage = Double.NaN;
            }
        }

        private byte[] CreatePacket(int length, byte deviceId, byte featureIndex, byte functionId = 0x00, byte[] paramsBytes = null)
        {
            byte[] output;

            switch (length)
            {
                case 7:
                    output = new byte[7];
                    output[0] = 0x10;
                    break;
                case 20:
                    output = new byte[20];
                    output[0] = 0x11;
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

        private struct ResData
        {
            private byte[] _resData;

            // HID++ 2.0
            public byte FeatureIndex => _resData[2];
            public byte FunctionId => (byte) (_resData[3] & 0xF0);
            public byte SwId => (byte) (_resData[3] & 0x0F);

            // HID++ 1.0
            public byte SubId => _resData[2];
            public byte Address => _resData[3];

            // Common
            public byte DeviceIndex => _resData[1];
            public byte Param(int index) => _resData[4 + index];

            public static implicit operator ResData(byte[] d) => new ResData() {_resData = d};
            public static implicit operator ResData(ReadResult d) => new ResData() {_resData = d.Data};

            public static implicit operator byte[](ResData d) => d._resData;
        }
        #endregion
    }
}