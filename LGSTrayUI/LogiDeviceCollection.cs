using LGSTrayCore;
using LGSTrayPrimitives.MessageStructs;
using MessagePipe;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LGSTrayUI
{
    public class LogiDeviceCollection : ILogiDeviceCollection
    {
        private readonly UserSettingsWrapper _userSettings;
        private readonly LogiDeviceViewModelFactory _logiDeviceViewModelFactory;
        private readonly ISubscriber<IPCMessage> _subscriber;

        public ObservableCollection<LogiDeviceViewModel> Devices { get; } = new();
        public IEnumerable<LogiDevice> GetDevices() => Devices;

        public LogiDeviceCollection(
            UserSettingsWrapper userSettings,
            LogiDeviceViewModelFactory logiDeviceViewModelFactory,
            ISubscriber<IPCMessage> subscriber
        )
        {
            _userSettings = userSettings;
            _logiDeviceViewModelFactory = logiDeviceViewModelFactory;
            _subscriber = subscriber;

            _subscriber.Subscribe(x =>
            {
                if (x is InitMessage initMessage)
                {
                    OnInitMessage(initMessage);
                }
                else if (x is UpdateMessage updateMessage)
                {
                    OnUpdateMessage(updateMessage);
                }
            });

            LoadPreviouslySelectedDevices();
        }

        private void LoadPreviouslySelectedDevices()
        {
            foreach (var deviceId in _userSettings.SelectedDevices)
            {
                if (string.IsNullOrEmpty(deviceId))
                {
                    continue;
                }

                Devices.Add(
                    _logiDeviceViewModelFactory.CreateViewModel((x) =>
                    {
                        x.DeviceId = deviceId!;
                        x.DeviceName = "Not Initialised";
                        x.IsChecked = true;
                    })
                );
            }
        }

        public bool TryGetDevice(string deviceId, [NotNullWhen(true)] out LogiDevice? device)
        {
            device = Devices.SingleOrDefault(x => x.DeviceId == deviceId);

            return device != null;
        }

        public void OnInitMessage(InitMessage initMessage)
        {
            LogiDeviceViewModel? dev = Devices.SingleOrDefault(x => x.DeviceId == initMessage.deviceId);
            if (dev != null)
            {
                Application.Current.Dispatcher.BeginInvoke(() => dev.UpdateState(initMessage));

                return;
            }

            dev = _logiDeviceViewModelFactory.CreateViewModel((x) => x.UpdateState(initMessage));

            Application.Current.Dispatcher.BeginInvoke(() => Devices.Add(dev));
        }

        public void OnUpdateMessage(UpdateMessage updateMessage)
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                var device = Devices.FirstOrDefault(dev => dev.DeviceId == updateMessage.deviceId);
                if (device == null) { return; }

                device.UpdateState(updateMessage);
            });
        }
    }
}
