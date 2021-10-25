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
        private const uint LOGITECH_VENDOR_ID = 0x046D;

        private DeviceListener _deviceListener;
        private HashSet<LogiDeviceHandler> logiDeviceHandlers = new HashSet<LogiDeviceHandler>();

        public HIDDeviceManager(ICollection<LogiDevice> logiDevices) : base(logiDevices)
        {
        }
        public override async Task LoadDevicesAsync()
        {
            var legacyHid = new FilterDeviceDefinition(vendorId: LOGITECH_VENDOR_ID, usagePage: 0xFF00)
                            .CreateWindowsHidDeviceFactory();

            var modernHid = new FilterDeviceDefinition(vendorId: LOGITECH_VENDOR_ID, usagePage: 0xFF43)
                            .CreateWindowsHidDeviceFactory();

            var factories = legacyHid.Aggregate(modernHid);

            var deviceDefinitions = (await factories.GetConnectedDeviceDefinitionsAsync().ConfigureAwait(false)).ToList();

            _LogiDevices.Clear();

            _deviceListener?.Dispose();
            _deviceListener = new DeviceListener(factories, 1000, null);
            _deviceListener.DeviceInitialized += _deviceListener_DeviceInitialized;
            _deviceListener.DeviceDisconnected += _deviceListener_DeviceDisconnected;

            _deviceListener.Start();
        }

        public override async Task UpdateDevicesAsync()
        {
            List<Task> tasks = new();
            foreach (var device in logiDeviceHandlers)
            {
                tasks.Add(device.UpdateBattery());
            }

            await Task.WhenAll(tasks);
        }

        #region PrivateMethods
        private async void _deviceListener_DeviceInitialized(object sender, DeviceEventArgs e)
        {
            var deviceDef = e.Device.ConnectedDeviceDefinition;
            if (deviceDef.WriteBufferSize != 20 || deviceDef.ReadBufferSize != 20)
            {
                e.Device.Close();
                Debug.WriteLine("Device is not HID++ Long, ignoring...");
                return;
            }

            Debug.WriteLine($"{deviceDef.DeviceId} has connected");

            var tmp = await LogiDeviceHandler.CreateNewHandler(e.Device);
            if (tmp == null)
            {
                e.Device.Close();
                Debug.WriteLine($"{deviceDef.DeviceId} invalid protocol version, ignoring...");
                return;
            }

            Debug.WriteLine($"{tmp.DeviceName} has initialized");
            logiDeviceHandlers.Add(tmp);
            tmp.StartRead();
            _LogiDevices.Add(tmp.GetLogiDeviceHID());
        }

        private void _deviceListener_DeviceDisconnected(object sender, DeviceEventArgs e)
        {
            Debug.WriteLine($"{e.Device.ConnectedDeviceDefinition.DeviceId} has disconnected");

            logiDeviceHandlers.RemoveWhere(x => x.HIDDeviceId == e.Device.DeviceId);
        }
        #endregion
    }
}
