using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LGSTrayUI;

public class ThemeBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush Black = new(Color.FromRgb(0x2e, 0x2e, 0x2e));
    private static readonly SolidColorBrush BlackText = new(Colors.Black);
    private static readonly SolidColorBrush White = new(Color.FromRgb(0xd0, 0xd0, 0xd0));
    private static readonly SolidColorBrush WhiteTest = new(Colors.White);

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not bool lightTheme) return Black;
        string? param = parameter as string;

        return lightTheme switch
        {
            true when param == "Text" => BlackText,
            false when param == "Text" => WhiteTest,
            true => White,
            false => Black,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
