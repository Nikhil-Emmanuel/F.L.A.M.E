using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace F.L.A.M.E
{
    public partial class SettingsView : UserControl
    {
        private readonly string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public SettingsView()
        {
            InitializeComponent();
            LoadSettings();
        }

        public static int GunsCount
        {
            get
            {
                var settings = LoadAppSettings();
                return settings?.GunCount ?? 8;
            }
        }

        public static string IPaddress
        {
            get
            {
                var settings = LoadAppSettings();
                return settings?.IpAddress ?? "192.168.1.1";
            }
        }

        private void LoadSettings()
        {
            var settings = LoadAppSettings();
            GunCountTextBox.Text = settings?.GunCount.ToString() ?? "8";
            IpAddressTextBox.Text = settings?.IpAddress ?? "192.168.1.1";
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(GunCountTextBox.Text, out int gunCount))
            {
                var settings = new AppSettings
                {
                    GunCount = gunCount,
                    IpAddress = IpAddressTextBox.Text
                };

                File.WriteAllText(settingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
                MessageBox.Show("Settings saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Invalid gun count entered.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static AppSettings? LoadAppSettings()
        {
            if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json")))
                return null;

            var json = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json"));
            return JsonSerializer.Deserialize<AppSettings>(json);
        }

        private void IpAddressTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }

    public class AppSettings
    {
        public int GunCount { get; set; } = 8;
        public string IpAddress { get; set; } = "192.168.1.1";
    }
}
