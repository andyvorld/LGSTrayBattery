using System;
using System.Drawing;
using System.Globalization;

namespace LGSTrayUI;

public static class NumericIconColors
{
    public static Color Parse(string? value, Color fallback)
    {
        if (value is { Length: 7 } && value[0] == '#' &&
            int.TryParse(value.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
            return Color.FromArgb(255, (rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255);
        return fallback;
    }

    public static string Format(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
}
