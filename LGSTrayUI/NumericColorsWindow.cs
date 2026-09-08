using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Automation;
using DrawingColor = System.Drawing.Color;

namespace LGSTrayUI;

public sealed class NumericColorsWindow : Window
{
    private readonly UserSettingsWrapper _settings;
    private readonly ComboBox _devices = new() { MinWidth = 420, DisplayMemberPath = "Label" };
    private readonly ComboBox _weight = new() { Margin = new Thickness(0, 6, 0, 14) };
    private readonly NumericColorEditor _text = new("Text color", "Automatic (Windows theme)");
    private readonly NumericColorEditor _background = new("Background color", "Transparent");
    private readonly TextBlock _status = new() { Margin = new Thickness(0, 8, 0, 0) };
    private readonly ObservableCollection<LogiDeviceViewModel> _source;
    private string? _deviceId;
    private bool _loading;

    private sealed record DeviceChoice(string? Id, string Label);

    public NumericColorsWindow(UserSettingsWrapper settings, ObservableCollection<LogiDeviceViewModel> devices)
    {
        _settings = settings;
        _source = devices;
        Title = "LGSTray - Numeric Icon Appearance";
        Width = 590;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.CanMinimize;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ShowInTaskbar = true;
        Background = Brushes.White;
        Foreground = Brushes.Black;
        var panel = new StackPanel { Margin = new Thickness(22) };
        panel.Children.Add(new TextBlock { Text = "Numeric icon appearance", FontSize = 23, FontWeight = FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = "Choose a device, adjust its appearance, then click Apply.", Margin = new Thickness(0, 8, 0, 14) });
        AutomationProperties.SetName(_devices, "Device");
        panel.Children.Add(_devices);
        var columns = new Grid { Margin = new Thickness(0, 18, 0, 12) };
        columns.ColumnDefinitions.Add(new ColumnDefinition());
        columns.ColumnDefinitions.Add(new ColumnDefinition());
        _text.Margin = new Thickness(0, 0, 12, 0);
        columns.Children.Add(_text);
        Grid.SetColumn(_background, 1);
        columns.Children.Add(_background);
        panel.Children.Add(columns);
        panel.Children.Add(new TextBlock { Text = "Font weight", FontWeight = FontWeights.SemiBold });
        AutomationProperties.SetName(_weight, "Font weight");
        panel.Children.Add(_weight);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var apply = new Button { Content = "Apply", MinWidth = 95, Padding = new Thickness(12, 6, 12, 6), IsDefault = true };
        apply.Click += (_, _) => SaveColors();
        var close = new Button { Content = "Close", MinWidth = 95, Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(8, 0, 0, 0) };
        close.Click += (_, _) => Close();
        buttons.Children.Add(apply);
        buttons.Children.Add(close);
        panel.Children.Add(buttons);
        panel.Children.Add(_status);
        Content = panel;
        RefreshDevices();
        _devices.SelectionChanged += (_, _) => LoadColors();
        _source.CollectionChanged += OnDevicesChanged;
        Closed += (_, _) => _source.CollectionChanged -= OnDevicesChanged;
        LoadColors();
    }

    private void OnDevicesChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => RefreshDevices();

    private void RefreshDevices()
    {
        var selected = _deviceId;
        _loading = true;
        _devices.Items.Clear();
        _devices.Items.Add(new DeviceChoice(null, "Defaults (all devices)"));
        foreach (var device in _source)
            _devices.Items.Add(new DeviceChoice(device.DeviceId, $"{device.DeviceName} ({device.DeviceId})"));
        _devices.SelectedIndex = 0;
        foreach (DeviceChoice choice in _devices.Items)
            if (choice.Id == selected) { _devices.SelectedItem = choice; break; }
        _loading = false;
    }

    private void LoadColors()
    {
        if (_loading || _devices.SelectedItem is not DeviceChoice choice) return;
        _deviceId = choice.Id;
        var colors = _deviceId is null
            ? new DeviceNumericColors(_settings.NumericTextColor, _settings.NumericBackgroundColor, _settings.NumericBold)
            : _settings.GetDeviceNumericColors(_deviceId);
        _text.Load(colors.Text, _deviceId is not null);
        _background.Load(colors.Background, _deviceId is not null);
        _weight.Items.Clear();
        if (_deviceId is not null) _weight.Items.Add("Use default");
        _weight.Items.Add("Normal");
        _weight.Items.Add("Bold");
        _weight.SelectedIndex = colors.Bold is null && _deviceId is not null ? 0 : (colors.Bold == true ? 1 : 0) + (_deviceId is null ? 0 : 1);
        _status.Text = "Changes are saved only when you click Apply.";
    }

    private void SaveColors()
    {
        if (!_text.TryGetValue(out var text) || !_background.TryGetValue(out var background))
        {
            _status.Text = "Enter a valid hex color, for example #00AAFF.";
            return;
        }
        try
        {
            if (_deviceId is null)
            {
                _settings.NumericTextColor = text ?? "";
                _settings.NumericBackgroundColor = background ?? "";
                _settings.NumericBold = _weight.SelectedIndex == 1;
            }
            else _settings.SetDeviceNumericColors(_deviceId, new(text, background, _weight.SelectedIndex == 0 ? null : _weight.SelectedIndex == 2));
            _status.Text = "Saved. Enable Display Numeric Icon in the tray menu to see the colors.";
        }
        catch (System.Configuration.ConfigurationErrorsException)
        {
            _status.Text = "Could not save settings. Check access to your user settings folder.";
        }
    }
}

internal sealed class NumericColorEditor : StackPanel
{
    private readonly ComboBox _mode = new() { Margin = new Thickness(0, 8, 0, 8) };
    private readonly TextBox _hex = new() { Margin = new Thickness(0, 6, 0, 8), MaxLength = 7 };

    private readonly Border _swatch = new() { Height = 36, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1) };
    private readonly StackPanel _custom = new();
    private readonly string _automatic;

    private bool _inherit;

    public NumericColorEditor(string title, string automatic)
    {
        _automatic = automatic;
        Children.Add(new TextBlock { Text = title, FontSize = 16, FontWeight = FontWeights.SemiBold });
        AutomationProperties.SetName(_mode, title + " mode");
        Children.Add(_mode);
        _custom.Children.Add(_swatch);
        AutomationProperties.SetName(_hex, title + " hex");
        _custom.Children.Add(_hex);
        var picker = new Button { Content = "Choose color...", Padding = new Thickness(10, 6, 10, 6) };
        AutomationProperties.SetName(picker, title + " picker");
        picker.Click += (_, _) => ChooseColor();
        _custom.Children.Add(picker);
        Children.Add(_custom);
        _hex.TextChanged += (_, _) => HexChanged();
        _mode.SelectionChanged += (_, _) => _custom.IsEnabled = _mode.SelectedIndex == (_inherit ? 2 : 1);
    }

    public void Load(string? value, bool inherit)
    {
        _inherit = inherit;
        _mode.Items.Clear();
        if (inherit) _mode.Items.Add("Use default colors");
        _mode.Items.Add(_automatic);
        _mode.Items.Add("Custom color");
        _hex.Text = string.IsNullOrEmpty(value) ? "#FFFFFF" : value;
        HexChanged();
        _mode.SelectedIndex = value is null && inherit ? 0 : string.IsNullOrEmpty(value) ? (inherit ? 1 : 0) : (inherit ? 2 : 1);
    }

    public bool TryGetValue(out string? value)
    {
        value = null;
        if (_inherit && _mode.SelectedIndex == 0) return true;
        value = "";
        if (_mode.SelectedIndex == (_inherit ? 1 : 0)) return true;
        var parsed = NumericIconColors.Parse(_hex.Text, DrawingColor.Empty);
        if (parsed.IsEmpty) return false;
        value = NumericIconColors.Format(parsed);
        return true;
    }

    private void HexChanged()
    {
        var color = NumericIconColors.Parse(_hex.Text, DrawingColor.Empty);
        _hex.BorderBrush = color.IsEmpty ? Brushes.Red : SystemColors.ControlDarkBrush;
        if (!color.IsEmpty)
            _swatch.Background = new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B));
    }

    private sealed class DialogOwner(IntPtr handle) : System.Windows.Forms.IWin32Window
    {
        public IntPtr Handle { get; } = handle;
    }

    private void ChooseColor()
    {
        var window = Window.GetWindow(this);
        if (window is null) return;
        // Own the native picker with the persistent settings window, never the tray popup.
        var owner = new DialogOwner(new System.Windows.Interop.WindowInteropHelper(window).EnsureHandle());
        using var dialog = new System.Windows.Forms.ColorDialog
        {
            FullOpen = true,
            AnyColor = true,
            Color = NumericIconColors.Parse(_hex.Text, DrawingColor.White)
        };
        if (dialog.ShowDialog(owner) == System.Windows.Forms.DialogResult.OK)
            _hex.Text = NumericIconColors.Format(dialog.Color);
    }
}
