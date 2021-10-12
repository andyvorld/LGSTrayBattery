using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LGSTrayGUI
{
    static class CheckTheme
    {
        public static bool LightTheme
        {
            get
            {
                var regPath = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", false);
                int regFlag = (int) regPath.GetValue("SystemUsesLightTheme", 0);

                return (regFlag != 0);
            }
        }

        public static string ThemeSuffix
        {
            get
            {
                return LightTheme ? "_light" : "";
            }
        }
    }
}
