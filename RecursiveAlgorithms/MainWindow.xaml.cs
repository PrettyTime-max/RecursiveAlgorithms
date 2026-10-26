using RecursiveAlgorithms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FractalDrawer
{
    public partial class MainWindow : Window
    {
        private ComboBox _algorithmComboBox = null!;
        private Slider _depthSlider = null!;
        private TextBlock _depthLabel = null!;
        private CheckBox _useStackCheckBox = null!;
        private Slider _speedSlider = null!;
        private TextBlock _speedLabel = null!;
        private Button _generateButton = null!;
        private Button _stopButton = null!;

        private Grid _statusGrid = null!;
        private ProgressBar _loadingProgressBar = null!;
        private TextBlock _executionTimeTextBlock = null!;
        private TextBlock _zoomTextBlock = null!;

        private Canvas _fractalCanvas = null!;
        private Image _fractalImage = null!;

        // Параметры камеры в мировом пространстве для динамического зума
        private double _viewZoom = 1.0;
        private double _viewPanX = 0.0;
        private double _viewPanY = 0.0;

        private bool _isPanning = false;
        private Point _lastMousePosition;
        private bool _isRedrawing = false;
        private bool _needsRedraw = false;

        private readonly Dictionary<string, IFractalAlgorithm> _algorithms;
        private bool _isAnimating = false;

        private bool _hasRendered = false;
        private string _lastRenderedAlgorithm = "";
        private int _lastRenderedDepth = 0;
        private int _lastHanoiStep = 0;

        // Поля для вкладки анализа сложности (доступные из Benchmark-файла)
        internal ComboBox _benchAlgorithmComboBox = null!;
        internal TextBox _benchMinNTextBox = null!;
        internal TextBox _benchMaxNTextBox = null!;
        internal TextBox _benchStepTextBox = null!;
        internal TextBox _benchRepsTextBox = null!;
        internal CheckBox _benchLogScaleCheckBox = null!;
        internal Button _benchStartButton = null!;
        internal Button _benchStopButton = null!;
        internal Button _benchClearButton = null!;
        internal ProgressBar _benchProgressBar = null!;
        internal TextBlock _benchStatusText = null!;
        internal Canvas _benchCanvas = null!;
        internal ListView _benchListView = null!;
        internal TextBlock _benchSummaryFormula = null!;
        internal TextBlock _benchSummaryC = null!;
        internal TextBlock _benchSummaryR2 = null!;
        internal CancellationTokenSource? _benchCts;
        internal readonly List<BenchmarkDataPoint> _benchPoints = new();

        public MainWindow()
        {
            _algorithms = new Dictionary<string, IFractalAlgorithm>
            {
                { "Треугольник Серпинского", new SierpinskiTriangleAlgorithm() },
                { "Кривая Гильберта", new HilbertCurveAlgorithm() },
                { "Ханойская башня", new HanoiTowerAlgorithm() },
                { "Кривая Леви", new LevyCurveFractal() },
                { "Фрактал Вичека", new VicsekFractalAlgorithm() }
            };

            InitializeUI();
        }

        private void AlgorithmComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string? selected = _algorithmComboBox.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selected)) return;

            _hasRendered = false;
            _fractalImage.Source = null;
            _executionTimeTextBlock.Text = $"Выбран алгоритм '{selected}'. Нажмите 'Запустить'.";

            ResetZoom(redraw: false);

            if (selected == "Ханойская башня")
            {
                _useStackCheckBox.Visibility = Visibility.Visible;
                _depthSlider.Minimum = 1;
                _depthSlider.Maximum = 8;
                _depthSlider.Value = 4;
                _speedSlider.Value = 120;
            }
            else if (selected == "Треугольник Серпинского")
            {
                _useStackCheckBox.Visibility = Visibility.Collapsed;
                _depthSlider.Minimum = 1;
                _depthSlider.Maximum = 16;
                _depthSlider.Value = 8;
                _speedSlider.Value = 70;
            }
            else if (selected == "Кривая Гильберта")
            {
                _useStackCheckBox.Visibility = Visibility.Collapsed;
                _depthSlider.Minimum = 1;
                _depthSlider.Maximum = 13;
                _depthSlider.Value = 5;
                _speedSlider.Value = 60;
            }
            else if (selected == "Кривая Леви")
            {
                _useStackCheckBox.Visibility = Visibility.Collapsed;
                _depthSlider.Minimum = 1;
                _depthSlider.Maximum = 27;
                _depthSlider.Value = 12;
                _speedSlider.Value = 50;
            }
            else
            {
                _useStackCheckBox.Visibility = Visibility.Collapsed;
                _depthSlider.Minimum = 1;
                _depthSlider.Maximum = 11;
                _depthSlider.Value = 5;
                _speedSlider.Value = 60;
            }
        }

        private async void FractalCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Point mousePos = e.GetPosition(_fractalCanvas);
            double factor = e.Delta > 0 ? 1.3 : (1.0 / 1.3);

            int width = (int)(_fractalCanvas.ActualWidth > 50 ? _fractalCanvas.ActualWidth : 800);
            int height = (int)(_fractalCanvas.ActualHeight > 50 ? _fractalCanvas.ActualHeight : 600);

            double oldZoom = _viewZoom;
            double newZoom = Math.Clamp(oldZoom * factor, 0.2, 50000.0);
            if (Math.Abs(newZoom - oldZoom) < 0.00001) return;

            double worldX = (mousePos.X - width / 2.0) / oldZoom + _viewPanX;
            double worldY = (mousePos.Y - height / 2.0) / oldZoom + _viewPanY;

            _viewPanX = worldX - (mousePos.X - width / 2.0) / newZoom;
            _viewPanY = worldY - (mousePos.Y - height / 2.0) / newZoom;
            _viewZoom = newZoom;

            UpdateZoomText();

            if (_hasRendered)
            {
                await RedrawCurrentFrameAsync();
            }

            e.Handled = true;
        }

        private void FractalCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ResetZoom(redraw: _hasRendered);
                return;
            }

            _isPanning = true;
            _lastMousePosition = e.GetPosition(_fractalCanvas);
            _fractalCanvas.CaptureMouse();
            _fractalCanvas.Cursor = Cursors.SizeAll;
        }

        private async void FractalCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                Point curPos = e.GetPosition(_fractalCanvas);
                double deltaX = curPos.X - _lastMousePosition.X;
                double deltaY = curPos.Y - _lastMousePosition.Y;
                _lastMousePosition = curPos;

                _viewPanX -= deltaX / _viewZoom;
                _viewPanY -= deltaY / _viewZoom;

                if (_hasRendered)
                {
                    await RedrawCurrentFrameAsync();
                }
            }
        }

        private void FractalCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            EndPanning();
        }

        private void FractalCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            EndPanning();
        }

        private void EndPanning()
        {
            if (_isPanning)
            {
                _isPanning = false;
                _fractalCanvas.ReleaseMouseCapture();
                _fractalCanvas.Cursor = Cursors.Arrow;
            }
        }

        private async void ResetZoom(bool redraw = true)
        {
            _viewZoom = 1.0;
            _viewPanX = 0.0;
            _viewPanY = 0.0;
            UpdateZoomText();

            if (redraw && _hasRendered)
            {
                await RedrawCurrentFrameAsync();
            }
        }

        private void UpdateZoomText()
        {
            if (_zoomTextBlock != null)
            {
                int percent = (int)Math.Round(_viewZoom * 100);
                _zoomTextBlock.Text = $"{percent}%";
            }
        }

        private async Task RedrawCurrentFrameAsync()
        {
            if (_isAnimating || !_hasRendered) return;
            if (_isRedrawing)
            {
                _needsRedraw = true;
                return;
            }

            _isRedrawing = true;

            try
            {
                do
                {
                    _needsRedraw = false;

                    string? selectedKey = _lastRenderedAlgorithm;
                    if (string.IsNullOrEmpty(selectedKey) || !_algorithms.ContainsKey(selectedKey)) return;

                    int width = (int)(_fractalCanvas.ActualWidth > 50 ? _fractalCanvas.ActualWidth : 800);
                    int height = (int)(_fractalCanvas.ActualHeight > 50 ? _fractalCanvas.ActualHeight : 600);

                    var algorithm = _algorithms[selectedKey];

                    if (algorithm is HanoiTowerAlgorithm hanoiAlg)
                    {
                        hanoiAlg.UseStackAlgorithm = _useStackCheckBox.IsChecked == true;
                        var bitmap = hanoiAlg.CalculateStepBitmap(width, height, _lastRenderedDepth, _lastHanoiStep * HanoiTowerAlgorithm.SubStepsPerMove);
                        _fractalImage.Source = bitmap;
                        _fractalImage.Width = width;
                        _fractalImage.Height = height;
                    }
                    else
                    {
                        var bitmap = await algorithm.CalculateBitmapAsync(width, height, _lastRenderedDepth, _viewZoom, _viewPanX, _viewPanY);

                        _fractalImage.Source = bitmap;
                        _fractalImage.Width = width;
                        _fractalImage.Height = height;
                    }
                }
                while (_needsRedraw && !_isAnimating);
            }
            finally
            {
                _isRedrawing = false;
            }
        }

        private async void ExecuteDrawAnimation()
        {
            string? selectedKey = _algorithmComboBox.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(selectedKey) || !_algorithms.ContainsKey(selectedKey)) return;

            int targetDepth = (int)_depthSlider.Value;

            _lastRenderedAlgorithm = selectedKey;
            _lastRenderedDepth = targetDepth;

            _isAnimating = true;
            _statusGrid.Visibility = Visibility.Visible;
            _generateButton.IsEnabled = false;
            _stopButton.IsEnabled = true;
            _executionTimeTextBlock.Text = "Шаг 1/2: Точный замер времени рекурсии...";

            int width = (int)(_fractalCanvas.ActualWidth > 50 ? _fractalCanvas.ActualWidth : 800);
            int height = (int)(_fractalCanvas.ActualHeight > 50 ? _fractalCanvas.ActualHeight : 600);

            var algorithm = _algorithms[selectedKey];

            Stopwatch sw = Stopwatch.StartNew();
            WriteableBitmap? finalBitmap = null;

            if (algorithm is HanoiTowerAlgorithm hanoiAlg)
            {
                hanoiAlg.UseStackAlgorithm = _useStackCheckBox.IsChecked == true;
                int disksCount = Math.Clamp(targetDepth, 1, 8);
                int totalMoves = hanoiAlg.GetTotalMoves(disksCount);

                finalBitmap = await Task.Run(() => hanoiAlg.CalculateStepBitmap(width, height, disksCount, totalMoves * HanoiTowerAlgorithm.SubStepsPerMove));
            }
            else
            {
                finalBitmap = await algorithm.CalculateBitmapAsync(width, height, targetDepth, _viewZoom, _viewPanX, _viewPanY);
            }

            sw.Stop();
            long exactTimeMs = sw.ElapsedMilliseconds;

            if (!_isAnimating)
            {
                CleanupAfterAnimation("Остановлено");
                return;
            }

            if (algorithm is HanoiTowerAlgorithm hanoiAnim)
            {
                hanoiAnim.UseStackAlgorithm = _useStackCheckBox.IsChecked == true;
                int disksCount = Math.Clamp(targetDepth, 1, 8);
                int totalMoves = hanoiAnim.GetTotalMoves(disksCount);
                string methodTitle = hanoiAnim.UseStackAlgorithm ? "Стек" : "Рекурсия";

                for (int move = 0; move <= totalMoves; move++)
                {
                    _lastHanoiStep = move;

                    for (int sub = 0; sub < HanoiTowerAlgorithm.SubStepsPerMove; sub++)
                    {
                        if (!_isAnimating) break;

                        int currentSubStep = (move == totalMoves) ? (totalMoves * HanoiTowerAlgorithm.SubStepsPerMove) : (move * HanoiTowerAlgorithm.SubStepsPerMove + sub);
                        var bitmap = hanoiAnim.CalculateStepBitmap(width, height, disksCount, currentSubStep);

                        if (!_isAnimating) break;

                        _fractalImage.Source = bitmap;
                        _fractalImage.Width = width;
                        _fractalImage.Height = height;

                        _executionTimeTextBlock.Text = $"[Время расчета: {exactTimeMs} ms] [{methodTitle}] Ход ({Math.Min(move + 1, totalMoves)}/{totalMoves})";

                        int delay = (int)(_speedSlider.Value / HanoiTowerAlgorithm.SubStepsPerMove);
                        await Task.Delay(Math.Max(1, delay));
                    }

                    if (!_isAnimating) break;
                }
            }
            else
            {
                for (int currentDepth = 1; currentDepth <= targetDepth; currentDepth++)
                {
                    if (!_isAnimating) break;

                    _lastRenderedDepth = currentDepth;
                    var bitmap = await algorithm.CalculateBitmapAsync(width, height, currentDepth, _viewZoom, _viewPanX, _viewPanY);

                    if (!_isAnimating) break;

                    _fractalImage.Source = bitmap;
                    _fractalImage.Width = width;
                    _fractalImage.Height = height;

                    _executionTimeTextBlock.Text = $"[Время расчета: {exactTimeMs} ms] Уровень рекурсии (N={currentDepth}/{targetDepth})";

                    int delay = (int)_speedSlider.Value;
                    await Task.Delay(delay);
                }
            }

            if (_isAnimating && finalBitmap != null)
            {
                _fractalImage.Source = finalBitmap;
                _fractalImage.Width = width;
                _fractalImage.Height = height;
            }

            _hasRendered = true;
            CleanupAfterAnimation(_isAnimating ? $"Готово! | Время расчета: {exactTimeMs} ms" : "Остановлено");
        }

        private void CleanupAfterAnimation(string statusText)
        {
            _executionTimeTextBlock.Text = statusText;
            _statusGrid.Visibility = Visibility.Collapsed;
            _generateButton.IsEnabled = true;
            _stopButton.IsEnabled = false;
            _isAnimating = false;
        }
    }
}