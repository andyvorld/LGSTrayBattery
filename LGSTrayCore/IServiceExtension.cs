using LGSTrayCore.HttpServer;
using LGSTrayCore.Managers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LGSTrayCore;

public static class IServiceExtension
{
    public static void AddWebserver(this IServiceCollection services, IConfiguration configs)
    {
        var settings = configs.Get<AppSettings>()!;
        if (!settings.HTTPServer.ServerEnable) return;

        services.AddSingleton<HttpControllerFactory>();
        services.AddHostedService<HttpServer.HttpServer>();
    }

    public static void AddIDeviceManager<T>(this IServiceCollection services, IConfiguration configs) where T : class, IDeviceManager, IHostedService
    {
        var settings = configs.Get<AppSettings>()!;
        bool isEnabled = typeof(T) switch
        {
            { } when typeof(T) == typeof(GHubManager) => settings.DeviceManager.GHUB,
            { } when typeof(T) == typeof(LGSTrayHIDManager) => settings.DeviceManager.Native,
            _ => false
        };
        if (!isEnabled) return;

        services.AddSingleton<T>();
        services.AddSingleton<IDeviceManager>(p => p.GetRequiredService<T>());
        services.AddSingleton<IHostedService>(p => p.GetRequiredService<T>());
    }
}
