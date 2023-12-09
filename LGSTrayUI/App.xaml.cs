using LGSTrayCore;
using LGSTrayCore.Managers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using System;
using LGSTrayPrimitives.IPC;
using System.Globalization;
using System.IO;
using System.Threading;
using LGSTrayPrimitives;
using Tommy.Extensions.Configuration;

using static LGSTrayUI.AppExtensions;
using System.Threading.Tasks;

namespace LGSTrayUI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CrashHandler);

        EnableEfficiencyMode();

        var builder = Host.CreateEmptyApplicationBuilder(null);
        await LoadAppSettings(builder.Configuration);

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

    static async Task LoadAppSettings(Microsoft.Extensions.Configuration.ConfigurationManager config)
    {
        try
        {
            config.AddTomlFile("appsettings.toml");
        }
        catch (Exception ex)
        {
            if (ex is FileNotFoundException || ex is InvalidDataException)
            {
                var msgBoxRet = MessageBox.Show(
                    "Failed to read settings, do you want reset to default?", 
                    "LGSTray - Settings Load Error", 
                    MessageBoxButton.YesNo, MessageBoxImage.Error, MessageBoxResult.No
                );

                if (msgBoxRet == MessageBoxResult.Yes)
                {
                    await File.WriteAllBytesAsync(
                        Path.Combine(AppContext.BaseDirectory, "appsettings.toml"),
                        LGSTrayUI.Properties.Resources.defaultAppsettings
                    );
                }

                config.AddTomlFile("appsettings.toml");
            }
            else
            {
                throw;
            }
        }
    }

    private void CrashHandler(object sender, UnhandledExceptionEventArgs args)
    {
        Exception e = (Exception)args.ExceptionObject;
        long unixTime = DateTimeOffset.Now.ToUnixTimeSeconds();

        using StreamWriter writer = new($"./crashlog_{unixTime}.log", false);
        writer.WriteLine(e.ToString());
    }
}