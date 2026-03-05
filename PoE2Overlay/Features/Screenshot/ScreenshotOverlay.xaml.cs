using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using PoE2Overlay.Core;
using Serilog;

namespace PoE2Overlay.Features.Screenshot
{
    public partial class ScreenshotOverlay : OverlayBase
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

        protected override void SaveWindowState()
        {
            var s = AppSettings.Instance;
            s.ScreenshotWindowLeft = Left;
            s.ScreenshotWindowTop = Top;
            s.ScreenshotWindowWidth = Width;
            s.ScreenshotWindowHeight = Height;
            s.Save();
        }

        protected override void LoadWindowState()
        {
            var s = AppSettings.Instance;
            Left = s.ScreenshotWindowLeft;
            Top = s.ScreenshotWindowTop;
            Width = s.ScreenshotWindowWidth;
            Height = s.ScreenshotWindowHeight;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _service.Screenshots.CollectionChanged -= OnScreenshotsChanged;
            _previewWindow?.Close();
            _previewWindow = null;
            base.OnClosing(e);
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
                var anchorRect = new Rect(Left, Top, Width, Height);
                _previewWindow.ShowImage(image, anchorRect);
            }
            catch (Exception ex) { Log.Debug(ex, "Preview failed"); }
        }

        private void HidePreview()
        {
            _previewWindow?.HidePreview();
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
