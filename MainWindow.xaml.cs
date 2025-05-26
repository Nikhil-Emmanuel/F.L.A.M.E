using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace F.L.A.M.E
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void GunStatus_Click(object sender, RoutedEventArgs e)
        {
            MainDisplay.Children.Clear();
          
            var gunStatusView = new GunStatusView(8);
          
            MainDisplay.Children.Add(gunStatusView);
        }

        private void SO_Click(object sender, RoutedEventArgs e)
        {
            MainDisplay.Children.Clear();
                var shopOverviewView = new ShopOverviewView(8); // You can set any sensor count
            MainDisplay.Children.Add(shopOverviewView); // You can set any sensor count

        }
        private void ReportButton_Click(object sender, RoutedEventArgs e)
        {
            MainDisplay.Children.Clear();

            var ReportView = new ReportView();

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
                Opacity = 0.06,
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
