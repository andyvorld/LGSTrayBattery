using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LGSTrayCore;
using PropertyChanged;

namespace LGSTrayGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainWindowViewModel viewModel;

        public MainWindow()
        {
            InitializeComponent();
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CrashHandler);

            this.TaskbarIcon.Icon = TrayIconTools.ErrorIcon();
            this.viewModel = new MainWindowViewModel(this);
            this.DataContext = viewModel;
            _ = this.viewModel.LoadViewModel();
        }

        private void CrashHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            long unixTime = DateTimeOffset.Now.ToUnixTimeSeconds();

            using (StreamWriter writer = new StreamWriter($"./crashlog_{unixTime}.log", false))
            {
                writer.WriteLine(e.ToString());
            }
        }

        private void ExitButton_OnClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        public static T GetChildOfType<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);

                var result = (child as T) ?? GetChildOfType<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        private void DeviceSelect_OnClick(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            if (mi == null)
            {
                return;
            }

            if (this.viewModel.SelectedDevice != null)
            {
                this.viewModel.SelectedDevice.PropertyChanged -= SelectedDevice_PropertyChanged;
            }

            this.viewModel.SelectedDevice = (LogiDevice)mi.DataContext;
            SelectedDevice_PropertyChanged(this.viewModel.SelectedDevice, null);
            this.viewModel.SelectedDevice.PropertyChanged += SelectedDevice_PropertyChanged;
            e.Handled = true;
        }

        private void SelectedDevice_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            LogiDevice selectedDevice = sender as LogiDevice;
            if (selectedDevice == null)
            {
                return;
            }

            //this.TaskbarIcon.Icon = TrayIconTools.GenerateIcon(selectedDevice);
        }

        private void RescanDevices_OnClick(object sender, RoutedEventArgs e)
        {
            this.viewModel.RescanDevices();
        }
    }
}
