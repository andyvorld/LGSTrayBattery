using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LGSTrayCore.Managers;

public static class IServiceExtension 
{
    private static bool IsManagerEnabled(AppSettings settings, Type T) => T switch
    {
        { } when T == typeof(GHubManager) => settings.DeviceManager.GHUB,
        { } when T == typeof(LGSTrayHIDManager) => settings.DeviceManager.Native,
        _ => false
    };

    public static void AddIDeviceManager<T>(this IServiceCollection services, IConfiguration configs) where T : class, IDeviceManager, IHostedService
    {
        var settings = configs.Get<AppSettings>()!;
        bool isEnabled = IsManagerEnabled(settings, typeof(T));
        if (!isEnabled)
        {
            return;
        }

        services.AddSingleton<T>();
        services.AddSingleton<IDeviceManager>(p => p.GetRequiredService<T>());
        services.AddSingleton<IHostedService>(p => p.GetRequiredService<T>());
    }
}

public interface IDeviceManager
{
    public void RediscoverDevices();
}
