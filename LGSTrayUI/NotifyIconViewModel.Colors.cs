using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace LGSTrayUI;

public partial class NotifyIconViewModel
{
    private NumericColorsWindow? _colorsWindow;

    [RelayCommand]
    private void OpenNumericColors()
    {
        // Let the tray popup finish releasing mouse capture before showing a real window.
        ((ContextMenu)Application.Current.FindResource("SysTrayMenu")).IsOpen = false;
        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
        {
            if (_colorsWindow is null)
            {
                _colorsWindow = new NumericColorsWindow(_userSettings, LogiDevices);
                _colorsWindow.Closed += (_, _) => _colorsWindow = null;
                _colorsWindow.Show();
            }
            if (_colorsWindow.WindowState == WindowState.Minimized) _colorsWindow.WindowState = WindowState.Normal;
            _colorsWindow.Activate();
        }));
    }
}
