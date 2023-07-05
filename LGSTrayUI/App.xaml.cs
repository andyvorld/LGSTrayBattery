using MessagePipe;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace LGSTrayUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        //TaskbarIcon notifyIcon = null!;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //create the notifyicon (it's a resource declared in NotifyIconResources.xaml
            //notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");

            //LogiDevice[] ldevs = new LogiDevice[]
            //{
            //    new LogiDevice()
            //    {
            //        LastUpdate = DateTime.Now,
            //        BatteryPercentage = 99,
            //        DeviceId = "1",
            //        DeviceType = DeviceType.Keyboard,
            //    },
            //    new LogiDevice()
            //    {
            //        LastUpdate = DateTime.Now,
            //        BatteryPercentage = 99,
            //        DeviceId = "2",
            //        DeviceType = DeviceType.Mouse,
            //    },
            //    new LogiDevice()
            //    {
            //        LastUpdate = DateTime.Now,
            //        BatteryPercentage = 99,
            //        DeviceId = "3",
            //        DeviceType = DeviceType.Headset,
            //    },
            //};

            //LogiDeviceCollection.Instance.Devices.Add(ldevs[0]);
            //LogiDeviceCollection.Instance.Devices.Add(ldevs[1]);
            //LogiDeviceCollection.Instance.Devices.Add(ldevs[2]);

            //new Thread(async () =>
            //{
            //    while (true)
            //    {
            //        foreach (var ldev in ldevs)
            //        {
            //            ldev.LastUpdate = DateTime.Now;
            //            ldev.BatteryPercentage = (ldev.BatteryPercentage < -10) ? 99 : ldev.BatteryPercentage - 1;
            //        }
            //        await Task.Delay(100);
            //    }
            //}).Start();

            var host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.Sources.Clear();
                    configuration.AddIniFile("appsettings.ini");
                })
                .ConfigureServices((ctx, services) =>
                {
                    var configurationRoot = ctx.Configuration;
                    services.Configure<AppSettings>(configurationRoot);

                    services.AddMessagePipe();
                    services.AddMessagePipeNamedPipeInterprocess("LGSTray", config =>
                    {
                        config.HostAsServer = true;
                    });
                    services.AddSingleton<UserSettingsWrapper>();

                    services.AddSingleton<LogiDeviceIconFactory>();
                    services.AddSingleton<LogiDeviceViewModelFactory>();

                    services.AddHostedService<LGSTrayHIDDaemon>();
                    services.AddSingleton<LogiDeviceCollection>();

                    services.AddSingleton<MainTaskbarIconWrapper>();
                    services.AddHostedService<NotifyIconViewModel>();
                })
                .Build();

            await host.RunAsync();
            Dispatcher.InvokeShutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
        }
    }
}
