using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
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
using PropertyChanged;

namespace LGSTrayBattery
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainWindowViewModel viewModel = new MainWindowViewModel();

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = viewModel;

            this.TaskbarIcon.Icon = LGSTrayBattery.Properties.Resources.Discovery;

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler((s,e) => viewModel.LoadViewModel().Wait());
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler((s, e) =>
                {
                    TaskbarIcon.Icon = LGSTrayBattery.Properties.Resources.Unknown;
                    viewModel.LoadLastSelected();
                });

            worker.RunWorkerAsync();
        }

        private void ExitButton_OnClick(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void DeviceSelect_OnClick(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem) sender;

            viewModel.UpdateSelectedDevice((LogiDevice) menuItem.DataContext);
        }

        private void PollIntervalSelect_OnClick(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;

            viewModel.UpdateSelectedPollInterval((PollInterval)menuItem.DataContext);
        }
    }
}
