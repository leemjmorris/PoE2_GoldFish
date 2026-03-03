using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

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

            foreach (var file in files)
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
            }
            catch
            {
                // 이미지 로딩 실패 (파일 쓰기 중 등) - 무시
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
            _watcher?.Dispose();
            _watcher = null;
        }

        public void Dispose()
        {
            StopWatching();
        }
    }
}
