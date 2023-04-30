using Hardcodet.Wpf.TaskbarNotification;
using LGSTrayCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace LGSTrayUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private TaskbarIcon notifyIcon = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //create the notifyicon (it's a resource declared in NotifyIconResources.xaml
            notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");

            LogiDevice[] ldevs = new LogiDevice[]
            {
                new LogiDevice()
                {
                    LastUpdate = DateTime.Now,
                    BatteryPercentage = 99,
                    DeviceId = "1",
                    DeviceType = DeviceType.Keyboard,
                },
                new LogiDevice()
                {
                    LastUpdate = DateTime.Now,
                    BatteryPercentage = 99,
                    DeviceId = "2",
                    DeviceType = DeviceType.Mouse,
                },
                new LogiDevice()
                {
                    LastUpdate = DateTime.Now,
                    BatteryPercentage = 99,
                    DeviceId = "3",
                    DeviceType = DeviceType.Headset,
                },
            };

            LogiDeviceCollection.Instance.Devices.Add(ldevs[0]);
            LogiDeviceCollection.Instance.Devices.Add(ldevs[1]);
            LogiDeviceCollection.Instance.Devices.Add(ldevs[2]);

            new Thread(async () =>
            {
                while (true)
                {
                    foreach (var ldev in ldevs)
                    {
                        ldev.LastUpdate = DateTime.Now;
                        ldev.BatteryPercentage = (ldev.BatteryPercentage < -10) ? 99 : ldev.BatteryPercentage - 1;
                    }
                    await Task.Delay(100);
                }
            }).Start();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            notifyIcon.Dispose(); //the icon would clean up automatically, but this is cleaner
            base.OnExit(e);
        }
    }
}
