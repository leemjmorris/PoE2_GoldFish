using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using PoE2Overlay.Core;
using PoE2Overlay.Features.PassiveTree.Services;

namespace PoE2Overlay.Features.PassiveTree
{
    public partial class PassiveTreeOverlay : OverlayBase
    {
        // ── 정적 브러시 (Freeze로 렌더링 최적화) ──────────────────────────────
        private static readonly SolidColorBrush _connBrush = Frozen(Color.FromArgb(90, 80, 80, 130));
        private static readonly SolidColorBrush _normalFill = Frozen(Color.FromRgb(75, 75, 100));
        private static readonly SolidColorBrush _normalStroke = Frozen(Color.FromArgb(130, 140, 140, 170));
        private static readonly SolidColorBrush _notableFill = Frozen(Color.FromRgb(160, 160, 50));
        private static readonly SolidColorBrush _keystoneFill = Frozen(Color.FromRgb(200, 120, 20));
        private static readonly SolidColorBrush _masteryFill = Frozen(Color.FromRgb(60, 100, 160));
        private static readonly SolidColorBrush _classStartFill = Frozen(Color.FromRgb(180, 50, 50));
        private static readonly SolidColorBrush _allocFill = Frozen(Color.FromRgb(212, 175, 55));
        private static readonly SolidColorBrush _allocStroke = Frozen(Color.FromRgb(255, 215, 90));

        private static SolidColorBrush Frozen(Color c)
        {
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        // ── 상태 ────────────────────────────────────────────────────────────
        private readonly PassiveTreeService _treeService = new();
        private HashSet<int> _allocatedNodes = new();
        private readonly Dictionary<int, Shape> _nodeShapes = new();

        // ── 줌/패닝 ─────────────────────────────────────────────────────────
        private Matrix _matrix = Matrix.Identity;
        private Point _panStart;
        private bool _isPanning;

        // ────────────────────────────────────────────────────────────────────

        public PassiveTreeOverlay()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadState();
            StatusText.Text = "tree.json 로드 중...";

            Task.Run(() =>
            {
                _treeService.Load();
                Dispatcher.InvokeAsync(() =>
                {
                    if (_treeService.IsLoaded)
                    {
                        RenderTree();
                        FitToView();
                        StatusText.Text = $"트리 로드 완료";
                        UpdateAllocText();
                    }
                    else
                    {
                        StatusText.Text =
                            "tree.json 없음 — Resources/PassiveTree/tree.json 을 배치하고 재빌드하세요";
                    }
                });
            });
        }

        // ── 트리 렌더링 ─────────────────────────────────────────────────────

        private void RenderTree()
        {
            TreeCanvas.Children.Clear();
            _nodeShapes.Clear();

            var nodes = _treeService.Nodes;

            // 1) 연결선 (노드 위에 쌓이지 않도록 먼저 그림)
            foreach (var kvp in nodes)
            {
                var node = kvp.Value;
                foreach (int connId in node.Out)
                {
                    if (!nodes.ContainsKey(connId)) continue;
                    if (connId < node.Id) continue; // 중복 방지

                    var connNode = nodes[connId];
                    var line = new Line
                    {
                        X1 = node.X, Y1 = node.Y,
                        X2 = connNode.X, Y2 = connNode.Y,
                        Stroke = _connBrush,
                        StrokeThickness = 1
                    };
                    TreeCanvas.Children.Add(line);
                }
            }

            // 2) 노드 원
            foreach (var kvp in nodes)
            {
                var node = kvp.Value;
                double size = NodeSize(node);
                var fill = DefaultFill(node);

                var ellipse = new Ellipse
                {
                    Width = size,
                    Height = size,
                    Fill = fill,
                    Stroke = _normalStroke,
                    StrokeThickness = 0.5,
                    Tag = node.Id,
                    ToolTip = string.IsNullOrEmpty(node.Name) ? null : node.Name
                };
                Canvas.SetLeft(ellipse, node.X - size / 2.0);
                Canvas.SetTop(ellipse, node.Y - size / 2.0);
                TreeCanvas.Children.Add(ellipse);
                _nodeShapes[node.Id] = ellipse;
            }
        }

        private static double NodeSize(TreeNodeInfo node)
        {
            if (node.IsKeystone) return 18;
            if (node.IsClassStart) return 20;
            if (node.IsNotable) return 11;
            if (node.IsMastery) return 10;
            return 6;
        }

        private static SolidColorBrush DefaultFill(TreeNodeInfo node)
        {
            if (node.IsClassStart) return _classStartFill;
            if (node.IsKeystone) return _keystoneFill;
            if (node.IsNotable) return _notableFill;
            if (node.IsMastery) return _masteryFill;
            return _normalFill;
        }

        private void ApplyHighlights()
        {
            foreach (var (id, shape) in _nodeShapes)
            {
                if (_allocatedNodes.Contains(id))
                {
                    shape.Fill = _allocFill;
                    shape.Stroke = _allocStroke;
                    shape.StrokeThickness = 2;
                }
                else
                {
                    var node = _treeService.GetNode(id);
                    shape.Fill = node is null ? _normalFill : DefaultFill(node);
                    shape.Stroke = _normalStroke;
                    shape.StrokeThickness = 0.5;
                }
            }
        }

        // ── 뷰 피팅 ─────────────────────────────────────────────────────────

        private void FitToView()
        {
            if (!_treeService.IsLoaded) return;

            UpdateLayout();
            double vpW = TreeViewport.ActualWidth;
            double vpH = TreeViewport.ActualHeight;
            double treeW = _treeService.MaxX - _treeService.MinX;
            double treeH = _treeService.MaxY - _treeService.MinY;

            if (vpW <= 0 || vpH <= 0 || treeW <= 0 || treeH <= 0) return;

            double scale = Math.Min(vpW / treeW, vpH / treeH) * 0.9;
            double offsetX = (vpW - treeW * scale) / 2.0 - _treeService.MinX * scale;
            double offsetY = (vpH - treeH * scale) / 2.0 - _treeService.MinY * scale;

            _matrix = new Matrix(scale, 0, 0, scale, offsetX, offsetY);
            CanvasTransform.Matrix = _matrix;
            UpdateZoomText();
        }

        // ── 줌 / 패닝 이벤트 ────────────────────────────────────────────────

        private void OnViewportMouseWheel(object sender, MouseWheelEventArgs e)
        {
            double factor = e.Delta > 0 ? 1.12 : 1.0 / 1.12;
            var pivot = e.GetPosition(TreeViewport);
            _matrix.ScaleAtPrepend(factor, factor, pivot.X, pivot.Y);
            CanvasTransform.Matrix = _matrix;
            UpdateZoomText();
            e.Handled = true;
        }

        private void OnViewportMouseDown(object sender, MouseButtonEventArgs e)
        {
            _panStart = e.GetPosition(TreeViewport);
            _isPanning = true;
            TreeViewport.CaptureMouse();
            e.Handled = true;
        }

        private void OnViewportMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isPanning = false;
            TreeViewport.ReleaseMouseCapture();
        }

        private void OnViewportMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isPanning) return;
            var pos = e.GetPosition(TreeViewport);
            _matrix.OffsetX += pos.X - _panStart.X;
            _matrix.OffsetY += pos.Y - _panStart.Y;
            _panStart = pos;
            CanvasTransform.Matrix = _matrix;
        }

        // ── 입력 이벤트 ─────────────────────────────────────────────────────

        private void OnInputTextChanged(object sender, TextChangedEventArgs e)
        {
            PobHint.Visibility = string.IsNullOrEmpty(PobInput.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void OnInputKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                _ = LoadBuildAsync();
        }

        private void OnLoadClick(object sender, RoutedEventArgs e)
            => _ = LoadBuildAsync();

        private void OnClearClick(object sender, RoutedEventArgs e)
        {
            _allocatedNodes.Clear();
            PobInput.Text = "";
            ApplyHighlights();
            UpdateAllocText();
        }

        // ── PoB 빌드 로딩 ───────────────────────────────────────────────────

        private async Task LoadBuildAsync()
        {
            string input = PobInput.Text.Trim();
            if (string.IsNullOrEmpty(input)) return;

            StatusText.Text = "빌드 코드 디코딩 중...";
            try
            {
                var ids = await PobDecoder.DecodeAsync(input);
                _allocatedNodes = new HashSet<int>(ids);
                ApplyHighlights();
                StatusText.Text = "로드 완료";
                UpdateAllocText();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"오류: {ex.Message}";
            }
        }

        // ── 텍스트 업데이트 ─────────────────────────────────────────────────

        private void UpdateZoomText()
        {
            ZoomText.Text = $"{_matrix.M11 * 100:F0}%";
        }

        private void UpdateAllocText()
        {
            AllocText.Text = _allocatedNodes.Count > 0
                ? $"할당 노드: {_allocatedNodes.Count}"
                : "";
        }

        // ── OverlayBase 구현 ─────────────────────────────────────────────────

        protected override void SaveWindowState()
        {
            var s = AppSettings.Instance;
            s.PassiveTreeWindowLeft = Left;
            s.PassiveTreeWindowTop = Top;
            s.PassiveTreeWindowWidth = Width;
            s.PassiveTreeWindowHeight = Height;
            s.Save();
        }

        protected override void LoadWindowState()
        {
            var s = AppSettings.Instance;
            Left = s.PassiveTreeWindowLeft;
            Top = s.PassiveTreeWindowTop;
            Width = s.PassiveTreeWindowWidth;
            Height = s.PassiveTreeWindowHeight;
        }
    }
}
