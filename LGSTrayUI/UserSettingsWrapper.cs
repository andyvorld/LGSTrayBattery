using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LGSTrayUI
{
    public partial class UserSettingsWrapper : ObservableObject
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "")]
        public StringCollection SelectedDevices => Properties.Settings.Default.SelectedDevices;

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
