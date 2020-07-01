﻿using System;
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

        private byte _randSwid;

        private readonly Random _rand = new Random();

        private readonly double _protocolVer;

        private PowerModel _powermodel;

        private ushort _wpid;

        private bool _listen = false;

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

        public async Task LoadDevice()
        {
            await GetFeaturesAsync();
            await GetDeviceNameAsync();
            await GetWPidAsync();
        }

        public async Task Listen()
        {
            await _hidDevices[7].InitializeAsync();
            await _hidDevices[20].InitializeAsync();

            //_readThread = new Thread(ReadLoop);
            //_readThread.Start();

            _listen = true;

            ReadLoopAsync(7);
            ReadLoopAsync(20);
        }

        public void StopListen()
        {
            _listen = false;
        }

        private async void ReadLoopAsync(int hidNum)
        {
            while (_listen)
            {
                try
                {
                    await _hidDevices[hidNum].InitializeAsync();
                    var resData = await _hidDevices[hidNum].ReadAsync().ConfigureAwait(false);
                    ParseReport(resData);
                }
                catch (ApiException e)
                {
                    //Debug.WriteLine(e.Message);
                    BatteryPercentage = Double.NaN;
                    BatteryVoltage = Double.NaN;
                    await Task.Delay(500);
                }
                catch (IOException e)
                {
                    BatteryPercentage = Double.NaN;
                    BatteryVoltage = Double.NaN;
                    await Task.Delay(500);
                }
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

        private async Task GetFeaturesAsync()
        {
            byte[] resData = await WriteReadRequestAsync(7, 0x01, 0x00, 0x00, LogiFeatures.GetHexCodeArray("FEATURE_SET"));
            byte featureIndex = resData[4];

            resData = await WriteReadRequestAsync(7, 0x01, featureIndex);
            int numFeatures = 1 + resData[4];

            Debug.WriteLine("Found {0:d} features.", numFeatures);

            for (byte ii = 0; ii < numFeatures; ii++)
            {
                resData = await WriteReadRequestAsync(7, 0x01, featureIndex, 0x10, new byte[] { ii });

                byte featureIdmsb = resData[4];
                byte featureIdlsb = resData[5];
                byte featureType = resData[6];

                string featureName = LogiFeatures.GetName(new byte[] { featureIdmsb, featureIdlsb });

                if (featureName != "UNKNOWN")
                {
                    _featureList.Add(featureName, ii);
                }

                Debug.Write($"\t{ii:d2} {featureName,-20:s}");
                Debug.Write($"\t [0x{featureIdmsb:X2}{featureIdlsb:X2}] ");

                if ((featureType & 0b10000000) > 0)
                {
                    Debug.Write("obsolete  ");
                }

                if ((featureType & 0b01000000) > 0)
                {
                    Debug.Write("hidden  ");
                }

                if ((featureType & 0b00100000) > 0)
                {
                    Debug.Write("internal  ");
                }

                Debug.WriteLine("");
            }
        }

        private async Task GetDeviceNameAsync()
        {
            if (!_featureList.ContainsKey("DEVICE_NAME"))
            {
                DeviceName = "NO Device Name";
            }

            byte deviceNameIdx = _featureList["DEVICE_NAME"];
            var resData = await WriteReadRequestAsync(7, 0x01, deviceNameIdx);

            int nameLength = resData[4];
            byte[] nameBuffer = new byte[nameLength];

            for (byte ii = 0; ii < nameLength; ii += 15)
            {
                resData = await WriteReadRequestAsync(7, 0x01, deviceNameIdx, 0x10, new byte[] { ii, 00, 00 });

                Buffer.BlockCopy(resData, 4, nameBuffer, ii, Math.Min(nameLength - ii, 15));
            }

            DeviceName = Encoding.ASCII.GetString(nameBuffer);
        }

        private async Task GetWPidAsync()
        {
            try
            {
                byte[] resData = await WriteReadRequestAsync(7, 0xFF, 0x83, 0xB5, new byte[] {0x20,});
                _wpid = (UInt16)(((UInt16)resData[7] << 8) + resData[8]);
            }
            catch (LogiDeviceException e)
            {
                _wpid = (ushort) (_hidDevices[20].ConnectedDeviceDefinition.ProductId ?? 0);
                Debug.WriteLine(e.Message);
            }
        }

        private async Task<double> GetProtocolVer()
        {
            try
            {
                var resData = await WriteReadRequestAsync(7, 0x01, 0x00, 0x10, new byte[] {0x00, 0x00, 0x88});

                string verStr = $"{resData[4]:X}.{resData[5]:X}";

                return double.Parse(verStr);
            }
            catch (LogiDeviceException e)
            {
                return 1.0;
            }
        }

        private async Task WriteRequestAsync(int length, byte deviceId, byte featureIndex, byte functionId = 0x00, byte[] paramsBytes = null)
        {
            _randSwid = (byte)_rand.Next(1, 16);

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

        private async Task<byte[]> WriteReadRequestAsync(int length, byte deviceId, byte featureIndex, byte functionId = 0x00, byte[] paramsBytes = null)
        {
            try
            {
                await _hidDevices[7].InitializeAsync();
                await _hidDevices[20].InitializeAsync();

                byte randSwid = (byte) _rand.Next(1, 16);

                if (featureIndex < 0x80)
                {
                    functionId |= randSwid;
                }

                byte[] request = CreatePacket(length, deviceId, featureIndex, functionId, paramsBytes);
                DebugParse(request, 1);

                await _hidDevices[7].WriteAsync(request);
                byte[] resData;

                do
                {
                    var reportReturnShort = _hidDevices[7].ReadAsync();
                    var reportReturnLong = _hidDevices[20].ReadAsync();

                    var reportReturn = await await Task.WhenAny(reportReturnLong, reportReturnShort);

                    resData = reportReturn;

                    if (resData[2] == 0x8F)
                    {
                        throw new LogiDeviceException("HID++ 1.0 Error");
                    }

                } while (featureIndex < 0x80 && (resData[3] & 0x0F) != randSwid);

                DebugParse(resData, 2);

                if (featureIndex < 0x80)
                {
                    if (resData[1] != deviceId)
                    {
                        throw new LogiDeviceException("Device ID mismatch");
                    }

                    if (resData[2] != featureIndex)
                    {
                        throw new LogiDeviceException("Feature Index mismatch");
                    }

                    if ((resData[3] & 0xF0) != (functionId & 0xF0))
                    {
                        throw new LogiDeviceException("Function ID mismatch");
                    }
                }

                return resData;
            }
            finally
            {
                _hidDevices[7].Close();
                _hidDevices[20].Close();
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

            if (mode == 1)
            {
                Debug.Write("W: ");
            }
            else if (mode == 2)
            {
                Debug.Write("R: ");
            }

            for (i = 0; i < 4; i++)
            {
                Debug.Write($"x{byteArray[i]:X2} ");
            }

            for (; i < byteArray.Length; i++)
            {
                Debug.Write($"x{byteArray[i]:X2}");
            }

            Debug.Write("\n");
        }
    }
}