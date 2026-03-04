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
        private readonly TradeApiClient _apiClient;
        private readonly StatIdResolver _statResolver;
        private ParsedItem _currentItem;
        private readonly List<ModFilterControl> _modFilterControls = new();
        private string _lastQueryId;
        private string _lastLeague;

        public TradeOverlay(TradeApiClient apiClient, StatIdResolver statResolver)
        {
            _apiClient = apiClient;
            _statResolver = statResolver;

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
            CorruptedText.Visibility = item.IsCorrupted
                ? Visibility.Visible : Visibility.Collapsed;

            BuildModFilters(item);

            ResultsList.ItemsSource = null;
            ResultSummaryText.Text = "";
            MedianPriceText.Text = "";
            OpenBrowserButton.Visibility = Visibility.Collapsed;
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

            SearchButton.IsEnabled = false;
            StatusText.Text = "Searching...";

            try
            {
                if (!_statResolver.IsLoaded)
                {
                    StatusText.Text = "Loading stat data...";
                    await _statResolver.EnsureLoadedAsync();
                    if (!_statResolver.IsLoaded)
                    {
                        StatusText.Text = "Failed to load stat data. Check your internet connection.";
                        return;
                    }
                }

                AppSettings.Instance.TradeLeague = LeagueInput.Text;
                AppSettings.Instance.Save();

                var request = BuildSearchRequest();
                var league = LeagueInput.Text;

                var result = await _apiClient.SearchAndFetchAsync(request, league);

                if (!string.IsNullOrEmpty(result.Error))
                {
                    StatusText.Text = result.Error;
                    return;
                }

                _lastQueryId = result.QueryId;
                _lastLeague = league;
                DisplayResults(result);
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
            }
            finally
            {
                SearchButton.IsEnabled = true;
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

                bool hasMin = double.TryParse(control.MinBox.Text, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double minVal)
                    && !string.IsNullOrWhiteSpace(control.MinBox.Text);
                bool hasMax = double.TryParse(control.MaxBox.Text, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double maxVal)
                    && !string.IsNullOrWhiteSpace(control.MaxBox.Text);

                if (hasMin || hasMax)
                {
                    filter.Value = new StatFilterValue
                    {
                        Min = hasMin ? minVal : (double?)null,
                        Max = hasMax ? maxVal : (double?)null
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

            var pricesByCurrency = result.Items
                .Where(i => i.Listing?.Price != null)
                .GroupBy(i => i.Listing.Price.Currency)
                .OrderByDescending(g => g.Count())
                .ToList();

            if (pricesByCurrency.Count > 0)
            {
                var medianParts = new List<string>();
                foreach (var group in pricesByCurrency)
                {
                    var sorted = group.Select(i => i.Listing.Price.Amount).OrderBy(p => p).ToList();
                    double median = sorted.Count % 2 == 0
                        ? (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2
                        : sorted[sorted.Count / 2];
                    medianParts.Add($"{median:F1} {group.Key} ({sorted.Count})");
                }
                MedianPriceText.Text = $"Median: {string.Join(" | ", medianParts)}";
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
                    PriceText = $"{i.Listing.Price.Amount:F1} {i.Listing.Price.Currency}",
                    IndexedText = FormatIndexedTime(i.Listing.Indexed),
                    WhisperText = BuildWhisperText(i)
                })
                .ToList();

            ResultsList.ItemsSource = displayItems;
            OpenBrowserButton.Visibility = result.QueryId != null
                ? Visibility.Visible : Visibility.Collapsed;
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

        private void OnWhisperClick(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn &&
                btn.Tag is string whisper &&
                !string.IsNullOrEmpty(whisper))
            {
                try { Clipboard.SetText(whisper); } catch { }
                StatusText.Text = "Whisper copied!";
            }
        }

        private void OnOpenBrowserClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_lastQueryId)) return;
            var league = Uri.EscapeDataString(_lastLeague ?? "Standard");
            var url = $"https://www.pathofexile.com/trade2/search/poe2/{league}/{_lastQueryId}";
            try
            {
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch { }
        }

        private static string FormatIndexedTime(string indexed)
        {
            if (string.IsNullOrEmpty(indexed)) return "";
            if (!DateTime.TryParse(indexed, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var dt)) return "";
            var elapsed = DateTime.UtcNow - dt.ToUniversalTime();
            if (elapsed.TotalMinutes < 1) return "just now";
            if (elapsed.TotalHours < 1) return $"{(int)elapsed.TotalMinutes}m ago";
            if (elapsed.TotalDays < 1) return $"{(int)elapsed.TotalHours}h ago";
            return $"{(int)elapsed.TotalDays}d ago";
        }

        private string BuildWhisperText(FetchedItem item)
        {
            var charName = item.Listing?.Account?.LastCharacterName
                        ?? item.Listing?.Account?.Name
                        ?? "?";
            var itemName = item.Item?.TypeLine ?? item.Item?.Name ?? "item";
            var price = item.Listing?.Price;
            var priceStr = price != null ? $"{price.Amount:F1} {price.Currency}" : "?";
            return $"@{charName} Hi, I would like to buy your {itemName} listed for {priceStr} in {LeagueInput.Text}";
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            PoE2Overlay.Core.OverlayHelper.AssertTopmost(this);
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
