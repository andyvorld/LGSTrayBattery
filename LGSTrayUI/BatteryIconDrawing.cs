using LGSTrayCore;
using LGSTrayUI.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Hardcodet.Wpf.TaskbarNotification;
using System.Runtime.InteropServices;
using System.Resources;
using LGSTrayPrimitives;

namespace LGSTrayUI
{
    public static partial class BatteryIconDrawing
    {
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool DestroyIcon(IntPtr handle);

        private static Bitmap Mouse => CheckTheme.LightTheme ? Resources.Mouse : Resources.Mouse_dark;
        private static Bitmap Keyboard => CheckTheme.LightTheme ? Resources.Keyboard : Resources.Keyboard_dark;
        private static Bitmap Headset => CheckTheme.LightTheme ? Resources.Headset : Resources.Headset_dark;
        private static Bitmap Battery => CheckTheme.LightTheme ? Resources.Battery : Resources.Battery_dark;
        private static Bitmap Missing => CheckTheme.LightTheme ? Resources.Missing : Resources.Missing_dark;

        private const int ImageSize = 32;

        private static Bitmap GetDeviceIcon(LogiDevice device)
        {
            return device.DeviceType switch
            {
                DeviceType.Keyboard => Keyboard,
                DeviceType.Headset => Headset,
                _ => Mouse,
            };
        }

        private static Color GetDeviceColor(LogiDevice device)
        {
            return Color.FromArgb(0xEE, 0xEE, 0xEE);

            //return device.DeviceType switch
            //{
            //    DeviceType.Keyboard => Color.FromArgb(0xA1, 0xE4, 0x4D),
            //    DeviceType.Headset => Color.FromArgb(0xFA, 0x79, 0x21),
            //    _ => Color.FromArgb(0xBB, 0x86, 0xFC),
            //};
        }

        private static Bitmap GetBatteryValue(LogiDevice device)
        {
            if (device.BatteryPercentage < 0)
            {
                return Missing;
            }
            else if (device.BatteryPercentage < 10)
            {
                return Resources.Indicator_10;
            }
            else if (device.BatteryPercentage < 30)
            {
                return Resources.Indicator_30;
            }
            else if (device.BatteryPercentage < 50)
            {
                return Resources.Indicator_50;
            }
            else
            {
                return Resources.Indicator_100;
            }
        }

        public static void DrawUnknown(TaskbarIcon taskbarIcon)
        {
            DrawIcon(taskbarIcon, new()
            {
                BatteryPercentage = -1,
            });
        }

        public static void DrawIcon(TaskbarIcon taskbarIcon, LogiDevice device)
        {
            var destRect = new Rectangle(0, 0, ImageSize, ImageSize);
            using var b = new Bitmap(ImageSize, ImageSize);
            using var g = Graphics.FromImage(b);
            g.CompositingMode = CompositingMode.SourceOver;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            using var wrapMode = new ImageAttributes();
            wrapMode.SetWrapMode(WrapMode.TileFlipXY);

            Bitmap[] layers = new Bitmap[]
            {
                GetBatteryValue(device),
                Battery,
                GetDeviceIcon(device),
            };

            foreach (var image in layers)
            {
                g.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                image.Dispose();
            }

            g.Save();

            IntPtr iconHandle = b.GetHicon();
            Icon tempManagedRes = Icon.FromHandle(iconHandle);
            taskbarIcon.Icon = (Icon)tempManagedRes.Clone();
            tempManagedRes.Dispose();
            DestroyIcon(iconHandle);
        }

        public static void DrawNumeric(TaskbarIcon taskbarIcon, LogiDevice device)
        {
            using Bitmap b = new(ImageSize, ImageSize);
            using Graphics g = Graphics.FromImage(b);

            string displayString = (device.BatteryPercentage < 0) ? "?" : $"{device.BatteryPercentage:f0}";
            g.DrawString(
                displayString,
                new Font("Segoe UI", (int) (0.8 * ImageSize), GraphicsUnit.Pixel),
                new SolidBrush(GetDeviceColor(device)),
                ImageSize/2, ImageSize/2,
                new(StringFormatFlags.FitBlackBox, 0)
                {
                    LineAlignment = StringAlignment.Center,
                    Alignment = StringAlignment.Center,
                }
            );

            IntPtr iconHandle = b.GetHicon();
            Icon tempManagedRes = Icon.FromHandle(iconHandle);
            taskbarIcon.Icon = (Icon)tempManagedRes.Clone();
            tempManagedRes.Dispose();
            DestroyIcon(iconHandle);
        }
    }
}
