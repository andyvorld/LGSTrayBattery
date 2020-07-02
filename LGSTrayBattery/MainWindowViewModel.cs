using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Device.Net;
using Hid.Net.Windows;

namespace LGSTrayBattery
{
    class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public List<LogiDevice> LogiDevices {get; private set;}
        public List<PollInterval> PollIntervals { get; set; }

        private PollInterval _seletedPollInterval;

        public PollInterval SelectedPollInterval
        {
            get => _seletedPollInterval;
            private set
            {
                _seletedPollInterval = value;
                Properties.Settings.Default.LastPollIdx = PollIntervals.FindIndex(x => x == value);
                Properties.Settings.Default.Save();
            }
        }

        private LogiDevice _selectedDevice;

        public LogiDevice SelectedDevice
        {
            get => _selectedDevice;
            private set
            {
                UpdateThread?.Abort();
                UpdateThread = new Thread(UpdateSelectedBattery);
                UpdateThread.Start();

                _selectedDevice = value;

                Properties.Settings.Default.LastUSBSerial = _selectedDevice.UsbSerialId;
                Properties.Settings.Default.Save();
            }
        }

        public Thread UpdateThread;

        public MainWindowViewModel()
        {
            PollIntervals = new List<PollInterval>()
            {
                new PollInterval(1000, "1 second"),
                new PollInterval(10000, "10 seconds"),
                new PollInterval(60*1000, "1 minute"),
                new PollInterval(5*60*1000, "5 minute"),
                new PollInterval(10*60*1000, "15 minute")
            };
        }

        public async Task LoadViewModel()
        {
            var logger = new DebugLogger();
            var tracer = new DebugTracer();

            LogiFeatures.LoadConfig();

            //Register the factory for creating Usb devices. This only needs to be done once.
            WindowsHidDeviceFactory.Register(logger, tracer);

            //Define the types of devices to search for. This particular device can be connected to via USB, or Hid
            var deviceDefinitions = new List<FilterDeviceDefinition>
            {
                new FilterDeviceDefinition {DeviceType = DeviceType.Hid, VendorId = 0x046D},
            };

            //Get the first available device and connect to it
            var devices = await DeviceManager.Current.GetDevicesAsync(deviceDefinitions).ConfigureAwait(false);

            var usbSerialRegex = new Regex(@"#.+?#(.+)&");

            Dictionary<String, List<IDevice>> hidDeviceGroups = new Dictionary<string, List<IDevice>>();

            foreach (var device in devices)
            {
                string usbSerial = usbSerialRegex.Match(device.DeviceId).Groups[1].ToString();

                //Debug.WriteLine(device.DeviceId);
                //Debug.WriteLine(usbSerial);

                if (hidDeviceGroups.ContainsKey(usbSerial))
                {
                    hidDeviceGroups[usbSerial].Add(device);
                }
                else
                {
                    hidDeviceGroups.Add(usbSerial, new List<IDevice>() {device});
                }
            }

            var temp = new List<LogiDevice>();
            foreach (var usbGroup in hidDeviceGroups)
            {
                LogiDevice logiDevice = new LogiDevice(usbGroup.Value, usbGroup.Key, out var valid);

                if (valid)
                {
                    temp.Add(logiDevice);
                    logiDevice.LoadDevice();
                }
                else
                {
                    //Console.WriteLine("Invalid logitech composite usb.");
                }
            }

            LogiDevices = temp;
        }

        public void LoadLastSelected()
        {
            string lastUsbSerial = Properties.Settings.Default.LastUSBSerial;
            LogiDevice lastDevice = LogiDevices.FirstOrDefault(x => x.UsbSerialId == lastUsbSerial);

            int lastPollIdx = Properties.Settings.Default.LastPollIdx;
            UpdateSelectedPollInterval(PollIntervals[lastPollIdx]);

            if (lastDevice == null)
            {
                return;
            }

            UpdateSelectedDevice(lastDevice);
        }

        public void UpdateSelectedDevice(LogiDevice selectedDevice)
        {
            foreach (var device in LogiDevices)
            {
                device.IsChecked = false;
                device.StopListen();
            }

            SelectedDevice = selectedDevice;
            SelectedDevice.IsChecked = true;
            SelectedDevice.Listen();
            _ = SelectedDevice.UpdateBatteryPercentage();
        }

        public void UpdateSelectedPollInterval(PollInterval selectedpollInterval)
        {
            foreach (var pollInterval in PollIntervals)
            {
                pollInterval.IsChecked = false;
            }

            SelectedPollInterval = selectedpollInterval;
            SelectedPollInterval.IsChecked = true;
        }

        private async void UpdateSelectedBattery()
        {
            while (true)
            {
                if (SelectedDevice == null)
                {
                    return;
                }

                await SelectedDevice.UpdateBatteryPercentage().ConfigureAwait(false);
                await Task.Delay(SelectedPollInterval.delayTime).ConfigureAwait(false);
            }
        }
    }
}
