using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace PoE2Overlay.Features.Screenshot
{
    public partial class ImagePreviewWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;

        public ImagePreviewWindow()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            ApplyClickThrough();
        }

        private void ApplyClickThrough()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;
            var style = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, style | WS_EX_TRANSPARENT);
        }

        /// <summary>
        /// 프리뷰 표시. anchorRect = 스크린샷 오버레이의 화면 좌표 영역.
        /// 프리뷰를 오버레이 옆에 배치하고, 화면 경계를 넘지 않도록 클램핑.
        /// </summary>
        public void ShowImage(BitmapImage image, Rect anchorRect)
        {
            PreviewImage.Source = image;

            var screenW = SystemParameters.VirtualScreenWidth;
            var screenH = SystemParameters.VirtualScreenHeight;
            var screenL = SystemParameters.VirtualScreenLeft;
            var screenT = SystemParameters.VirtualScreenTop;

            // 이미지 비율 유지하면서 화면의 60%로 제한
            double maxW = screenW * 0.6;
            double maxH = screenH * 0.6;
            PreviewImage.MaxWidth = maxW;
            PreviewImage.MaxHeight = maxH;

            // 실제 이미지 크기 기반으로 표시 크기 계산
            double imgW = image.PixelWidth;
            double imgH = image.PixelHeight;
            double scale = Math.Min(maxW / imgW, maxH / imgH);
            if (scale > 1) scale = 1; // 원본보다 크게 확대하지 않음
            double displayW = imgW * scale + 12; // Border padding + thickness
            double displayH = imgH * scale + 12;

            // 기본: 오버레이 오른쪽에 배치
            double x = anchorRect.Right + 8;
            double y = anchorRect.Top;

            // 오른쪽에 공간이 없으면 왼쪽에 배치
            if (x + displayW > screenL + screenW)
                x = anchorRect.Left - displayW - 8;

            // 왼쪽에도 없으면 화면 오른쪽 끝에 맞춤
            if (x < screenL)
                x = screenL + screenW - displayW;

            // 수직 클램핑
            if (y + displayH > screenT + screenH)
                y = screenT + screenH - displayH;
            if (y < screenT)
                y = screenT;

            Left = x;
            Top = y;

            Show();
            ApplyClickThrough(); // Show() 후 재적용 보장
        }

        public void HidePreview()
        {
            if (IsVisible)
                Hide();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
    }
}
