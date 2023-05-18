using Hardcodet.Wpf.TaskbarNotification;
using LGSTrayUI.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LGSTrayUI
{
    public class MainTaskbarIconWrapper : IDisposable
    {
        private readonly TaskbarIcon _taskbarIcon = (TaskbarIcon)Application.Current.FindResource("NotifyIcon");
        private bool disposedValue;

        public MainTaskbarIconWrapper()
        {
            BatteryIconDrawing.DrawUnknown(_taskbarIcon);
            LogiDeviceIcon.RefCountChanged += RefCountChanged;
            _RefCountChanged(LogiDeviceIcon.RefCount);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _taskbarIcon.Dispose();
                    LogiDeviceIcon.RefCountChanged -= RefCountChanged;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~MainTaskbarIconWrapper()
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

        private void _RefCountChanged(int refCount)
        {
            _taskbarIcon.Visibility = (refCount == 0) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void RefCountChanged(object? s, PropertyChangedEventArgs _)
        {
            if (s is not int refCount)
            {
                return;
            }

            _RefCountChanged(refCount);
        }
    }
}
