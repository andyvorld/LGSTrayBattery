using LGSTrayCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LGSTrayGUI
{
    public class LogiDeviceIsSelectedConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            //if (values[0] == null)
            //{
            //    return false;
            //}

            return (values[0] as LogiDevice)?.DeviceID == (values[1] as LogiDevice)?.DeviceID;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
