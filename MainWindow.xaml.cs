using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace F.L.A.M.E
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _plcStatusTimer = new DispatcherTimer();
        public MainWindow()
        {
            InitializeComponent();
            _plcStatusTimer.Interval = TimeSpan.FromSeconds(2);
            _plcStatusTimer.Tick += CheckPlcStatus;
            _plcStatusTimer.Start();
        }

        private void StartServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                string batchPath = Path.Combine(exeDir, "StartServer.bat");

                if (!File.Exists(batchPath))
                {
                    MessageBox.Show("StartServer.bat not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = batchPath,
                    Verb = "runas", // This runs as administrator
                    UseShellExecute = true,
                    WorkingDirectory = exeDir
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to start server:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                string batchPath = Path.Combine(exeDir, "StopServer.bat");

                if (!File.Exists(batchPath))
                {
                    MessageBox.Show("StopServer.bat not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = batchPath,
                    Verb = "runas", // This runs as administrator
                    UseShellExecute = true,
                    WorkingDirectory = exeDir
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to stop server:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CheckPlcStatus(object? sender, EventArgs e)
        {
            bool isConnected = App.PlcReaderInstance?.IsConnected == true;

            if (isConnected)
            {
                PlcStatusDot.Fill = Brushes.Green;
                PlcStatusText.Text = "PLC Status: Connected";
            }
            else
            {
                PlcStatusDot.Fill = Brushes.Red;
                PlcStatusText.Text = "PLC Status: Disconnected";
            }
        }

        private void GunStatus_Click(object sender, RoutedEventArgs e)
        {
            MainDisplay.Children.Clear();

            var gunStatusView = new GunStatusView(SettingsView.GunsCount);

            MainDisplay.Children.Add(gunStatusView);
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MainDisplay.Children.Clear();

            var SettingsView = new SettingsView();

            MainDisplay.Children.Add(SettingsView);
        }

        private void SO_Click(object sender, RoutedEventArgs e)
        {
            MainDisplay.Children.Clear();
            var shopOverviewView = new ShopOverviewView(SettingsView.GunsCount); // You can set any sensor count
            MainDisplay.Children.Add(shopOverviewView); // You can set any sensor count

        }
        private void ReportButton_Click(object sender, RoutedEventArgs e)
        {
            MainDisplay.Children.Clear();

            var ReportView = new ReportView(SettingsView.GunsCount);

            MainDisplay.Children.Add(ReportView);
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            // Clear all content in main display
            MainDisplay.Children.Clear();

            // Create the watermark image
            Image watermark = new Image
            {
                Source = new BitmapImage(new Uri("pack://application:,,,/Assets/flame.png")),
                Opacity = 0.2,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };

            // Add it to the MainDisplay (Grid or other Panel)
            MainDisplay.Children.Add(watermark);
        }
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            // Show login window again
            LoginWindow login = new();
            login.Show();
            // Close this window
            this.Close();
        }
    }
}
