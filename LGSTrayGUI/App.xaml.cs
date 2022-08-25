using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace LGSTrayGUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public async void App_Startup(object sender, StartupEventArgs e)
        {
            using IHost host = Host.CreateDefaultBuilder(e.Args).ConfigureAppConfiguration((hostingContext, configuration) =>
            {
                configuration.Sources.Clear();

                IHostEnvironment env = hostingContext.HostingEnvironment;

                configuration.AddIniFile("appsettings.ini", optional: true, reloadOnChange: true);

                AppSettings.Setting = configuration.Build().Get<AppSettings.AppSettingsInstace>();
            }).Build();

            await host.RunAsync();
        }
    }
}
