using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Specialized;

namespace LGSTrayUI
{
    public partial class UserSettingsWrapper : ObservableObject
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "")]
        public StringCollection SelectedDevices => Properties.Settings.Default.SelectedDevices;

        public DeviceNumericColors GetDeviceNumericColors(string deviceId) =>
            DeviceNumericColorStore.Get(Properties.Settings.Default.DeviceNumericColors, deviceId);

        public void SetDeviceNumericColors(string deviceId, DeviceNumericColors colors)
        {
            Properties.Settings.Default.DeviceNumericColors = DeviceNumericColorStore.Set(
                Properties.Settings.Default.DeviceNumericColors, deviceId, colors);
            Properties.Settings.Default.Save();
            OnPropertyChanged("DeviceNumericColors");
        }

        public bool NumericBold
        {
            get => Properties.Settings.Default.NumericBold;
            set
            {
                Properties.Settings.Default.NumericBold = value;
                Properties.Settings.Default.Save();
                OnPropertyChanged();
            }
        }

        public string NumericTextColor
        {
            get => Properties.Settings.Default.NumericTextColor;
            set
            {
                Properties.Settings.Default.NumericTextColor = value;
                Properties.Settings.Default.Save();
                OnPropertyChanged();
            }
        }

        public string NumericBackgroundColor
        {
            get => Properties.Settings.Default.NumericBackgroundColor;
            set
            {
                Properties.Settings.Default.NumericBackgroundColor = value;
                Properties.Settings.Default.Save();
                OnPropertyChanged();
            }
        }

        public bool NumericDisplay
        {
            get => Properties.Settings.Default.NumericDisplay;
            set
            {
                Properties.Settings.Default.NumericDisplay = value;
                Properties.Settings.Default.Save();

                OnPropertyChanged();
            }
        }

        public void AddDevice(string deviceId)
        {
            if (Properties.Settings.Default.SelectedDevices.Contains(deviceId))
            {
                return;
            }

            Properties.Settings.Default.SelectedDevices.Add(deviceId);
            Properties.Settings.Default.Save();

            OnPropertyChanged(nameof(SelectedDevices));
        }

        public void RemoveDevice(string deviceId)
        {
            Properties.Settings.Default.SelectedDevices.Remove(deviceId);
            Properties.Settings.Default.Save();

            OnPropertyChanged(nameof(SelectedDevices));
        }
    }
}
