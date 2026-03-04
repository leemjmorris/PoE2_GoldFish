using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using PoE2Overlay.Core;

namespace PoE2Overlay.Features.Memo
{
    public partial class MemoOverlay : Window
    {
        private readonly MemoService _memoService = new();
        private DispatcherTimer _autoSaveTimer;
        private bool _isDirty;

        public MemoOverlay()
        {
            InitializeComponent();
            LoadState();
            MemoTextBox.Text = _memoService.Load();
            _isDirty = false;

            _autoSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _autoSaveTimer.Tick += OnAutoSave;
            _autoSaveTimer.Start();
        }

        private void LoadState()
        {
            var s = AppSettings.Instance;
            Left = s.MemoWindowLeft;
            Top = s.MemoWindowTop;
            Width = s.MemoWindowWidth;
            Height = s.MemoWindowHeight;
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
            s.MemoWindowLeft = Left;
            s.MemoWindowTop = Top;
            s.MemoWindowWidth = Width;
            s.MemoWindowHeight = Height;
            s.Save();
        }

        private void OnTitleBarDrag(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            SaveMemo();
            SaveState();
            Hide();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            SaveMemo();
            SaveState();
            Hide();
        }

        private void OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            _isDirty = true;
            StatusText.Text = "Unsaved";
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            SaveMemo();
        }

        private void OnAutoSave(object sender, EventArgs e)
        {
            if (_isDirty)
                SaveMemo();
        }

        private void SaveMemo()
        {
            if (_memoService.Save(MemoTextBox.Text))
            {
                _isDirty = false;
                StatusText.Text = $"Saved {DateTime.Now:HH:mm:ss}";
            }
            else
            {
                StatusText.Text = "Save failed";
            }
        }

        private void OnResizeDrag(object sender, DragDeltaEventArgs e)
        {
            double newWidth = Width + e.HorizontalChange;
            double newHeight = Height + e.VerticalChange;
            if (newWidth >= MinWidth) Width = newWidth;
            if (newHeight >= MinHeight) Height = newHeight;
        }

        public void Toggle()
        {
            if (IsVisible)
            {
                SaveMemo();
                SaveState();
                Hide();
            }
            else
            {
                Show();
                // Activate() 제거 — PoE2가 포커스 유지, 클릭 시에만 포커스 전환
            }
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            PoE2Overlay.Core.OverlayHelper.AssertTopmost(this);
        }
    }
}