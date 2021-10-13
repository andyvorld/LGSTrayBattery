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
            var hidFactory = new FilterDeviceDefinition(vendorId: 0x046D).CreateWindowsHidDeviceFactory();
            var deviceDefinitions = (await hidFactory.GetConnectedDeviceDefinitionsAsync().ConfigureAwait(false));

            _LogiDevices.Clear();
            foreach (var device in deviceDefinitions.Where(x => x.WriteBufferSize == 20 && x.ReadBufferSize == 20))
            {
                var hidDevice = await hidFactory.GetDeviceAsync(device);
                await hidDevice.InitializeAsync();
                LogiDeviceHID tmp = new LogiDeviceHID()
                {
                    _hidDevice = hidDevice as HidDevice
                };

                if (tmp.InitializeDevice())
                {
                    _LogiDevices.Add(tmp);
                }
            }
        }

        public override Task UpdateDevicesAsync()
        {
            throw new NotImplementedException();
        }
    }
}
