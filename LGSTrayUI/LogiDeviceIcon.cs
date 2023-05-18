using Hardcodet.Wpf.TaskbarNotification;
using LGSTrayCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace LGSTrayUI
{
    public class LogiDeviceIconFactory
    {
        private readonly UserSettingsWrapper _userSettings;

        public LogiDeviceIconFactory(UserSettingsWrapper userSettings)
        {
            _userSettings = userSettings;
        }

        public LogiDeviceIcon CreateDeviceIcon(LogiDevice device, Action<LogiDeviceIcon>? config = null) 
        {
            LogiDeviceIcon output = new(device, _userSettings);
            config?.Invoke(output);

            return output;
        }
    }

    public class LogiDeviceIcon : IDisposable
    {
        public static int RefCount = 0;

        public static void AddRef()
        {
            RefCount++;
            RefCountChanged?.Invoke(RefCount, new("_refCount"));
        }

        public static void SubRef()
        {
            RefCount--;
            RefCountChanged?.Invoke(RefCount, new("_refCount"));
        }

        public static event PropertyChangedEventHandler? RefCountChanged;

        #region IDisposable
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _taskbarIcon.Dispose();
                    SubRef();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~LogiDeviceIcon()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion

        private readonly TaskbarIcon _taskbarIcon;
        private readonly LogiDevice _logiDevice;
        //private readonly NotifyIconViewModel _notifyIconViewModel;

        private Action<TaskbarIcon, LogiDevice> _drawBatteryIcon;

        public LogiDeviceIcon(LogiDevice device, UserSettingsWrapper userSettings)
        {
            AddRef();

            _taskbarIcon = new()
            {
                DataContext = device,
                ToolTipText = device.ToolTipString
            };
            _taskbarIcon.ContextMenu = (ContextMenu)_taskbarIcon.FindResource("SysTrayMenu");

            _logiDevice = device;
            _logiDevice.PropertyChanged += LogiDevicePropertyChanged;

            userSettings.PropertyChanged += NotifyIconViewModelPropertyChanged;
            _drawBatteryIcon = userSettings.NumericDisplay ? BatteryIconDrawing.DrawNumeric : BatteryIconDrawing.DrawIcon;
            DrawBatteryIcon();
        }

        private void NotifyIconViewModelPropertyChanged(object? s, PropertyChangedEventArgs e)
        {
            if (s is not UserSettingsWrapper userSettings)
            {
                return;
            }

            if (e.PropertyName == nameof(UserSettingsWrapper.NumericDisplay))
            {
                _drawBatteryIcon = userSettings.NumericDisplay ? BatteryIconDrawing.DrawNumeric : BatteryIconDrawing.DrawIcon;
                DrawBatteryIcon();
            }
        }

        private void LogiDevicePropertyChanged(object? s, PropertyChangedEventArgs e)
        {
            if (s is not LogiDevice _logiDevice)
            {
                return;
            }

            if (e.PropertyName == nameof(LogiDevice.ToolTipString))
            {
                _taskbarIcon?.Dispatcher.Invoke(() =>
                {
                    _taskbarIcon.ToolTipText = _logiDevice.ToolTipString;
                });
            }
            else if (e.PropertyName == nameof(LogiDevice.BatteryPercentage))
            {
                DrawBatteryIcon();
            }
        }

        private void DrawBatteryIcon()
        {
            _taskbarIcon?.Dispatcher.BeginInvoke(() => _drawBatteryIcon(_taskbarIcon, _logiDevice));
        }
    }
}
