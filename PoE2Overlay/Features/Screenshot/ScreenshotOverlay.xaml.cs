using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using PoE2Overlay.Core;

namespace PoE2Overlay.Features.Screenshot
{
    public partial class ScreenshotOverlay : Window
    {
        private readonly ScreenshotService _service;
        private ImagePreviewWindow _previewWindow;

        public ScreenshotOverlay()
        {
            InitializeComponent();
            _service = new ScreenshotService(Dispatcher);
            ThumbnailList.ItemsSource = _service.Screenshots;
            _service.Screenshots.CollectionChanged += OnScreenshotsChanged;
            LoadState();

            var dir = AppSettings.Instance.ScreenshotDirectory;
            if (!string.IsNullOrEmpty(dir))
            {
                DirectoryText.Text = dir;
                _service.StartWatching(dir);
                UpdateStatus();
            }
        }

        private void LoadState()
        {
            var s = AppSettings.Instance;
            Left = s.ScreenshotWindowLeft;
            Top = s.ScreenshotWindowTop;
            Width = s.ScreenshotWindowWidth;
            Height = s.ScreenshotWindowHeight;
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
            s.ScreenshotWindowLeft = Left;
            s.ScreenshotWindowTop = Top;
            s.ScreenshotWindowWidth = Width;
            s.ScreenshotWindowHeight = Height;
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

        private void OnBrowseClick(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select Screenshot Directory"
            };
            if (dialog.ShowDialog() == true)
            {
                var dir = dialog.FolderName;
                AppSettings.Instance.ScreenshotDirectory = dir;
                AppSettings.Instance.Save();
                DirectoryText.Text = dir;
                _service.StartWatching(dir);
                UpdateStatus();
            }
        }

        private void OnThumbnailMouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is System.Windows.Controls.Border border &&
                border.Tag is string filePath)
            {
                ShowPreview(filePath);
            }
        }

        private void OnThumbnailMouseLeave(object sender, MouseEventArgs e)
        {
            HidePreview();
        }

        private void ShowPreview(string filePath)
        {
            try
            {
                if (_previewWindow == null)
                    _previewWindow = new ImagePreviewWindow();

                var image = _service.LoadFullImage(filePath);
                // 이 오버레이의 화면 좌표를 앵커로 전달
                var anchorRect = new Rect(Left, Top, Width, Height);
                _previewWindow.ShowImage(image, anchorRect);
            }
            catch { }
        }

        private void HidePreview()
        {
            _previewWindow?.HidePreview();
        }

        private void OnResizeDrag(object sender, DragDeltaEventArgs e)
        {
            double newWidth = Width + e.HorizontalChange;
            double newHeight = Height + e.VerticalChange;
            if (newWidth >= MinWidth) Width = newWidth;
            if (newHeight >= MinHeight) Height = newHeight;
        }

        private void OnScreenshotsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            StatusText.Text = $"{_service.Screenshots.Count} images";
        }
    }
}
