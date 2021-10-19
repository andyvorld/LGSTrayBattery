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

namespace LGSTrayHID
{
    public class HIDDeviceManager : LogiDeviceManager
    {
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
            Task[] taskQueue = new Task[deviceDefinitions.Count()];
            for (int i = 0; i < deviceDefinitions.Count(); i++)
            {
                var device = await factories.GetDeviceAsync(deviceDefinitions[i]);
                taskQueue[i] = Task.Run(async () =>
                    {
                        try
                        {
                            await device.InitializeAsync();
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

            await Task.WhenAll(taskQueue);
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
