using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ProjectX.Pages
{
    public partial class ForecastWeatherPage : Page
    {
        private const string ApiKey = "4cf9e8bfede38c5eebde9a484c29b402"; // <-- replace with your OpenWeather key
        private readonly HttpClient _http = new HttpClient();

        public ForecastWeatherPage()
        {
            InitializeComponent();
            Loaded += ForecastWeatherPage_Loaded;
        }

        private async void ForecastWeatherPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadForecastAsync("New York"); // default city
        }

        // UI handlers
        private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                await TrySearchAsync();
        }

        private async void SearchIcon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await TrySearchAsync();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            PlaceholderText.Visibility = string.IsNullOrWhiteSpace(SearchBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private async System.Threading.Tasks.Task TrySearchAsync()
        {
            var city = SearchBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(city))
            {
                MessageBox.Show("Please enter a city name.", "Search", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            await LoadForecastAsync(city);
        }

        // Main loader: calls OpenWeather 5-day / 3-hour forecast
        private async System.Threading.Tasks.Task LoadForecastAsync(string city)
        {
            try
            {
                CityNameText.Text = $"Loading {city}...";

                string url = $"https://api.openweathermap.org/data/2.5/forecast?q={Uri.EscapeDataString(city)}&appid={ApiKey}&units=metric";
                string json = await _http.GetStringAsync(url);
                var root = JObject.Parse(json);

                // city info
                string cityName = root["city"]?["name"]?.ToString() ?? city;
                string country = root["city"]?["country"]?.ToString() ?? "";
                CityNameText.Text = string.IsNullOrEmpty(country) ? cityName : $"{cityName}, {country}";

                // clear previous items
                HourlyPanel.Children.Clear();
                DailyPanel.Children.Clear();

                var items = root["list"]?.ToObject<List<JObject>>() ?? new List<JObject>();

                // Hourly: take next ~8 entries (about 24 hours). Filter to future times.
                var upcoming = items
                    .Select(i => new
                    {
                        Dt = DateTime.Parse(i["dt_txt"].ToString()),
                        Temp = Math.Round(i["main"]?["temp"]?.ToObject<double>() ?? 0),
                        Feels = Math.Round(i["main"]?["feels_like"]?.ToObject<double>() ?? 0),
                        Humidity = i["main"]?["humidity"]?.ToObject<int>() ?? 0,
                        Wind = i["wind"]?["speed"]?.ToObject<double>() ?? 0,
                        IconCode = i["weather"]?[0]?["icon"]?.ToString() ?? ""
                    })
                    .Where(x => x.Dt > DateTime.Now)
                    .Take(8)
                    .ToList();

                foreach (var h in upcoming)
                {
                    var exp = CreateHourlyExpander(h.Dt, (int)h.Temp, (int)h.Feels, h.Humidity, h.Wind, h.IconCode);
                    // spacing between horizontal items
                    exp.Margin = new Thickness(0, 0, 12, 0);
                    HourlyPanel.Children.Add(exp);
                }

                // Daily: group by date, show up to 7 days
                var dailyGroups = items
                    .GroupBy(i => DateTime.Parse(i["dt_txt"].ToString()).Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        AvgTemp = Math.Round(g.Average(x => (double)x["main"]["temp"])),
                        High = Math.Round(g.Max(x => (double)x["main"]["temp"])),
                        Low = Math.Round(g.Min(x => (double)x["main"]["temp"])),
                        HumidityAvg = Math.Round(g.Average(x => (double)x["main"]["humidity"])),
                        WindAvg = Math.Round(g.Average(x => (double)x["wind"]["speed"]), 1),
                        IconCode = g.First()["weather"]?[0]?["icon"]?.ToString() ?? ""
                    })
                    .Take(7)
                    .ToList();

                foreach (var d in dailyGroups)
                {
                    var exp = CreateDailyExpander(d.Date, (int)d.AvgTemp, (int)d.High, (int)d.Low, (int)d.HumidityAvg, d.WindAvg, d.IconCode);
                    DailyPanel.Children.Add(exp);
                }
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Network error: {ex.Message}", "Forecast", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load forecast for \"{city}\": {ex.Message}", "Forecast", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // build hourly expander card
        private Expander CreateHourlyExpander(DateTime dt, int temp, int feels, int humidity, double wind, string iconCode)
        {
            // header stack
            var headerStack = new StackPanel { Orientation = Orientation.Vertical, HorizontalAlignment = HorizontalAlignment.Center };
            headerStack.Children.Add(new TextBlock { Text = dt.ToString("HH:mm"), FontSize = 14, Foreground = Brushes.Black, HorizontalAlignment = HorizontalAlignment.Center });
            headerStack.Children.Add(new Image
            {
                Source = new BitmapImage(new Uri(GetIconUri(iconCode), UriKind.Absolute)),
                Width = 44,
                Height = 44,
                Margin = new Thickness(0, 6, 0, 6)
            });
            headerStack.Children.Add(new TextBlock { Text = $"{temp}°C", FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = Brushes.Black, HorizontalAlignment = HorizontalAlignment.Center });

            var headerBorder = new Border { Style = (Style)TryFindResource("CardBorderStyle"), Width = 110, Height = 150, Child = headerStack };

            // detail content
            var detailPanel = new StackPanel { Orientation = Orientation.Vertical };
            detailPanel.Children.Add(new TextBlock { Text = $"Feels like: {feels}°C", Margin = new Thickness(0, 2, 0, 2), Foreground = Brushes.Black });
            detailPanel.Children.Add(new TextBlock { Text = $"Humidity: {humidity}%", Margin = new Thickness(0, 2, 0, 2), Foreground = Brushes.Black });
            detailPanel.Children.Add(new TextBlock { Text = $"Wind: {wind} m/s", Margin = new Thickness(0, 2, 0, 2), Foreground = Brushes.Black });

            var detailBorder = new Border { Style = (Style)TryFindResource("ContentBorderStyle"), Child = detailPanel };

            var exp = new Expander
            {
                Header = headerBorder,
                Content = detailBorder,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Width = 120
            };

            return exp;
        }

        // build daily expander card
        private Expander CreateDailyExpander(DateTime date, int avgTemp, int high, int low, int humidityAvg, double windAvg, string iconCode)
        {
            var headerStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            var dayText = new TextBlock { Text = date.ToString("dddd"), FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = Brushes.Black, Width = 140 };
            var icon = new Image
            {
                Source = new BitmapImage(new Uri(GetIconUri(iconCode), UriKind.Absolute)),
                Width = 36,
                Height = 36,
                Margin = new Thickness(6, 0, 10, 0)
            };
            var tempText = new TextBlock { Text = $"{avgTemp}°C", FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = Brushes.Black };

            headerStack.Children.Add(dayText);
            headerStack.Children.Add(icon);
            headerStack.Children.Add(tempText);

            var headerBorder = new Border { Style = (Style)TryFindResource("CardBorderStyle"), Child = headerStack };

            var details = new StackPanel();
            details.Children.Add(new TextBlock { Text = $"High: {high}°C", Margin = new Thickness(0, 2, 0, 2), Foreground = Brushes.Black });
            details.Children.Add(new TextBlock { Text = $"Low: {low}°C", Margin = new Thickness(0, 2, 0, 2), Foreground = Brushes.Black });
            details.Children.Add(new TextBlock { Text = $"Humidity: {humidityAvg}%", Margin = new Thickness(0, 2, 0, 2), Foreground = Brushes.Black });
            details.Children.Add(new TextBlock { Text = $"Wind: {windAvg} m/s", Margin = new Thickness(0, 2, 0, 2), Foreground = Brushes.Black });

            var contentBorder = new Border { Style = (Style)TryFindResource("ContentBorderStyle"), Child = details };

            var exp = new Expander
            {
                Header = headerBorder,
                Content = contentBorder,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 6, 0, 6)
            };

            return exp;
        }

        // Map OpenWeather icon code to local pack-URI assets
        private string GetIconUri(string iconCode)
        {
            if (string.IsNullOrEmpty(iconCode)) return "pack://application:,,,/Assets/weather.png";

            if (iconCode.StartsWith("01")) return "pack://application:,,,/Assets/sun.png";
            if (iconCode.StartsWith("02") || iconCode.StartsWith("03") || iconCode.StartsWith("04")) return "pack://application:,,,/Assets/cloud.png";
            if (iconCode.StartsWith("09") || iconCode.StartsWith("10")) return "pack://application:,,,/Assets/rain.png";
            if (iconCode.StartsWith("11")) return "pack://application:,,,/Assets/storm.png";
            if (iconCode.StartsWith("13")) return "pack://application:,,,/Assets/snow.png";
            if (iconCode.StartsWith("50")) return "pack://application:,,,/Assets/fog.png";

            return "pack://application:,,,/Assets/weather.png";
        }
    }
}
