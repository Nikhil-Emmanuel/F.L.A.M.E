using System.Threading;
using System.Windows;

namespace F.L.A.M.E
{
    public partial class App : Application
    {
        public static PlcReader PlcReaderInstance => PlcReader.SharedInstance;

        private BackgroundLogger? _logger;
        private readonly CancellationTokenSource _cts = new();
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Start PLC reader (singleton)
            _ = PlcReader.SharedInstance.StartPollingAsync(_cts.Token);
            MessageBox.Show("PLC Reader started. Please wait for the connection to be established.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);

            // Start background logger with the same PlcReader instance
            _logger = new BackgroundLogger(PlcReader.SharedInstance);
            _logger.Start();

            // Show login window
            new LoginWindow().Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _cts.Cancel();
            _logger?.Stop();
            base.OnExit(e);
        }
    }
}
