using System.Windows;

namespace F.L.A.M.E
{
    public partial class App : Application
    {
        private BackgroundLogger _logger;
        private MockGunDataProvider _provider;
    
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            new LoginWindow().Show(); // Show the login window first
            _provider = new MockGunDataProvider();
            _provider.Start(); // Start generating mock data

            _logger = new BackgroundLogger(_provider);
            _logger.Start(); // Start logging in background
        }
        protected override void OnExit(ExitEventArgs e)
        {
            _logger?.Stop();
            base.OnExit(e);
        }
    }
}
