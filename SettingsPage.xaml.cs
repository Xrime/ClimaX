using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace ProjectX.Pages
{
    public partial class SettingsPage : Page
    {
        private readonly string settingsFile = "settings.json";

        public SettingsPage()
        {
            InitializeComponent();
            LoadSettings();

            RefreshSlider.ValueChanged += RefreshSlider_ValueChanged;
        }

        // 🕒 Update label when slider moves
        private void RefreshSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (RefreshValue != null)
                RefreshValue.Text = $"{(int)RefreshSlider.Value} min";
        }

        // 💾 Save settings
        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            var settings = new AppSettings
            {
                DefaultLocation = DefaultLocationBox.Text,
                TemperatureUnit = (UnitSelector.SelectedItem as ComboBoxItem)?.Content.ToString(),
                RefreshInterval = (int)RefreshSlider.Value,
            };

            File.WriteAllText(settingsFile, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));

            MessageBox.Show("Settings saved successfully!", "✅ Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // 📂 Load settings if file exists
        private void LoadSettings()
        {
            if (File.Exists(settingsFile))
            {
                var json = File.ReadAllText(settingsFile);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);

                if (settings != null)
                {
                    DefaultLocationBox.Text = settings.DefaultLocation;
                    UnitSelector.SelectedIndex = settings.TemperatureUnit?.Contains("Fahrenheit") == true ? 1 : 0;
                    RefreshSlider.Value = settings.RefreshInterval;
                    RefreshValue.Text = $"{settings.RefreshInterval} min";
                }
            }
        }
    }

    // 📌 Model for settings
    public class AppSettings
    {
        public string DefaultLocation { get; set; }
        public string TemperatureUnit { get; set; }
        public int RefreshInterval { get; set; }
        public bool IsDarkMode { get; set; }
    }
}
