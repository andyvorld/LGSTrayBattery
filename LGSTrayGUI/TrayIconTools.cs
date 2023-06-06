using LGSTrayCore;
using LGSTrayGHUB;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LGSTrayGUI
{
    public static class TrayIconTools
    {
        private static Bitmap Mouse => CheckTheme.LightTheme ? Properties.Resources.Mouse : Properties.Resources.Mouse_dark;
        private static Bitmap Keyboard => CheckTheme.LightTheme ? Properties.Resources.Keyboard : Properties.Resources.Keyboard_dark;
        private static Bitmap Headset => CheckTheme.LightTheme ? Properties.Resources.Headset : Properties.Resources.Headset_dark;
        private static Bitmap Battery => CheckTheme.LightTheme ? Properties.Resources.Battery : Properties.Resources.Battery_dark;
        private static Bitmap Charging => CheckTheme.LightTheme ? Properties.Resources.Charging : Properties.Resources.Charging_dark;
        private static Bitmap Missing => CheckTheme.LightTheme ? Properties.Resources.Missing : Properties.Resources.Missing_dark;

        private static IndicatorFactory _indicatorFactory = new IndicatorFactory();

        private static Bitmap MixBitmap(params Bitmap[] bitmaps)
        {
            Bitmap result = new Bitmap(bitmaps[0].Width, bitmaps[0].Height);
            Graphics canvas = Graphics.FromImage(result);
            foreach (var bitmap in bitmaps)
                if (bitmap != null)
                    canvas.DrawImage(bitmap, new Point(0, 0));

            canvas.Save();

            return result;
        }

        private static Bitmap ErrorBitMap()
        {
            return MixBitmap(Mouse, Battery, Missing);
        }

        public static Icon ErrorIcon()
        {
            return Icon.FromHandle(ErrorBitMap().GetHicon());
        }
        public static Icon GenerateIcon(LogiDevice logiDevice)
        {
            Bitmap output;
            if (logiDevice == null)
            {
                output = ErrorBitMap();
            }
            else
            {
                Bitmap device = GetDeviceIcon(logiDevice);
                Bitmap indicator = _indicatorFactory.DrawIndicator((int)logiDevice.BatteryPercentage);
                Bitmap status = GetStatusIcon(logiDevice);

                output = MixBitmap(device, Battery, indicator, status);
            }

            return Icon.FromHandle(output.GetHicon());
        }

        private static Bitmap GetDeviceIcon(LogiDevice logiDevice)
        {
            Bitmap device;
            switch (logiDevice.DeviceType)
            {
                case DeviceType.Mouse:
                    device = Mouse;
                    break;
                case DeviceType.Keyboard:
                    device = Keyboard;
                    break;
                case DeviceType.Headset:
                    device = Headset;
                    break;
                default:
                    device = Mouse;
                    break;
            }

            return device;
        }

        private static Bitmap? GetStatusIcon(LogiDevice logiDevice)
        {
            if (logiDevice is LogiDeviceGHUB dev && dev.Charging || logiDevice is LogiDeviceNative dev2 && dev2.Charging)
                return Charging;

            if (logiDevice.BatteryPercentage == 0)
                return Missing;

            return null;
        }
    }
}
