using ProjectX.Pages;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;

namespace ProjectX
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Load default page
            MainFrame.Navigate(new CurrentWeatherPage());
        }

        private void CurrentWeather_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new CurrentWeatherPage());
        }

        private void Forecast_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ForecastWeatherPage());
        }

        private void Favorites_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new FavoritesPage());
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new SettingsPage());
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new AboutPage());
        }
    }
}
