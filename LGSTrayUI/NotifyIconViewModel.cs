using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hardcodet.Wpf.TaskbarNotification;
using LGSTrayCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace LGSTrayUI
{
    public partial class NotifyIconViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<LogiDevice> _logiDevices = LogiDeviceCollection.Instance.Devices;

        private readonly Dictionary<string, LogiDeviceIcon> _taskbarIcons = new();

        [RelayCommand]
        private static void ExitApplication()
        {
            Environment.Exit(0);
        }

        [RelayCommand]
        private void DeviceClicked(object? sender)
        {
            if (sender is not MenuItem menuItem)
            {
                return;
            }

            LogiDevice logiDevice = (LogiDevice) menuItem.DataContext;

            var ret = _taskbarIcons.TryGetValue(logiDevice.DeviceId, out var taskbarIcon);
            if (menuItem.IsChecked && !ret)
            {
                taskbarIcon = new(logiDevice);
                _taskbarIcons[logiDevice.DeviceId] = taskbarIcon;
            }
            else if (!menuItem.IsChecked && ret)
            {
                taskbarIcon!.Dispose();
                _taskbarIcons.Remove(logiDevice.DeviceId);
            }
        }
    }
}
