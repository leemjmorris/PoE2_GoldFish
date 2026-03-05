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
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr newStyle);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;

        private const double MaxScreenPercent = 0.6;
        private const double BorderPadding = 12;
        private const double AnchorGap = 8;

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
            var style = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
            SetWindowLongPtr(hwnd, GWL_EXSTYLE, style | (IntPtr)WS_EX_TRANSPARENT);
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

            double maxW = screenW * MaxScreenPercent;
            double maxH = screenH * MaxScreenPercent;
            PreviewImage.MaxWidth = maxW;
            PreviewImage.MaxHeight = maxH;

            double imgW = image.PixelWidth;
            double imgH = image.PixelHeight;
            double scale = Math.Min(maxW / imgW, maxH / imgH);
            if (scale > 1) scale = 1;
            double displayW = imgW * scale + BorderPadding;
            double displayH = imgH * scale + BorderPadding;

            double x = anchorRect.Right + AnchorGap;
            double y = anchorRect.Top;

            if (x + displayW > screenL + screenW)
                x = anchorRect.Left - displayW - AnchorGap;

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
