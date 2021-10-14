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
using Microsoft.Extensions.Logging;

namespace LGSTrayHID
{
    public class HIDDeviceManager : LogiDeviceManager
    {
        public HIDDeviceManager(ICollection<LogiDevice> logiDevices) : base(logiDevices)
        {
        }
        public override async Task LoadDevicesAsync()
        {
            var logger = new DebugLogger();
            var tracer = new DebugTracer();

            //Register the factory for creating Usb devices. This only needs to be done once.
            WindowsHidDeviceFactory.Register(logger, tracer);

            //Define the types of devices to search for. This particular device can be connected to via USB, or Hid
            var deviceDefinitions = new List<FilterDeviceDefinition>
            {
                new FilterDeviceDefinition {DeviceType = Device.Net.DeviceType.Hid, VendorId = 0x046D},
            };

            //Get the first available device and connect to it
            var devices = await DeviceManager.Current.GetDevicesAsync(deviceDefinitions).ConfigureAwait(false);

            _LogiDevices.Clear();
            Task[] taskQueue = new Task[devices.Count];
            for (int i = 0; i < devices.Count; i++)
            {
                var device = devices[i];
                taskQueue[i] = Task.Run(async () =>
                    {
                        var t = i;
                        try
                        {
                            device.InitializeAsync().Wait();
                        }
                        catch (Exception)
                        {
                            return;
                        }

                        if (device.ConnectedDeviceDefinition.ReadBufferSize == 20 && device.ConnectedDeviceDefinition.WriteBufferSize == 20)
                        {
                            LogiDeviceHID tmp = new LogiDeviceHID()
                            {
                                _hidDevice = device
                            };

                            if (await tmp.InitializeDeviceAsync())
                            {
                                _LogiDevices.Add(tmp);
                            }
                        }
                    }
                );
            }

            Task.WhenAll(taskQueue);
            await UpdateDevicesAsync();
        }

        public override async Task UpdateDevicesAsync()
        {
            Task[] taskQueue = new Task[_LogiDevices.Count()];

            foreach (var it in _LogiDevices.Select((x, i) => new { index = i, item = x }))
            {
                var device = it.item;
                taskQueue[it.index] = (device as LogiDeviceHID).UpdateBattery();
            }
            await Task.WhenAll(taskQueue);
        }
    }
}
