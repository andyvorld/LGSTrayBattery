using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LGSTrayCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace LGSTrayUI
{
    public partial class NotifyIconViewModel : ObservableObject, IHostedService
    {
        private readonly MainTaskbarIconWrapper _mainTaskbarIconWrapper;
        
        [ObservableProperty]
        private ObservableCollection<LogiDeviceViewModel> _logiDevices;

        private readonly UserSettingsWrapper _userSettings;
        public bool NumericDisplay
        {
            get
            {
                return _userSettings.NumericDisplay;
            }

            set
            {
                _userSettings.NumericDisplay = value;
                OnPropertyChanged();
            }
        }

        public static string AssemblyVersion
        {
            get
            {
                return "v" + Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Split('+')[0] ?? "Missing";
            }
        }

        private readonly Dictionary<string, LogiDeviceIcon> _taskbarIcons = new();

        public NotifyIconViewModel(MainTaskbarIconWrapper mainTaskbarIconWrapper, ILogiDeviceCollection logiDeviceCollection, UserSettingsWrapper userSettings)
        {
            _mainTaskbarIconWrapper = mainTaskbarIconWrapper;
            ((ContextMenu)Application.Current.FindResource("SysTrayMenu")).DataContext = this;

            _logiDevices = (logiDeviceCollection as LogiDeviceCollection)!.Devices;
            _userSettings = userSettings;
        }

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

            if (menuItem.IsChecked)
            {
                _userSettings.AddDevice(logiDevice.DeviceId);
            }
            else
            {
                _userSettings.RemoveDevice(logiDevice.DeviceId);
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _mainTaskbarIconWrapper.Dispose();
            return Task.CompletedTask;
        }
    }
}
