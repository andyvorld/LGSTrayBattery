using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using LGSTrayPrimitives.IPC;
using Microsoft.Extensions.Configuration;
using LGSTrayPrimitives;
using Tommy.Extensions.Configuration;

namespace LGSTrayHID
{
    internal static class GlobalSettings
    {
        public static NativeDeviceManagerSettings settings = new();
    }

    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = Host.CreateEmptyApplicationBuilder(null);
            builder.Configuration.AddTomlFile("appsettings.toml");

            GlobalSettings.settings = builder.Configuration.GetSection("Native")
                .Get<NativeDeviceManagerSettings>() ?? GlobalSettings.settings;

            builder.Services.AddLGSMessagePipe();
            builder.Services.AddHostedService<HidppManagerService>();

            var host = builder.Build();

            _ = Task.Run(async () =>
            {
                bool ret = int.TryParse(args.ElementAtOrDefault(0), out int parentPid);
                if (!ret) {
#if DEBUG
                    return; 
#else
                    // Started without a parent, assume invalid.
                    Environment.Exit(0);
#endif
                }

                await Process.GetProcessById(parentPid).WaitForExitAsync();

                CancellationTokenSource cts = new(5000);
                await host.StopAsync(cts.Token);

                Environment.Exit(0);
            });

            await host.RunAsync();
        }
    }
}