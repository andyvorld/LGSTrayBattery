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
                return "/Resources/Bat_100.ico";
            }
            else if (batteryPercent >= 75)
            {
                return "/Resources/Bat_75.ico";
            }
            else if (batteryPercent >= 50)
            {
                return "/Resources/Bat_50.ico";
            }
            else if (batteryPercent >= 25)
            {
                return "/Resources/Bat_25.ico";
            }
            else if (batteryPercent < 25)
            {
                return "/Resources/Bat_10.ico";
            }

            return "/Resources/Unknown.ico";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}