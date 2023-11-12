using LGSTrayCore;
using LGSTrayCore.HttpServer;
using LGSTrayCore.Managers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Winmdroot = Windows.Win32;
using System.Windows;
using System;
using System.Diagnostics;
using LGSTrayPrimitives.IPC;

namespace LGSTrayUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
        private static unsafe void EnableEfficiencyMode()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT &&
            Environment.OSVersion.Version >= new Version(6, 2)) {
                var handle = Process.GetCurrentProcess().SafeHandle;
                Winmdroot.PInvoke.SetPriorityClass(handle, Winmdroot.System.Threading.PROCESS_CREATION_FLAGS.IDLE_PRIORITY_CLASS);

                Winmdroot.System.Threading.PROCESS_POWER_THROTTLING_STATE state = new()
                {
                    Version = Winmdroot.PInvoke.PROCESS_POWER_THROTTLING_CURRENT_VERSION,
                    ControlMask = Winmdroot.PInvoke.PROCESS_POWER_THROTTLING_EXECUTION_SPEED,
                    StateMask = Winmdroot.PInvoke.PROCESS_POWER_THROTTLING_EXECUTION_SPEED,
                };

                Winmdroot.PInvoke.SetProcessInformation(
                    handle,
                    Winmdroot.System.Threading.PROCESS_INFORMATION_CLASS.ProcessPowerThrottling,
                    &state,
                    (uint)sizeof(Winmdroot.System.Threading.PROCESS_POWER_THROTTLING_STATE)
                );
            }
        }

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

            EnableEfficiencyMode();

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

                    services.AddLGSMessagePipe(true);
                    services.AddSingleton<UserSettingsWrapper>();

                    services.AddSingleton<LogiDeviceIconFactory>();
                    services.AddSingleton<LogiDeviceViewModelFactory>();

                    services.AddSingleton<HttpControllerFactory>();
                    services.AddHostedService<HttpServer>();

                    services.AddIDeviceManager<LGSTrayHIDManager>(configurationRoot);
                    services.AddIDeviceManager<GHubManager>(configurationRoot);
                    services.AddSingleton<ILogiDeviceCollection, LogiDeviceCollection>();

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
