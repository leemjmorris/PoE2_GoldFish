using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using PoE2Overlay.Core;

namespace PoE2Overlay.Features.Memo
{
    public partial class MemoOverlay : OverlayBase
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

        protected override void SaveWindowState()
        {
            var s = AppSettings.Instance;
            s.MemoWindowLeft = Left;
            s.MemoWindowTop = Top;
            s.MemoWindowWidth = Width;
            s.MemoWindowHeight = Height;
            s.Save();
        }

        protected override void LoadWindowState()
        {
            var s = AppSettings.Instance;
            Left = s.MemoWindowLeft;
            Top = s.MemoWindowTop;
            Width = s.MemoWindowWidth;
            Height = s.MemoWindowHeight;
        }

        public override void Toggle()
        {
            if (IsVisible)
            {
                SaveMemo();
                _autoSaveTimer.Stop();
            }
            else
            {
                _autoSaveTimer.Start();
            }
            base.Toggle();
        }

        protected override void OnCloseClick(object sender, RoutedEventArgs e)
        {
            SaveMemo();
            _autoSaveTimer.Stop();
            base.OnCloseClick(sender, e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            SaveMemo();
            _autoSaveTimer.Stop();
            base.OnClosing(e);
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
    }
}
