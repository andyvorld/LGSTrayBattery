using LGSTrayCore;
using LGSTrayUI.Properties;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Hardcodet.Wpf.TaskbarNotification;
using System.Runtime.InteropServices;
using LGSTrayPrimitives;
using Microsoft.Win32;

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
        private static Bitmap Charging => CheckTheme.LightTheme ? Resources.Charging : Resources.Charging_dark;

        private static int ImageSize;

        static BatteryIconDrawing()
        {
            int dpi;

            try
            {
                var reg = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\ThemeManager", false);
                var _dpi = (string?)reg?.GetValue("LastLoadedDPI");
                if (!int.TryParse(_dpi, out dpi))
                {
                    dpi = 96;
                }
            }
            catch { dpi = 96; }
            var scale = dpi / 96f;

            ImageSize = (int)(32 * scale);
        }

        private static Bitmap GetDeviceIcon(LogiDevice device) => device.DeviceType switch
        {
            DeviceType.Keyboard => Keyboard,
            DeviceType.Headset => Headset,
            _ => Mouse,
        };

        private static Color GetDeviceColor(LogiDevice device) => device.DeviceType switch
        {
            _ => CheckTheme.LightTheme ? Color.FromArgb(0x11, 0x11, 0x11) : Color.FromArgb(0xEE, 0xEE, 0xEE)

            //return device.DeviceType switch
            //{
            //    DeviceType.Keyboard => Color.FromArgb(0xA1, 0xE4, 0x4D),
            //    DeviceType.Headset => Color.FromArgb(0xFA, 0x79, 0x21),
            //    _ => Color.FromArgb(0xBB, 0x86, 0xFC),
            //};
        };

        private static Bitmap GetBatteryValue(LogiDevice device) => device.BatteryPercentage switch
        {
            { } when device.PowerSupplyStatus == PowerSupplyStatus.POWER_SUPPLY_STATUS_CHARGING => Charging,
            < 0 => Missing,
            < 10 => Resources.Indicator_10,
            < 50 => Resources.Indicator_30,
            < 85 => Resources.Indicator_50,
            _ => Resources.Indicator_100
        };

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
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            using var wrapMode = new ImageAttributes();
            wrapMode.SetWrapMode(WrapMode.TileFlipXY);

            Bitmap[] layers = [
                GetBatteryValue(device),
                Battery,
                GetDeviceIcon(device),
            ];

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
            var colors = DeviceNumericColorStore.Resolve(Settings.Default.DeviceNumericColors,
                device.DeviceId, Settings.Default.NumericTextColor, Settings.Default.NumericBackgroundColor, Settings.Default.NumericBold);
            using Bitmap b = CreateNumericBitmap(device.BatteryPercentage,
                NumericIconColors.Parse(colors.Text, GetDeviceColor(device)),
                NumericIconColors.Parse(colors.Background, Color.Transparent), colors.Bold ?? false);
            IntPtr iconHandle = b.GetHicon();
            Icon tempManagedRes = Icon.FromHandle(iconHandle);
            taskbarIcon.Icon = (Icon)tempManagedRes.Clone();
            tempManagedRes.Dispose();
            DestroyIcon(iconHandle);
        }
        public static Bitmap CreateNumericBitmap(double percentage, Color textColor, Color backgroundColor, bool bold = false)
        {
            Bitmap bitmap = new(ImageSize, ImageSize);
            using Graphics graphics = Graphics.FromImage(bitmap);
            graphics.Clear(backgroundColor);
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            using Font font = new("Segoe UI", (int)(0.8 * ImageSize), bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
            using SolidBrush brush = new(textColor);
            using StringFormat format = new(StringFormatFlags.FitBlackBox, 0)
            {
                LineAlignment = StringAlignment.Center,
                Alignment = StringAlignment.Center,
            };
            string text = percentage < 0 ? "?" : $"{percentage:f0}";
            graphics.DrawString(text, font, brush, ImageSize / 2, ImageSize / 2, format);
            return bitmap;
        }
    }
}
