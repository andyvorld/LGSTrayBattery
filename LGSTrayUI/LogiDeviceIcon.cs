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
    public class LogiDeviceIcon : IDisposable
    {
        #region IDisposable
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _taskbarIcon.Dispose();
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

        public LogiDeviceIcon(LogiDevice device)
        {
            _taskbarIcon = new()
            {
                DataContext = device,
                ToolTipText = device.ToolTipString
            };
            _taskbarIcon.ContextMenu = (ContextMenu)_taskbarIcon.FindResource("SysTrayMenu");

            _logiDevice = device;
            _logiDevice.PropertyChanged += LogiDevicePropertyChanged;
        }

        private void LogiDevicePropertyChanged(object? s, PropertyChangedEventArgs e)
        {
            if (s is not LogiDevice _logiDevice)
            {
                return;
            }

            if (e.PropertyName == nameof(LogiDevice.ToolTipString))
            {
                _taskbarIcon.Dispatcher.Invoke(() =>
                {
                    _taskbarIcon.ToolTipText = _logiDevice.ToolTipString;
                });
            }
            else if (e.PropertyName == nameof(LogiDevice.BatteryPercentage))
            {
                _taskbarIcon.Dispatcher.Invoke(() =>
                {
                    BatteryIconDrawing.DrawNumeric(_taskbarIcon, _logiDevice);
                });
            }
        }
    }
}
