using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
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
            string dir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            if (dir != null)
            {
                Directory.SetCurrentDirectory(dir);
            }

            using IHost host = Host.CreateDefaultBuilder(e.Args).ConfigureAppConfiguration((hostingContext, configuration) =>
            {
                configuration.Sources.Clear();

                IHostEnvironment env = hostingContext.HostingEnvironment;

                configuration.AddIniFile("appsettings.ini", optional: true, reloadOnChange: true);

                AppSettings.Settings = configuration.Build().Get<AppSettings.AppSettingsInstace>();
            }).Build();

            MainWindow mw = new();
            mw.Show();

            await host.RunAsync();
        }
    }
}
