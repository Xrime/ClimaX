using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ProjectX.Pages
{
    public partial class FavoritesPage : Page
    {
        private readonly string filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProjectX", "favorites.json");

        private List<string> favoriteCities = new();

        public FavoritesPage()
        {
            InitializeComponent();
            LoadFavorites();
            DisplayFavorites();
        }

        private void AddFavorite_Click(object sender, MouseButtonEventArgs e)
        {
            AddCityFromInput();
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) AddCityFromInput();
        }

        private void AddCityFromInput()
        {
            string city = SearchBox.Text.Trim();
            if (!string.IsNullOrEmpty(city) && !favoriteCities.Contains(city))
            {
                favoriteCities.Add(city);
                SaveFavorites();
                DisplayFavorites();
                SearchBox.Clear();
            }
        }

        private void SaveFavorites()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            string json = JsonSerializer.Serialize(favoriteCities, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        private void LoadFavorites()
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                favoriteCities = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
        }

        private void DisplayFavorites()
        {
            FavoritesPanel.Children.Clear();

            foreach (var city in favoriteCities)
            {
                Border card = new Border
                {
                    Style = (Style)FindResource("CardBorderStyle"),
                    Cursor = Cursors.Hand
                };

                DockPanel content = new DockPanel();

                Image weatherIcon = new Image
                {
                    Source = new BitmapImage(new Uri("/Assets/city.png", UriKind.Relative)),
                    Width = 30,
                    Height = 30,
                    Margin = new Thickness(0, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                TextBlock cityText = new TextBlock
                {
                    Text = city,
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Button removeBtn = new Button
                {
                    Content = "➖",
                    Foreground = System.Windows.Media.Brushes.Red,
                    Background = System.Windows.Media.Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    FontSize = 16,
                    Padding = new Thickness(4),
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                removeBtn.Click += (s, e) =>
                {
                    favoriteCities.Remove(city);
                    SaveFavorites();
                    DisplayFavorites();
                };

                DockPanel.SetDock(removeBtn, Dock.Right);
                DockPanel.SetDock(weatherIcon, Dock.Left);

                content.Children.Add(weatherIcon);
                content.Children.Add(cityText);
                content.Children.Add(removeBtn);

                card.Child = content;

                // Navigate to CurrentWeatherPage with city when card is clicked
                card.MouseLeftButtonUp += (s, e) =>
                {
                    NavigationService?.Navigate(new ProjectX.CurrentWeatherPage(city));
                };

                FavoritesPanel.Children.Add(card);
            }
        }

        private void ClearAllFavorites_Click(object sender, RoutedEventArgs e)
        {
            favoriteCities.Clear();
            SaveFavorites();
            DisplayFavorites();
        }
    }
}
