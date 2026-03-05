using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace PoE2Overlay.Core
{
    /// <summary>
    /// 모든 오버레이 윈도우의 공통 기능을 제공하는 기본 클래스.
    /// 위치/크기 저장/복원, 화면 클램핑, 드래그 이동, 리사이즈, Topmost 유지를 포함합니다.
    /// </summary>
    public abstract class OverlayBase : Window
    {
        /// <summary>현재 위치/크기를 AppSettings에 저장합니다.</summary>
        protected abstract void SaveWindowState();

        /// <summary>AppSettings에서 위치/크기를 복원합니다.</summary>
        protected abstract void LoadWindowState();

        protected void LoadState()
        {
            LoadWindowState();
            ClampToScreen();
        }

        protected void SaveState()
        {
            SaveWindowState();
        }

        protected void ClampToScreen()
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

        public virtual void Toggle()
        {
            if (IsVisible)
            {
                SaveState();
                Hide();
                GameFocusManager.RestoreFocusToGame();
            }
            else
            {
                GameFocusManager.CaptureCurrentFocus();
                Show();
            }
        }

        protected void OnTitleBarDrag(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        protected virtual void OnCloseClick(object sender, RoutedEventArgs e)
        {
            SaveState();
            Hide();
            GameFocusManager.RestoreFocusToGame();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            SaveState();
            Hide();
            GameFocusManager.RestoreFocusToGame();
        }

        protected void OnResizeDrag(object sender, DragDeltaEventArgs e)
        {
            double newW = Width + e.HorizontalChange;
            double newH = Height + e.VerticalChange;
            if (newW >= MinWidth) Width = newW;
            if (newH >= MinHeight) Height = newH;
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            OverlayHelper.AssertTopmost(this);
        }
    }
}
