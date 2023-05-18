﻿using CommunityToolkit.Mvvm.ComponentModel;
using Hardcodet.Wpf.TaskbarNotification;
using LGSTrayCore;
using System;

namespace LGSTrayUI
{
    public class LogiDeviceViewModelFactory
    {
        private readonly UserSettingsWrapper _userSettings;

        public LogiDeviceViewModelFactory(UserSettingsWrapper userSettings)
        {
            _userSettings = userSettings;
        }

        public LogiDeviceViewModel CreateViewModel(Action<LogiDeviceViewModel>? config = null)
        {
            LogiDeviceViewModel output = new(_userSettings);
            config?.Invoke(output);

            return output;
        }
    }

    public partial class LogiDeviceViewModel : LogiDevice
    {
        private readonly UserSettingsWrapper _userSettings;

        [ObservableProperty]
        private bool _isChecked = false;

        private LogiDeviceIcon? taskbarIcon;

        public LogiDeviceViewModel(UserSettingsWrapper userSettings)
        {
            _userSettings = userSettings;
        }

        partial void OnIsCheckedChanged(bool oldValue, bool newValue)
        {
            if (newValue)
            {
                taskbarIcon ??= new(this, _userSettings);
            }
            else
            {
                taskbarIcon?.Dispose();
                taskbarIcon = null;
            }
        }
    }
}
