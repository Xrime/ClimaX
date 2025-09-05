using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Newtonsoft.Json.Linq;

namespace ProjectX
{
    public partial class CurrentWeatherPage : Page
    {
        private const string ApiKey = "4cf9e8bfede38c5eebde9a484c29b402";
        private const string BaseUrl = "https://api.openweathermap.org/data/2.5/weather";

        private static readonly HttpClient http = new HttpClient();

        private string DefaultCity = "New York"; // fallback
        private string Unit = "metric";          // metric (°C), imperial (°F)
        private string UnitSymbol = "°C";
        private string WindUnit = "m/s";

        public CurrentWeatherPage()
        {
            InitializeComponent();

            LoadUserSettings();

            PlaceholderText.Visibility = string.IsNullOrEmpty(SearchBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;

            SearchBox.KeyDown += SearchBox_KeyDown;
            Loaded += async (_, __) => await LoadWeatherData(DefaultCity);
        }

        // NEW: constructor to accept a favorite city
        public CurrentWeatherPage(string city) : this()
        {
            Loaded += async (_, __) => await LoadWeatherData(city);
        }

        private void LoadUserSettings()
        {
            try
            {
                string settingsFile = "settings.json";
                if (File.Exists(settingsFile))
                {
                    var json = File.ReadAllText(settingsFile);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);

                    if (settings != null)
                    {
                        if (!string.IsNullOrWhiteSpace(settings.DefaultLocation))
                            DefaultCity = settings.DefaultLocation;

                        if (settings.TemperatureUnit?.ToLower().Contains("fahrenheit") == true)
                        {
                            Unit = "imperial";
                            UnitSymbol = "°F";
                            WindUnit = "mph";
                        }
                        else
                        {
                            Unit = "metric";
                            UnitSymbol = "°C";
                            WindUnit = "m/s";
                        }
                    }
                }
            }
            catch
            {
                // ignore errors, fallback to defaults
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            PlaceholderText.Visibility = string.IsNullOrEmpty(SearchBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private async void SearchIcon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            await TrySearchAsync();
        }

        private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await TrySearchAsync();
            }
        }

        private async Task TrySearchAsync()
        {
            var city = SearchBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(city))
            {
                MessageBox.Show("Please enter a city name.", "Weather", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            await LoadWeatherData(city);
        }

        public async Task LoadWeatherData(string city)
        {
            try
            {
                CityNameText.Text = $"Loading {city}…";
                ResetWeatherUI();

                WeatherIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/weather.png"));

                var url = $"{BaseUrl}?q={Uri.EscapeDataString(city)}&appid={ApiKey}&units={Unit}";
                var json = await http.GetStringAsync(url);
                var data = JObject.Parse(json);

                if (data["cod"]?.ToString() != "200")
                {
                    var msg = data["message"]?.ToString() ?? "Unknown error";
                    throw new Exception(msg);
                }

                string cityName = data["name"]?.ToString() ?? city;
                string country = data["sys"]?["country"]?.ToString() ?? "";
                double temp = SafeDouble(data["main"]?["temp"], double.NaN);
                double feelsLike = SafeDouble(data["main"]?["feels_like"], double.NaN);
                int humidity = SafeInt(data["main"]?["humidity"], 0);
                int pressure = SafeInt(data["main"]?["pressure"], 0);
                double windSpeed = SafeDouble(data["wind"]?["speed"], 0);
                int visibilityM = SafeInt(data["visibility"], 0);

                string description = data["weather"]?[0]?["description"]?.ToString() ?? "";
                string condition = data["weather"]?[0]?["main"]?.ToString() ?? "";

                long sunriseUtc = SafeLong(data["sys"]?["sunrise"], 0);
                long sunsetUtc = SafeLong(data["sys"]?["sunset"], 0);
                int tzOffset = SafeInt(data["timezone"], 0);

                CityNameText.Text = string.IsNullOrEmpty(country) ? cityName : $"{cityName}, {country}";
                TemperatureText.Text = double.IsNaN(temp) ? "" : $"{Math.Round(temp)}{UnitSymbol}";
                DescriptionText.Text = ToTitleCase(description);
                FeelsLikeText.Text = double.IsNaN(feelsLike) ? "" : $"Feels like {Math.Round(feelsLike)}{UnitSymbol}";

                HumidityText.Text = humidity > 0 ? $"{humidity}%" : "";
                WindText.Text = $"{windSpeed:0.##} {WindUnit}";
                VisibilityText.Text = visibilityM > 0 ? $"{visibilityM / 1000.0:0.#} km" : "";
                PressureText.Text = pressure > 0 ? $"{pressure} hPa" : "";

                SunriseText.Text = UnixToLocalTimeString(sunriseUtc, tzOffset);
                SunsetText.Text = UnixToLocalTimeString(sunsetUtc, tzOffset);

                WeatherIcon.Source = new BitmapImage(new Uri(IconFor(condition, description)));
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Network error: {ex.Message}", "Weather", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load weather for \"{city}\": {ex.Message}", "Weather", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetWeatherUI()
        {
            TemperatureText.Text = "";
            DescriptionText.Text = "";
            FeelsLikeText.Text = "";
            HumidityText.Text = "";
            WindText.Text = "";
            VisibilityText.Text = "";
            PressureText.Text = "";
            SunriseText.Text = "";
            SunsetText.Text = "";
        }

        private static string ToTitleCase(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return s;
            var parts = s.Split(' ');
            for (int i = 0; i < parts.Length; i++)
                parts[i] = char.ToUpper(parts[i][0]) + (parts[i].Length > 1 ? parts[i][1..] : "");
            return string.Join(" ", parts);
        }

        private static string UnixToLocalTimeString(long unixUtc, int tzOffsetSeconds)
        {
            if (unixUtc <= 0) return "";
            var dt = DateTimeOffset.FromUnixTimeSeconds(unixUtc).ToOffset(TimeSpan.FromSeconds(tzOffsetSeconds));
            return dt.ToString("hh:mm tt");
        }

        private static double SafeDouble(JToken token, double fallback) =>
            token != null && double.TryParse(token.ToString(), out var v) ? v : fallback;

        private static int SafeInt(JToken token, int fallback) =>
            token != null && int.TryParse(token.ToString(), out var v) ? v : fallback;

        private static long SafeLong(JToken token, long fallback) =>
            token != null && long.TryParse(token.ToString(), out var v) ? v : fallback;

        private static string IconFor(string conditionMain, string description)
        {
            string c = (conditionMain ?? "").Trim().ToLower();
            string d = (description ?? "").Trim().ToLower();

            return c switch
            {
                "clear" => "pack://application:,,,/Assets/sun.png",
                "clouds" => "pack://application:,,,/Assets/cloud.png",
                "rain" or "drizzle" => "pack://application:,,,/Assets/rain.png",
                "thunderstorm" => "pack://application:,,,/Assets/storm.png",
                "snow" => "pack://application:,,,/Assets/snow.png",
                "mist" or "fog" or "haze" or "smoke" => "pack://application:,,,/Assets/fog.png",
                "dust" or "sand" or "ash" => "pack://application:,,,/Assets/fog.png",
                "squall" or "tornado" => "pack://application:,,,/Assets/storm.png",
                _ => d.Contains("rain") ? "pack://application:,,,/Assets/rain.png" :
                     d.Contains("cloud") ? "pack://application:,,,/Assets/cloud.png" :
                     d.Contains("clear") ? "pack://application:,,,/Assets/sun.png" :
                     "pack://application:,,,/Assets/weather.png"
            };
        }
    }

    // ✅ Shared settings model (same as SettingsPage)
    public class AppSettings
    {
        public string DefaultLocation { get; set; }
        public string TemperatureUnit { get; set; }
        public int RefreshInterval { get; set; }
        public bool IsDarkMode { get; set; }
    }
}














