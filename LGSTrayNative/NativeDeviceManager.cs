using LGSTrayCore;
using LGSTrayNative;
using System.Runtime.InteropServices;

namespace LGSTrayGHUB
{
    public class NativeDeviceManager : LogiDeviceManager
    {
        private delegate void BatteryUpdateCallback(string dev_id, int bat_percent, bool charging, double mileage, int bat_mv);
        private delegate void DeviceReadyCallback(string dev_id, int dev_type, string dev_name);

        private readonly BatteryUpdateCallback batteryUpdateCallback;
        private readonly DeviceReadyCallback deviceReadyCallback;

        public NativeDeviceManager(ICollection<LogiDevice> logiDevices) : base(logiDevices)
        {
            batteryUpdateCallback = new(battery_update_cb);
            deviceReadyCallback = new(device_ready_cb);
        }

        public override Task LoadDevicesAsync()
        {
            LGSTrayNative_bridge.Register_battery_update_cb(Marshal.GetFunctionPointerForDelegate(batteryUpdateCallback));
            LGSTrayNative_bridge.Register_device_ready_cb(Marshal.GetFunctionPointerForDelegate(deviceReadyCallback));

            LGSTrayNative_bridge.Load_devices();

            return Task.CompletedTask;
        }

        public override Task UpdateDevicesAsync()
        {
            foreach (var dev in _LogiDevices)
            {
                LGSTrayNative_bridge.Update_device_battery(dev.DeviceID);
            }

            return Task.CompletedTask;
        }

        private void device_ready_cb(string dev_id, int dev_type, string dev_name)
        {
            if (_LogiDevices.Where(x => x.DeviceID == dev_id).Any())
            {
                return;
            }

            _LogiDevices.Add(new LogiDeviceNative
            {
                DeviceID = dev_id,
                DeviceType = (DeviceType) dev_type,
                DeviceName = dev_name
            });

            return;
        }

        private void battery_update_cb(string dev_id, int bat_percent, bool charging, double mileage, int bat_mv)
        {
            LogiDeviceNative? dev = _LogiDevices.FirstOrDefault(x => x.DeviceID == dev_id) as LogiDeviceNative;
            if (dev == null)
            {
                return;
            }

            dev.BatteryPercentage = bat_percent;
            dev.Charging = charging;
            dev.BatteryVoltage = bat_mv * 1e-3;

            return;
        }
    }
}
