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
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.IO;
using System.Threading;
using Tommy.Extensions.Configuration;

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

            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CrashHandler);

            EnableEfficiencyMode();

            var builder = Host.CreateEmptyApplicationBuilder(null);
            builder.Configuration.AddTomlFile("appsettings.toml");

            builder.Services.Configure<AppSettings>(builder.Configuration);
            builder.Services.AddLGSMessagePipe(true);
            builder.Services.AddSingleton<UserSettingsWrapper>();

            builder.Services.AddSingleton<LogiDeviceIconFactory>();
            builder.Services.AddSingleton<LogiDeviceViewModelFactory>();

            builder.Services.AddWebserver(builder.Configuration);

            builder.Services.AddIDeviceManager<LGSTrayHIDManager>(builder.Configuration);
            builder.Services.AddIDeviceManager<GHubManager>(builder.Configuration);
            builder.Services.AddSingleton<ILogiDeviceCollection, LogiDeviceCollection>();

            builder.Services.AddSingleton<MainTaskbarIconWrapper>();
            builder.Services.AddHostedService<NotifyIconViewModel>();

            var host = builder.Build();
            await host.RunAsync();
            Dispatcher.InvokeShutdown();
        }

        private void CrashHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            long unixTime = DateTimeOffset.Now.ToUnixTimeSeconds();

            using StreamWriter writer = new($"./crashlog_{unixTime}.log", false);
            writer.WriteLine(e.ToString());
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
        }
    }
}
