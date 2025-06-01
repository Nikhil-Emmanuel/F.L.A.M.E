using System.Threading;
using System.Windows;

namespace F.L.A.M.E
{
    public partial class App : Application
    {
        public static PlcReader PlcReaderInstance => PlcReader.SharedInstance;

        private BackgroundLogger? _logger;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            PlcReaderInstance.StartMockPolling();

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
