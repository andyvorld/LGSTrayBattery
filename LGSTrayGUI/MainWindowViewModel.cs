using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using LGSTrayCore;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using LGSTrayGHUB;
using LGSTrayHID;
using System.Linq;
using PropertyChanged;
using System.Windows;
using System;
using System.IO;
using System.Diagnostics;

namespace LGSTrayGUI
{
    class MainWindowViewModel : INotifyPropertyChanged
    {
        private const double BATTERY_UPDATE_PERIOD_MS = 5e3;

        private MainWindow view;

        public event PropertyChangedEventHandler PropertyChanged;

        private bool? _autoStart = null;
        public bool AutoStart
        {
            get
            {
                if (_autoStart == null)
                {
                    RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                    _autoStart = registryKey?.GetValue("LGSTrayGUI") != null;
                }

                return _autoStart ?? false;
            }
            set
            {
                RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                if (registryKey == null)
                {
                    return;
                }

                if (value)
                {
                    registryKey.SetValue("LGSTrayGUI", Path.Combine(AppContext.BaseDirectory, Process.GetCurrentProcess().MainModule.FileName));
                }
                else
                {
                    registryKey.DeleteValue("LGSTrayGUI", false);
                }

                _autoStart = value;
            }
        }

        private Thread httpThread;
        private System.Timers.Timer updateTimer;

        private ICollection<ObservableCollection<LogiDevice>> _logiDevices = new List<ObservableCollection<LogiDevice>>();
        public ICollection<ObservableCollection<LogiDevice>> LogiDevices { get { return this._logiDevices; } }

        private LogiDevice _SelectedDevice;
        public LogiDevice SelectedDevice
        {
            get
            {
                return _SelectedDevice;
            }
            set
            {
                if (_SelectedDevice != null)
                {
                    _SelectedDevice.PropertyChanged -= UpdateBatteryIcon;

                }
                _SelectedDevice = value;
                _SelectedDevice.PropertyChanged += UpdateBatteryIcon;
                UpdateBatteryIcon(value, new PropertyChangedEventArgs("LastUpdate"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedDevice)));

                Properties.Settings.Default.LastSelectedDeviceId = value.DeviceID;
                Properties.Settings.Default.Save();
            }
        }

        private void UpdateBatteryIcon(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "LastUpdate")
            {
                return;
            }

            view.TaskbarIcon.Icon = TrayIconTools.GenerateIcon(sender as LogiDevice);
        }

        public IEnumerable<LogiDevice> LogiDevicesFlat { get => LogiDevices.SelectMany(x => x); }

        private GHUBDeviceManager ghubDeviceManager;
        private HIDDeviceManager hidDeviceManager;

        public delegate void UpdateDeviceListDelegate(IEnumerable<LogiDevice> val);
        public MainWindowViewModel(MainWindow view)
        {
            this.view = view;
        }

        public async Task LoadViewModel()
        {
            ObservableCollection<LogiDevice> ghubDevices = new ObservableCollection<LogiDevice>();
            ObservableCollection<LogiDevice> hidDevices = new ObservableCollection<LogiDevice>();

            PropertyChanged += UpdateSelectedDeviceOnLaunch;

            ghubDevices.CollectionChanged += (o, e) => {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogiDevicesFlat)));
            };
            hidDevices.CollectionChanged += (o, e) => {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogiDevicesFlat)));
            };

            LogiDevices.Add(ghubDevices);
            LogiDevices.Add(hidDevices);

            updateTimer = new System.Timers.Timer();
            ghubDeviceManager = new GHUBDeviceManager(ghubDevices);
            _ = ghubDeviceManager.LoadDevicesAsync().ContinueWith(_ =>
            {
                updateTimer.Elapsed += async (s, e) => { await ghubDeviceManager.UpdateDevicesAsync(); };
            });

            hidDeviceManager = new HIDDeviceManager(hidDevices);
            _ = hidDeviceManager.LoadDevicesAsync().ContinueWith(_ =>
            {
            updateTimer.Elapsed += async (s, e) => { await hidDeviceManager.UpdateDevicesAsync(); };
            });

            updateTimer.Interval = BATTERY_UPDATE_PERIOD_MS;
            updateTimer.Start();

            HttpServer.LoadConfig();
            if (HttpServer.ServerEnabled)
            {
                httpThread = new Thread(() => HttpServer.ServeLoop(_logiDevices));
                httpThread.IsBackground = true;
                httpThread.Start();
            }
        }

        private void UpdateSelectedDeviceOnLaunch(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(LogiDevicesFlat))
            {
                return;
            }

            if (SelectedDevice != null)
            {
                return;
            }


            LogiDevice found = LogiDevicesFlat.FirstOrDefault(x => x.DeviceID == Properties.Settings.Default.LastSelectedDeviceId);
            if (found != null)
            {
                SelectedDevice = found;
            }
        }
    }
}
