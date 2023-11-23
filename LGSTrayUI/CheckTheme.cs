using Microsoft.Win32;
using System.ComponentModel;
using System.Globalization;
using System.Management;
using System.Security.Principal;

namespace LGSTrayUI
{
    public static class CheckTheme
    {
        private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string RegistryValueName = "SystemUsesLightTheme";

        private static bool _lightTheme = true;
        public static bool LightTheme => _lightTheme;

        public static string ThemeSuffix
        {
            get
            {
                return LightTheme ? "" : "_dark";
            }
        }

        public static event PropertyChangedEventHandler? StaticPropertyChanged;

        static CheckTheme()
        {
            var currentUser = WindowsIdentity.GetCurrent();
            string query = string.Format(
                CultureInfo.InvariantCulture,
                @"SELECT * FROM RegistryValueChangeEvent WHERE Hive = 'HKEY_USERS' AND KeyPath = '{0}\\{1}' AND ValueName = '{2}'",
                currentUser.User!.Value,
                RegistryKeyPath.Replace(@"\", @"\\"),
                RegistryValueName);

            try
            {
                var watcher = new ManagementEventWatcher(query);
                watcher.EventArrived += Watcher_EventArrived;

                watcher.Start();
                UpdateThemeStatus();
            }
            catch
            {
                // Fails on Win7
                _lightTheme = false;
            }

        }

        private static void UpdateThemeStatus()
        {
            var regPath = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
            int regFlag = (int)regPath!.GetValue(RegistryValueName, 0);

            _lightTheme = regFlag != 0;
            StaticPropertyChanged?.Invoke(typeof(CheckTheme), new(nameof(LightTheme)));
        }

        private static void Watcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            UpdateThemeStatus();
        }
    }
}
