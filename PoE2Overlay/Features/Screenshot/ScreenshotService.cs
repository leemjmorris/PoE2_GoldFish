using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Serilog;

namespace PoE2Overlay.Features.Screenshot
{
    public class ScreenshotItem
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public BitmapImage Thumbnail { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ScreenshotService : IDisposable
    {
        private const int MaxThumbnails = 100;

        private FileSystemWatcher _watcher;
        private readonly Dispatcher _dispatcher;

        public ObservableCollection<ScreenshotItem> Screenshots { get; } = new();

        public ScreenshotService(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public void StartWatching(string directory)
        {
            StopWatching();

            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                return;

            LoadExistingImages(directory);

            _watcher = new FileSystemWatcher(directory)
            {
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime
            };
            _watcher.Filters.Add("*.png");
            _watcher.Filters.Add("*.jpg");
            _watcher.Filters.Add("*.jpeg");
            _watcher.Filters.Add("*.bmp");
            _watcher.Created += OnFileCreated;
            _watcher.Deleted += OnFileDeleted;
        }

        private void LoadExistingImages(string directory)
        {
            Screenshots.Clear();
            var extensions = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp" };
            var files = extensions
                .SelectMany(ext => Directory.GetFiles(directory, ext))
                .OrderByDescending(f => File.GetCreationTime(f));

            foreach (var file in files.Take(MaxThumbnails))
                AddScreenshot(file);
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            _dispatcher.BeginInvoke(() => AddScreenshot(e.FullPath));
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            _dispatcher.BeginInvoke(() =>
            {
                var item = Screenshots.FirstOrDefault(s => s.FilePath == e.FullPath);
                if (item != null) Screenshots.Remove(item);
            });
        }

        private void AddScreenshot(string filePath)
        {
            try
            {
                var thumbnail = new BitmapImage();
                thumbnail.BeginInit();
                thumbnail.UriSource = new Uri(filePath);
                thumbnail.DecodePixelWidth = 160;
                thumbnail.CacheOption = BitmapCacheOption.OnLoad;
                thumbnail.EndInit();
                thumbnail.Freeze();

                Screenshots.Insert(0, new ScreenshotItem
                {
                    FilePath = filePath,
                    FileName = Path.GetFileName(filePath),
                    Thumbnail = thumbnail,
                    CreatedAt = File.GetCreationTime(filePath)
                });

                // 최대 개수 초과 시 가장 오래된 항목 제거
                while (Screenshots.Count > MaxThumbnails)
                    Screenshots.RemoveAt(Screenshots.Count - 1);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Thumbnail load failed for {FilePath}", filePath);
            }
        }

        public BitmapImage LoadFullImage(string filePath)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(filePath);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();
            return image;
        }

        public void StopWatching()
        {
            if (_watcher != null)
            {
                _watcher.Created -= OnFileCreated;
                _watcher.Deleted -= OnFileDeleted;
                _watcher.Dispose();
                _watcher = null;
            }
        }

        public void Dispose()
        {
            StopWatching();
        }
    }
}
