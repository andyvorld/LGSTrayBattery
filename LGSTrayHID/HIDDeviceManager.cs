using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LGSTrayCore;
using Device.Net;
using Hid.Net;
using Hid.Net.Windows;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace LGSTrayHID
{
    public class HIDDeviceManager : LogiDeviceManager
    {
        private DeviceListener _deviceListener;
        private readonly Dictionary<IDevice, HIDPPProfile> _hidppMap = new Dictionary<IDevice, HIDPPProfile>();
        private readonly Dictionary<string, LogiDeviceHID> _logiDeviceMap = new Dictionary<string, LogiDeviceHID>();

        public HIDDeviceManager(ICollection<LogiDevice> logiDevices) : base(logiDevices)
        {
        }
        public override async Task LoadDevicesAsync()
        {
            var legacyHid = new FilterDeviceDefinition(vendorId: 0x046D, usagePage: 0xFF00)
                            .CreateWindowsHidDeviceFactory();

            var modernHid = new FilterDeviceDefinition(vendorId: 0x046D, usagePage: 0xFF43)
                            .CreateWindowsHidDeviceFactory();

            var factories = legacyHid.Aggregate(modernHid);

            var deviceDefinitions = (await factories.GetConnectedDeviceDefinitionsAsync().ConfigureAwait(false)).ToList();

            _LogiDevices.Clear();

            _deviceListener = new DeviceListener(factories, 1000, null);
            _deviceListener.DeviceInitialized += _deviceListener_DeviceInitialized;
            _deviceListener.DeviceDisconnected += _deviceListener_DeviceDisconnected;

            _deviceListener.Start();

            _deviceListener.Stop();
        }

        public override async Task UpdateDevicesAsync()
        {
            Task[] taskQueue = new Task[_hidppMap.Keys.Count()];

            foreach (var it in _hidppMap.Keys.Select((x, i) => new { index = i, item = x }))
            {
                var device = it.item;
                var hidppProfile = _hidppMap[device];
                taskQueue[it.index] = Task.Run(async () => {
                    if (!device.IsInitialized)
                    {
                        return;
                    }

                    double ret = await HIDMsg.UpdateBattery(device, hidppProfile);

                    var logiDevice = _logiDeviceMap[hidppProfile.deviceName];
                    if (hidppProfile.batteryStatusIdx != 0)
                    {
                        logiDevice.BatteryPercentage = ret;
                    }
                    if (hidppProfile.batteryVoltageIdx != 0)
                    {
                        logiDevice.BatteryVoltage = ret;
                    }
                });
            }
            await Task.WhenAll(taskQueue);
        }

        #region PrivateMethods
        private async void _deviceListener_DeviceInitialized(object sender, DeviceEventArgs e)
        {
            var deviceDef = e.Device.ConnectedDeviceDefinition;
            if (deviceDef.WriteBufferSize != 20 || deviceDef.ReadBufferSize != 20)
            {
                e.Device.Dispose();
                Debug.WriteLine("Device is not HID++ Long, ignoring...");
                return;
            }

            Debug.WriteLine($"{deviceDef.DeviceId} has connected");

            HIDPPProfile? hidppProfile;
            try
            {
                hidppProfile = await HIDMsg.InitializeHIDPPAsync(e.Device);

            }
            catch (Exception)
            {
                Debug.WriteLine("Failed to initialize HID++ device, ignoring...");
                return;
            }

            if (hidppProfile == null)
            {
                e.Device.Close();
                Debug.WriteLine("Device is not HID++ 2.0, ignoring...");
                return;
            }

            Debug.WriteLine($"{hidppProfile?.deviceName} has initialized");
            _hidppMap[e.Device] = (HIDPPProfile) hidppProfile;
            
            if (!_logiDeviceMap.ContainsKey(hidppProfile?.deviceName))
            {
                var logiDevice = new LogiDeviceHID()
                {
                    DeviceName = hidppProfile?.deviceName,
                    DeviceID = deviceDef.DeviceId.GetHashCode().ToString(),
                    DeviceType = hidppProfile?.deviceType ?? LGSTrayCore.DeviceType.Mouse
                };

                _logiDeviceMap[hidppProfile?.deviceName] = logiDevice;
                _LogiDevices.Add(logiDevice);
            }
        }

        private void _deviceListener_DeviceDisconnected(object sender, DeviceEventArgs e)
        {
            Debug.WriteLine($"{e.Device.ConnectedDeviceDefinition.DeviceId} has disconnected");

            if (_hidppMap.ContainsKey(e.Device))
            {
                _hidppMap.Remove(e.Device);
            }
        }
        #endregion
    }
}
