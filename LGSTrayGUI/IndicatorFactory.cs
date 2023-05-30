using System;
using System.Collections.Generic;
using System.Drawing;

namespace LGSTrayGUI
{
    public class IndicatorFactory
    {
        private Size _imageSize = new Size(48, 48);

        private int _left = 29;
        private int _right = 43;
        private int _top = 13;
        private int _bottom = 41;

        private Dictionary<int, Bitmap> _cachedIndicators = new Dictionary<int, Bitmap>();

        public Bitmap DrawIndicator(int percentage)
        {
            percentage = Math.Min(Math.Max(percentage, 0), 100); // ensure that percentage is always 0-100
            if (_cachedIndicators.ContainsKey(percentage))
                return _cachedIndicators[percentage];

            var bitmap = new Bitmap(_imageSize.Width, _imageSize.Height);
            using var graphics = Graphics.FromImage(bitmap);
            
            int height = (int)((_bottom - _top) * percentage / 100f);
            if (height > 0)
            {
                Color gradientColor = GenerateColor(percentage);
                 
                graphics.FillRectangle(new SolidBrush(gradientColor), _left, _bottom - height, _right - _left, height);
            }

            _cachedIndicators.Add(percentage, bitmap);
            return bitmap;
        }

        private Color GenerateColor(int percentage)
        {
            if (percentage <= 75)
            {
                int red = 255;
                int green = (int)(percentage / 75f * 255);
                int blue = 0;
                return Color.FromArgb(red, green, blue);
            }
            else
            {
                int red = (int)((100 - percentage) / 25f * 255);
                int green = 255;
                int blue = 0;
                return Color.FromArgb(red, green, blue);
            }
        }
    }
}
