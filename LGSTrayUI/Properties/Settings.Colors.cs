using System.Configuration;

namespace LGSTrayUI.Properties;

internal sealed partial class Settings
{
    [UserScopedSetting, DefaultSettingValue("{}")]
    public string DeviceNumericColors
    {
        get => (string)this[nameof(DeviceNumericColors)];
        set => this[nameof(DeviceNumericColors)] = value;
    }

    [UserScopedSetting, DefaultSettingValue("False")]
    public bool NumericBold
    {
        get => (bool)this[nameof(NumericBold)];
        set => this[nameof(NumericBold)] = value;
    }

    // Empty values retain the original automatic text / transparent background.
    [UserScopedSetting, DefaultSettingValue("")]
    public string NumericTextColor
    {
        get => (string)this[nameof(NumericTextColor)];
        set => this[nameof(NumericTextColor)] = value;
    }

    [UserScopedSetting, DefaultSettingValue("")]
    public string NumericBackgroundColor
    {
        get => (string)this[nameof(NumericBackgroundColor)];
        set => this[nameof(NumericBackgroundColor)] = value;
    }
}
