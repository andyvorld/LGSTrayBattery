using System;
using System.Globalization;
using System.Windows.Data;

namespace LGSTrayBattery
{
    public class BatteryToIcoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double batteryPercent = value is double d ? d : 0;

            if (batteryPercent >= 90)
            {
                return $"/Resources/Bat_100{CheckTheme.ThemeSuffix}.ico";
            }
            else if (batteryPercent >= 65)
            {
                return $"/Resources/Bat_75{CheckTheme.ThemeSuffix}.ico";
            }
            else if (batteryPercent >= 40)
            {
                return $"/Resources/Bat_50{CheckTheme.ThemeSuffix}.ico";
            }
            else if (batteryPercent >= 15)
            {
                return $"/Resources/Bat_25{CheckTheme.ThemeSuffix}.ico";
            }
            else if (batteryPercent < 15)
            {
                return $"/Resources/Bat_10{CheckTheme.ThemeSuffix}.ico";
            }

            return $"/Resources/Unknown{CheckTheme.ThemeSuffix}.ico";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}