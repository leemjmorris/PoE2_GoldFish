using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using PoE2Overlay.Core;
using PoE2Overlay.Features.Trade.Models;
using PoE2Overlay.Features.Trade.Services;

namespace PoE2Overlay.Features.Trade
{
    public partial class TradeOverlay : Window
    {
        private readonly TradeApiClient _apiClient = new();
        private readonly StatIdResolver _statResolver = new();
        private ParsedItem _currentItem;
        private readonly List<ModFilterControl> _modFilterControls = new();

        public TradeOverlay()
        {
            InitializeComponent();
            LoadState();
            LeagueInput.Text = AppSettings.Instance.TradeLeague;

            _ = _statResolver.LoadStatsAsync();
        }

        public void ProcessClipboardText(string text)
        {
            var parsed = ItemParser.Parse(text);
            if (parsed == null || !parsed.IsValid)
                return;

            _currentItem = parsed;
            DisplayItem(parsed);
            if (!IsVisible)
            {
                Show();
            }
        }

        private void DisplayItem(ParsedItem item)
        {
            ItemNameText.Text = item.Name ?? "";
            ItemBaseText.Text = item.BaseType ?? "";
            ItemLevelText.Text = item.ItemLevel.HasValue
                ? $"Item Level: {item.ItemLevel}" : "";

            BuildModFilters(item);

            ResultsList.ItemsSource = null;
            ResultSummaryText.Text = "";
            MedianPriceText.Text = "";
            StatusText.Text = "Item loaded. Adjust filters and click Search.";
        }

        private void BuildModFilters(ParsedItem item)
        {
            ModFiltersPanel.Children.Clear();
            _modFilterControls.Clear();

            if (item.ImplicitMods.Count > 0)
            {
                AddSectionHeader("Implicit");
                foreach (var mod in item.ImplicitMods)
                    AddModFilter(mod);
            }

            if (item.ExplicitMods.Count > 0)
            {
                AddSectionHeader("Explicit");
                foreach (var mod in item.ExplicitMods)
                    AddModFilter(mod);
            }
        }

        private void AddSectionHeader(string title)
        {
            ModFiltersPanel.Children.Add(new TextBlock
            {
                Text = title,
                Foreground = FindResource("AccentBrush") as Brush,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 6, 0, 2)
            });
        }

        private void AddModFilter(ItemMod mod)
        {
            var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });

            var checkbox = new CheckBox
            {
                Content = mod.RawText,
                IsChecked = mod.IsEnabled,
                Foreground = FindResource("TextBrush") as Brush,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(checkbox, 0);

            double? defaultMin = null, defaultMax = null;
            if (mod.Value.HasValue)
            {
                var (min, max) = ItemParser.CalculateRange(mod.Value.Value, 10);
                defaultMin = min;
                defaultMax = max;
            }

            var minBox = new TextBox
            {
                Text = defaultMin?.ToString() ?? "",
                Width = 45,
                FontSize = 11,
                Background = new SolidColorBrush(Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF)),
                Foreground = FindResource("TextBrush") as Brush,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(2)
            };
            Grid.SetColumn(minBox, 1);

            var tilde = new TextBlock
            {
                Text = " ~ ",
                Foreground = FindResource("TextBrush") as Brush,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 11
            };
            Grid.SetColumn(tilde, 2);

            var maxBox = new TextBox
            {
                Text = defaultMax?.ToString() ?? "",
                Width = 45,
                FontSize = 11,
                Background = new SolidColorBrush(Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF)),
                Foreground = FindResource("TextBrush") as Brush,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(2)
            };
            Grid.SetColumn(maxBox, 3);

            grid.Children.Add(checkbox);
            grid.Children.Add(minBox);
            grid.Children.Add(tilde);
            grid.Children.Add(maxBox);

            ModFiltersPanel.Children.Add(grid);

            _modFilterControls.Add(new ModFilterControl
            {
                Mod = mod,
                CheckBox = checkbox,
                MinBox = minBox,
                MaxBox = maxBox
            });
        }

        private async void OnSearchClick(object sender, RoutedEventArgs e)
        {
            if (_currentItem == null) return;

            StatusText.Text = "Searching...";

            AppSettings.Instance.TradeLeague = LeagueInput.Text;
            AppSettings.Instance.Save();

            var request = BuildSearchRequest();
            var league = LeagueInput.Text;

            try
            {
                var result = await _apiClient.SearchAndFetchAsync(request, league);

                if (!string.IsNullOrEmpty(result.Error))
                {
                    StatusText.Text = result.Error;
                    return;
                }

                DisplayResults(result);
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
            }
        }

        private TradeSearchRequest BuildSearchRequest()
        {
            var request = new TradeSearchRequest();

            if (_currentItem.Rarity == ItemRarity.Unique &&
                !string.IsNullOrEmpty(_currentItem.Name))
            {
                request.Query.Name = _currentItem.Name;
                request.Query.Type = _currentItem.BaseType;
            }
            else
            {
                request.Query.Type = _currentItem.BaseType;
            }

            var statFilters = new StatFilterGroup { Type = "and" };

            foreach (var control in _modFilterControls)
            {
                if (control.CheckBox.IsChecked != true) continue;

                var statId = _statResolver.Resolve(
                    control.Mod.RawText, control.Mod.Type);

                if (statId == null) continue;

                var filter = new StatFilter
                {
                    Id = statId,
                    Disabled = false
                };

                double.TryParse(control.MinBox.Text, out double minVal);
                double.TryParse(control.MaxBox.Text, out double maxVal);

                if (minVal > 0 || maxVal > 0)
                {
                    filter.Value = new StatFilterValue
                    {
                        Min = minVal > 0 ? minVal : (double?)null,
                        Max = maxVal > 0 ? maxVal : (double?)null
                    };
                }

                statFilters.Filters.Add(filter);
            }

            if (statFilters.Filters.Count > 0)
                request.Query.Stats.Add(statFilters);
            else
                request.Query.Stats.Add(new StatFilterGroup { Type = "and" });

            return request;
        }

        private void DisplayResults(TradeResult result)
        {
            StatusText.Text = $"Found {result.TotalCount} results";
            ResultSummaryText.Text = $"Showing {result.Items.Count} of {result.TotalCount}";

            var prices = result.Items
                .Where(i => i.Listing?.Price != null)
                .Select(i => i.Listing.Price.Amount)
                .OrderBy(p => p)
                .ToList();

            if (prices.Count > 0)
            {
                double median = prices.Count % 2 == 0
                    ? (prices[prices.Count / 2 - 1] + prices[prices.Count / 2]) / 2
                    : prices[prices.Count / 2];
                var currency = result.Items
                    .First(i => i.Listing?.Price != null)
                    .Listing.Price.Currency;
                MedianPriceText.Text = $"Median: {median:F1} {currency}";
            }
            else
            {
                MedianPriceText.Text = "No price data";
            }

            var displayItems = result.Items
                .Where(i => i.Listing?.Price != null)
                .Select(i => new
                {
                    AccountName = i.Listing.Account?.Name ?? "Unknown",
                    PriceText = $"{i.Listing.Price.Amount:F1} {i.Listing.Price.Currency}"
                })
                .ToList();

            ResultsList.ItemsSource = displayItems;
        }

        // === 공통 오버레이 메서드 ===

        private void LoadState()
        {
            var s = AppSettings.Instance;
            Left = s.TradeWindowLeft;
            Top = s.TradeWindowTop;
            Width = s.TradeWindowWidth;
            Height = s.TradeWindowHeight;
            ClampToScreen();
        }

        private void ClampToScreen()
        {
            var screenW = SystemParameters.VirtualScreenWidth;
            var screenH = SystemParameters.VirtualScreenHeight;
            var screenL = SystemParameters.VirtualScreenLeft;
            var screenT = SystemParameters.VirtualScreenTop;

            if (Left < screenL) Left = screenL;
            if (Top < screenT) Top = screenT;
            if (Left + Width > screenL + screenW) Left = screenL + screenW - Width;
            if (Top + Height > screenT + screenH) Top = screenT + screenH - Height;
        }

        private void SaveState()
        {
            var s = AppSettings.Instance;
            s.TradeWindowLeft = Left;
            s.TradeWindowTop = Top;
            s.TradeWindowWidth = Width;
            s.TradeWindowHeight = Height;
            s.Save();
        }

        public void Toggle()
        {
            if (IsVisible)
            {
                SaveState();
                Hide();
            }
            else
            {
                Show();
            }
        }

        private void OnTitleBarDrag(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            SaveState();
            Hide();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            SaveState();
            Hide();
        }

        private void OnResizeDrag(object sender, DragDeltaEventArgs e)
        {
            double newW = Width + e.HorizontalChange;
            double newH = Height + e.VerticalChange;
            if (newW >= MinWidth) Width = newW;
            if (newH >= MinHeight) Height = newH;
        }

    }

    internal class ModFilterControl
    {
        public ItemMod Mod { get; set; }
        public CheckBox CheckBox { get; set; }
        public TextBox MinBox { get; set; }
        public TextBox MaxBox { get; set; }
    }
}
