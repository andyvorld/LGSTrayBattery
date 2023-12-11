using CommunityToolkit.Mvvm.ComponentModel;
using Hardcodet.Wpf.TaskbarNotification;
using LGSTrayCore;
using LGSTrayPrimitives.MessageStructs;
using System;

namespace LGSTrayUI
{
    public class LogiDeviceViewModelFactory
    {
        private readonly LogiDeviceIconFactory _logiDeviceIconFactory;

        public LogiDeviceViewModelFactory(LogiDeviceIconFactory logiDeviceIconFactory)
        {
            _logiDeviceIconFactory = logiDeviceIconFactory;
        }

        public LogiDeviceViewModel CreateViewModel(Action<LogiDeviceViewModel>? config = null)
        {
            LogiDeviceViewModel output = new(_logiDeviceIconFactory);
            config?.Invoke(output);

            return output;
        }
    }

    public partial class LogiDeviceViewModel : LogiDevice
    {
        private readonly LogiDeviceIconFactory _logiDeviceIconFactory;

        [ObservableProperty]
        private bool _isChecked = false;

        private LogiDeviceIcon? taskbarIcon;

        public LogiDeviceViewModel(LogiDeviceIconFactory logiDeviceIconFactory)
        {
            _logiDeviceIconFactory = logiDeviceIconFactory;
        }

        partial void OnIsCheckedChanged(bool oldValue, bool newValue)
        {
            if (newValue)
            {
                taskbarIcon ??= _logiDeviceIconFactory.CreateDeviceIcon(this);
            }
            else
            {
                taskbarIcon?.Dispose();
                taskbarIcon = null;
            }
        }

        public void UpdateState(InitMessage initMessage)
        {
            if (string.IsNullOrEmpty(DeviceId) || DeviceId == NOT_FOUND)
            {
                DeviceId = initMessage.deviceId;
            }

            DeviceName = initMessage.deviceName;
            HasBattery = initMessage.hasBattery;
            DeviceType = initMessage.deviceType;
        }

        public void UpdateState(UpdateMessage updateMessage)
        {
            BatteryPercentage = updateMessage.batteryPercentage;
            PowerSupplyStatus = updateMessage.powerSupplyStatus;
            BatteryVoltage = updateMessage.batteryMVolt / 1000.0;
            BatteryMileage = updateMessage.Mileage;
            LastUpdate = updateMessage.updateTime;
        }
    }
}
