﻿using System.ComponentModel;
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

namespace LGSTrayGUI
{
    class MainWindowViewModel : INotifyPropertyChanged
    {
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
                    registryKey.SetValue("LGSTrayGUI", Assembly.GetEntryAssembly().Location);
                }
                else
                {
                    registryKey.DeleteValue("LGSTrayGUI", false);
                }

                _autoStart = value;
            }
        }

        private Thread httpThread;
        public Thread UpdateThread;
        private CancellationTokenSource _ctTimerSource;

        private ObservableCollection<ObservableCollection<LogiDevice>> _logiDevices = new ObservableCollection<ObservableCollection<LogiDevice>>();
        public ObservableCollection<ObservableCollection<LogiDevice>> LogiDevices { get { return this._logiDevices; } }

        public IEnumerable<LogiDevice> LogiDevicesFlat { get => LogiDevices.SelectMany(x => x); }

        private GHUBDeviceManager ghubDeviceManager;
        private HIDDeviceManager hidDeviceManager;

        public delegate void UpdateDeviceListDelegate(IEnumerable<LogiDevice> val);
        public MainWindowViewModel()
        {
        }

        public async Task LoadViewModel()
        {
            ObservableCollection<LogiDevice> ghubDevices = new ObservableCollection<LogiDevice>();
            ObservableCollection<LogiDevice> hidDevices = new ObservableCollection<LogiDevice>();

            ghubDevices.CollectionChanged += (o, e) => {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LogiDevicesFlat"));
            };
            hidDevices.CollectionChanged += (o, e) => {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LogiDevicesFlat"));
            };

            LogiDevices.Add(ghubDevices);
            LogiDevices.Add(hidDevices);

            ghubDeviceManager = new GHUBDeviceManager(ghubDevices);
            var t1 = ghubDeviceManager.LoadDevicesAsync();

            hidDeviceManager = new HIDDeviceManager(hidDevices);
            var t2 = hidDeviceManager.LoadDevicesAsync();

            await Task.WhenAll(t1, t2);

            //updateDeviceList(LogiDevicesFlat);

            HttpServer.LoadConfig();
            if (HttpServer.ServerEnabled)
            {
                httpThread = new Thread(() => HttpServer.ServeLoop(_logiDevices));
                httpThread.IsBackground = true;
                httpThread.Start();
            }
        }
    }
}