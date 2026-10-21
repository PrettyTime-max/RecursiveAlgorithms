using RecursiveAlgorithms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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

        // Флаг: был ли уже запущен расчет пользователем через кнопку "Запустить"
        private bool _hasRendered = false;
        private string _lastRenderedAlgorithm = "";
        private int _lastRenderedDepth = 0;
        private int _lastHanoiStep = 0;

        // --- Поля для вкладки анализа теоретической и эмпирической сложности ---
        private ComboBox _benchAlgorithmComboBox = null!;
        private TextBox _benchMinNTextBox = null!;
        private TextBox _benchMaxNTextBox = null!;
        private TextBox _benchStepTextBox = null!;
        private TextBox _benchRepsTextBox = null!;
        private CheckBox _benchLogScaleCheckBox = null!;
        private Button _benchStartButton = null!;
        private Button _benchStopButton = null!;
        private Button _benchClearButton = null!;
        private ProgressBar _benchProgressBar = null!;
        private TextBlock _benchStatusText = null!;
        private Canvas _benchCanvas = null!;
        private ListView _benchListView = null!;
        private TextBlock _benchSummaryFormula = null!;
        private TextBlock _benchSummaryC = null!;
        private TextBlock _benchSummaryR2 = null!;
        private CancellationTokenSource? _benchCts;
        private readonly List<BenchmarkDataPoint> _benchPoints = new();

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

        private void InitializeUI()
        {
            this.Title = "Генератор фракталов и рекурсивных алгоритмов";
            this.Width = 1040;
            this.Height = 720;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            Grid mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // --- 1. Панель управления ---
            GroupBox controlGroupBox = new GroupBox
            {
                Header = "Панель управления фракталами и алгоритмами",
                Margin = new Thickness(10),
                FontWeight = FontWeights.Normal
            };

            WrapPanel controlPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(5),
                VerticalAlignment = VerticalAlignment.Center
            };

            // Алгоритм
            controlPanel.Children.Add(new TextBlock
            {
                Text = "Алгоритм:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0),
                FontWeight = FontWeights.Normal
            });

            _algorithmComboBox = new ComboBox
            {
                Width = 210,
                Height = 26,
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 15, 0),
                FontWeight = FontWeights.Normal
            };
            foreach (var key in _algorithms.Keys)
            {
                _algorithmComboBox.Items.Add(key);
            }
            if (_algorithmComboBox.Items.Count > 0) _algorithmComboBox.SelectedIndex = 0;
            _algorithmComboBox.SelectionChanged += AlgorithmComboBox_SelectionChanged;
            controlPanel.Children.Add(_algorithmComboBox);

            // --- Блок ползунка глубины рекурсии (N) в рамке ---
            StackPanel depthInnerStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            depthInnerStack.Children.Add(new TextBlock
            {
                Text = "Рекурсии (N):",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0),
                FontWeight = FontWeights.Normal
            });

            _depthSlider = new Slider
            {
                Minimum = 1,
                Maximum = 16,
                Value = 8,
                Width = 95,
                IsSnapToTickEnabled = true,
                TickFrequency = 1,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0),
                SmallChange = 1,
                LargeChange = 2
            };

            _depthLabel = new TextBlock
            {
                Text = "8",
                Width = 28,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 0),
                Foreground = Brushes.Black,
                FontWeight = FontWeights.Normal
            };

            _depthSlider.ValueChanged += (s, e) =>
            {
                if (_depthLabel != null)
                {
                    _depthLabel.Text = $"{(int)_depthSlider.Value}";
                }
            };

            depthInnerStack.Children.Add(_depthSlider);
            depthInnerStack.Children.Add(_depthLabel);

            Border depthBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6, 3, 6, 3),
                Margin = new Thickness(0, 0, 15, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = depthInnerStack
            };
            controlPanel.Children.Add(depthBorder);

            // Чекбокс для решения через Стек (Дополнительное задание ТЗ)
            _useStackCheckBox = new CheckBox
            {
                Content = "Через Стек (доп. задание)",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 15, 0),
                Visibility = Visibility.Collapsed,
                FontWeight = FontWeights.Normal,
                ToolTip = "Итеративное решение Ханойских башен с использованием явного стека вместо системного стека вызовов"
            };
            controlPanel.Children.Add(_useStackCheckBox);

            // --- Блок регулятора скорости в рамке ---
            StackPanel speedInnerStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            speedInnerStack.Children.Add(new TextBlock
            {
                Text = "Задержка:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0),
                FontWeight = FontWeights.Normal
            });

            _speedSlider = new Slider
            {
                Minimum = 10,
                Maximum = 400,
                Value = 50,
                Width = 90,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0),
                SmallChange = 10,
                LargeChange = 50
            };
            _speedLabel = new TextBlock
            {
                Text = "50 мс",
                Width = 45,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 0),
                Foreground = Brushes.DimGray,
                FontWeight = FontWeights.Normal
            };
            _speedSlider.ValueChanged += (s, e) =>
            {
                if (_speedLabel != null)
                {
                    _speedLabel.Text = $"{(int)_speedSlider.Value} мс";
                }
            };

            speedInnerStack.Children.Add(_speedSlider);
            speedInnerStack.Children.Add(_speedLabel);

            Border speedBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6, 3, 6, 3),
                Margin = new Thickness(0, 0, 15, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = speedInnerStack
            };
            controlPanel.Children.Add(speedBorder);

            // Кнопка Запустить
            _generateButton = new Button
            {
                Content = "Запустить",
                Width = 90,
                Height = 26,
                FontWeight = FontWeights.Normal,
                Margin = new Thickness(0, 0, 6, 0)
            };
            _generateButton.Click += (s, e) => ExecuteDrawAnimation();
            controlPanel.Children.Add(_generateButton);

            // Кнопка Стоп
            _stopButton = new Button
            {
                Content = "Стоп",
                Width = 70,
                Height = 26,
                FontWeight = FontWeights.Normal,
                IsEnabled = false,
                Margin = new Thickness(0, 0, 5, 0)
            };
            _stopButton.Click += (s, e) => { _isAnimating = false; };
            controlPanel.Children.Add(_stopButton);

            controlGroupBox.Content = controlPanel;
            Grid.SetRow(controlGroupBox, 0);
            mainGrid.Children.Add(controlGroupBox);

            // --- 2. Статусная строка ---
            _statusGrid = new Grid
            {
                Margin = new Thickness(10, 0, 10, 8),
                Visibility = Visibility.Collapsed
            };
            _statusGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _statusGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _loadingProgressBar = new ProgressBar
            {
                Height = 6,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 4),
                IsIndeterminate = true
            };
            Grid.SetRow(_loadingProgressBar, 0);
            _statusGrid.Children.Add(_loadingProgressBar);

            TextBlock vpnWarningText = new TextBlock
            {
                Text = "Для нормальной работы приложения, отключите ВПН",
                HorizontalAlignment = HorizontalAlignment.Left,
                Foreground = Brushes.Black,
                FontSize = 12,
                FontStyle = FontStyles.Italic,
                FontWeight = FontWeights.Normal
            };
            Grid.SetRow(vpnWarningText, 1);
            _statusGrid.Children.Add(vpnWarningText);

            Grid.SetRow(_statusGrid, 1);
            mainGrid.Children.Add(_statusGrid);

            // --- 3. Холст с динамическим зумом и навигацией ---
            Grid canvasContainer = new Grid
            {
                Margin = new Thickness(10, 2, 10, 10)
            };

            Border canvasBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                ClipToBounds = true
            };

            _fractalCanvas = new Canvas
            {
                ClipToBounds = true,
                Background = Brushes.White
            };

            _fractalImage = new Image
            {
                Stretch = Stretch.None
            };
            RenderOptions.SetBitmapScalingMode(_fractalImage, BitmapScalingMode.NearestNeighbor);

            _fractalCanvas.Children.Add(_fractalImage);
            canvasBorder.Child = _fractalCanvas;
            canvasContainer.Children.Add(canvasBorder);

            // События мыши для динамического Zoom & Pan
            _fractalCanvas.MouseWheel += FractalCanvas_MouseWheel;
            _fractalCanvas.MouseLeftButtonDown += FractalCanvas_MouseLeftButtonDown;
            _fractalCanvas.MouseMove += FractalCanvas_MouseMove;
            _fractalCanvas.MouseLeftButtonUp += FractalCanvas_MouseLeftButtonUp;
            _fractalCanvas.MouseLeave += FractalCanvas_MouseLeave;

            // --- Информационная строка поверх холста (внизу, только время) ---
            Border infoOverlayBorder = new Border
            {
                Height = 28,
                Background = new SolidColorBrush(Color.FromArgb(235, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(12),
                Padding = new Thickness(10, 4, 10, 4)
            };

            _executionTimeTextBlock = new TextBlock
            {
                Text = "Выберите алгоритм и нажмите 'Запустить'",
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 11,
                FontFamily = new FontFamily("Consolas, Segoe UI"),
                Foreground = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
                FontWeight = FontWeights.Normal
            };
            infoOverlayBorder.Child = _executionTimeTextBlock;
            canvasContainer.Children.Add(infoOverlayBorder);

            Grid.SetRow(canvasContainer, 2);
            mainGrid.Children.Add(canvasContainer);

            // --- Корневой TabControl с двумя вкладками: 1) Визуализация 2) Анализ сложности ---
            TabControl rootTabControl = new TabControl
            {
                Background = new SolidColorBrush(Color.FromRgb(248, 249, 250)),
                BorderThickness = new Thickness(0, 1, 0, 0)
            };

            TabItem fractalTab = new TabItem
            {
                Header = "🎨  Визуализация фракталов",
                Content = mainGrid,
                FontSize = 13,
                Padding = new Thickness(14, 6, 14, 6)
            };
            rootTabControl.Items.Add(fractalTab);

            TabItem complexityTab = new TabItem
            {
                Header = "📊  Анализ сложности (График)",
                Content = CreateComplexityTabContent(),
                FontSize = 13,
                Padding = new Thickness(14, 6, 14, 6)
            };
            rootTabControl.Items.Add(complexityTab);

            this.Content = rootTabControl;
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
                _depthSlider.Maximum = 8; // При N=8 время анимации ~10-12 секунд
                _depthSlider.Value = 4;
                _speedSlider.Value = 120;
            }
            else if (selected == "Треугольник Серпинского")
            {
                _useStackCheckBox.Visibility = Visibility.Collapsed;
                _depthSlider.Minimum = 1;
                _depthSlider.Maximum = 16; // При N=16 время расчета ~9 секунд
                _depthSlider.Value = 8;
                _speedSlider.Value = 70;
            }
            else if (selected == "Кривая Гильберта")
            {
                _useStackCheckBox.Visibility = Visibility.Collapsed;
                _depthSlider.Minimum = 1;
                _depthSlider.Maximum = 13; // При N=13 время расчета ~6-10 секунд
                _depthSlider.Value = 5;
                _speedSlider.Value = 60;
            }
            else if (selected == "Кривая Леви")
            {
                _useStackCheckBox.Visibility = Visibility.Collapsed;
                _depthSlider.Minimum = 1;
                _depthSlider.Maximum = 27; // При N=27 время расчета ровно ~10 секунд
                _depthSlider.Value = 12;
                _speedSlider.Value = 50;
            }
            else // Фрактал Вичека
            {
                _useStackCheckBox.Visibility = Visibility.Collapsed;
                _depthSlider.Minimum = 1;
                _depthSlider.Maximum = 11;
                _depthSlider.Value = 5;
                _speedSlider.Value = 60;
            }
        }

        // --- Логика динамического зума и панорамирования ---
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

        // --- Выполнение в два прохода: 1) Точный замер времени 2) Красивая анимация ---
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

            // ==========================================
            // ПРОХОД 1: Точный замер времени без визуальных задержек
            // ==========================================
            Stopwatch sw = Stopwatch.StartNew();
            WriteableBitmap? finalBitmap = null;

            if (algorithm is HanoiTowerAlgorithm hanoiAlg)
            {
                hanoiAlg.UseStackAlgorithm = _useStackCheckBox.IsChecked == true;
                int disksCount = Math.Clamp(targetDepth, 1, 8);
                int totalMoves = hanoiAlg.GetTotalMoves(disksCount);

                // Считаем финальное состояние в фоне для точного времени
                finalBitmap = await Task.Run(() => hanoiAlg.CalculateStepBitmap(width, height, disksCount, totalMoves * HanoiTowerAlgorithm.SubStepsPerMove));
            }
            else
            {
                // Считаем фрактал сразу для конечной глубины
                finalBitmap = await algorithm.CalculateBitmapAsync(width, height, targetDepth, _viewZoom, _viewPanX, _viewPanY);
            }

            sw.Stop();
            long exactTimeMs = sw.ElapsedMilliseconds;

            if (!_isAnimating)
            {
                CleanupAfterAnimation("Остановлено");
                return;
            }

            // ==========================================
            // ПРОХОД 2: Красивая покадровая анимация для зрителей
            // ==========================================
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
                // Анимация построения фрактала по уровням рекурсии от 1 до targetDepth
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

        // =========================================================================
        // ВКЛАДКА: АНАЛИЗ ТЕОРЕТИЧЕСКОЙ И ЭМПИРИЧЕСКОЙ СЛОЖНОСТИ (БЕЗ ОГРАНИЧЕНИЙ)
        // =========================================================================

        private UIElement CreateComplexityTabContent()
        {
            Grid root = new Grid
            {
                Margin = new Thickness(10)
            };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 1. Панель управления
            GroupBox controlBox = new GroupBox
            {
                Header = "Параметры замера теоретической и эмпирической сложности (без ограничений глубины)",
                Margin = new Thickness(0, 0, 0, 10),
                FontWeight = FontWeights.Normal
            };

            WrapPanel panel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(5),
                VerticalAlignment = VerticalAlignment.Center
            };

            panel.Children.Add(new TextBlock { Text = "Алгоритм:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 5, 0) });

            _benchAlgorithmComboBox = new ComboBox
            {
                Width = 200,
                Height = 26,
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            };
            foreach (var algo in ComplexityBenchmarkService.GetAvailableAlgorithms())
            {
                _benchAlgorithmComboBox.Items.Add(algo);
            }
            _benchAlgorithmComboBox.SelectedIndex = 0;
            _benchAlgorithmComboBox.SelectionChanged += BenchAlgorithmComboBox_SelectionChanged;
            panel.Children.Add(_benchAlgorithmComboBox);

            // N от
            panel.Children.Add(new TextBlock { Text = "N от:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) });
            _benchMinNTextBox = new TextBox
            {
                Text = "1",
                Width = 42,
                Height = 26,
                VerticalContentAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            panel.Children.Add(_benchMinNTextBox);

            // N до
            panel.Children.Add(new TextBlock { Text = "до:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) });
            var initialInfo = ComplexityBenchmarkService.GetAlgorithmInfo(_benchAlgorithmComboBox.SelectedItem?.ToString() ?? "Кривая Леви");
            _benchMaxNTextBox = new TextBox
            {
                Text = initialInfo.DefaultMaxN.ToString(),
                Width = 46,
                Height = 26,
                VerticalContentAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            panel.Children.Add(_benchMaxNTextBox);

            // Шаг
            panel.Children.Add(new TextBlock { Text = "Шаг:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) });
            _benchStepTextBox = new TextBox
            {
                Text = "1",
                Width = 36,
                Height = 26,
                VerticalContentAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            panel.Children.Add(_benchStepTextBox);

            // Замеров
            panel.Children.Add(new TextBlock { Text = "Замеров:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) });
            _benchRepsTextBox = new TextBox
            {
                Text = "3",
                Width = 36,
                Height = 26,
                VerticalContentAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 15, 0)
            };
            panel.Children.Add(_benchRepsTextBox);

            // Чекбокс Log
            _benchLogScaleCheckBox = new CheckBox
            {
                Content = "Логарифмическая шкала (Log₁₀)",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 15, 0)
            };
            _benchLogScaleCheckBox.Checked += (s, e) => RenderComplexityChart();
            _benchLogScaleCheckBox.Unchecked += (s, e) => RenderComplexityChart();
            panel.Children.Add(_benchLogScaleCheckBox);

            // Кнопка Запустить
            _benchStartButton = new Button
            {
                Content = "▶  Запустить замеры",
                Width = 145,
                Height = 26,
                Margin = new Thickness(0, 0, 6, 0)
            };
            _benchStartButton.Click += async (s, e) => await StartBenchmarkAsync();
            panel.Children.Add(_benchStartButton);

            // Кнопка Стоп
            _benchStopButton = new Button
            {
                Content = "⏹  Остановить",
                Width = 95,
                Height = 26,
                IsEnabled = false,
                Margin = new Thickness(0, 0, 6, 0)
            };
            _benchStopButton.Click += (s, e) => StopBenchmark();
            panel.Children.Add(_benchStopButton);

            // Кнопка Очистить
            _benchClearButton = new Button
            {
                Content = "Очистить",
                Width = 75,
                Height = 26,
                Margin = new Thickness(0, 0, 5, 0)
            };
            _benchClearButton.Click += (s, e) => ClearBenchmark();
            panel.Children.Add(_benchClearButton);

            controlBox.Content = panel;
            Grid.SetRow(controlBox, 0);
            root.Children.Add(controlBox);

            // 2. Основная рабочая область (График слева + Карточка и Таблица справа)
            Grid centerGrid = new Grid();
            centerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
            centerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });

            // Левая колонка: Canvas графика
            Border canvasBorder = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(205, 205, 215)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 10, 0)
            };

            _benchCanvas = new Canvas
            {
                Background = Brushes.White,
                ClipToBounds = true
            };
            _benchCanvas.SizeChanged += (s, e) => RenderComplexityChart();
            canvasBorder.Child = _benchCanvas;
            Grid.SetColumn(canvasBorder, 0);
            centerGrid.Children.Add(canvasBorder);

            // Правая колонка: Карточка аппроксимации и таблица результатов
            Grid rightGrid = new Grid();
            rightGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rightGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            GroupBox summaryBox = new GroupBox
            {
                Header = "Теоретическая модель",
                FontWeight = FontWeights.Normal,
                Margin = new Thickness(0, 0, 0, 8)
            };
            StackPanel summaryStack = new StackPanel { Margin = new Thickness(6) };

            _benchSummaryFormula = new TextBlock
            {
                Text = "Формула: " + initialInfo.FormulaDescription,
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 4),
                Foreground = new SolidColorBrush(Color.FromRgb(180, 20, 20))
            };
            summaryStack.Children.Add(_benchSummaryFormula);

            _benchSummaryC = new TextBlock
            {
                Text = "Коэффициент C: — (вычисляется по МНК)",
                FontFamily = new FontFamily("Consolas, Segoe UI"),
                FontSize = 11,
                Margin = new Thickness(0, 0, 0, 2)
            };
            summaryStack.Children.Add(_benchSummaryC);

            _benchSummaryR2 = new TextBlock
            {
                Text = "Коэффициент детерминации R²: —",
                FontFamily = new FontFamily("Consolas, Segoe UI"),
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 120, 30))
            };
            summaryStack.Children.Add(_benchSummaryR2);

            summaryBox.Content = summaryStack;
            Grid.SetRow(summaryBox, 0);
            rightGrid.Children.Add(summaryBox);

            GroupBox tableBox = new GroupBox
            {
                Header = "Журнал измерений",
                FontWeight = FontWeights.Normal
            };

            _benchListView = new ListView
            {
                FontFamily = new FontFamily("Consolas, Segoe UI"),
                FontSize = 11,
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent
            };
            var gridView = new GridView();
            gridView.Columns.Add(new GridViewColumn { Header = "N", DisplayMemberBinding = new System.Windows.Data.Binding("N"), Width = 38 });
            gridView.Columns.Add(new GridViewColumn { Header = "T факт", DisplayMemberBinding = new System.Windows.Data.Binding("Empirical"), Width = 88 });
            gridView.Columns.Add(new GridViewColumn { Header = "T теор", DisplayMemberBinding = new System.Windows.Data.Binding("Theoretical"), Width = 88 });
            gridView.Columns.Add(new GridViewColumn { Header = "Погреш.", DisplayMemberBinding = new System.Windows.Data.Binding("Error"), Width = 68 });
            _benchListView.View = gridView;

            tableBox.Content = _benchListView;
            Grid.SetRow(tableBox, 1);
            rightGrid.Children.Add(tableBox);

            Grid.SetColumn(rightGrid, 1);
            centerGrid.Children.Add(rightGrid);

            Grid.SetRow(centerGrid, 1);
            root.Children.Add(centerGrid);

            // 3. Нижняя статусная строка
            Grid statusGrid = new Grid { Margin = new Thickness(0, 8, 0, 0) };
            statusGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            statusGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _benchProgressBar = new ProgressBar
            {
                Height = 5,
                Margin = new Thickness(0, 0, 0, 4),
                Visibility = Visibility.Collapsed
            };
            Grid.SetRow(_benchProgressBar, 0);
            statusGrid.Children.Add(_benchProgressBar);

            _benchStatusText = new TextBlock
            {
                Text = "Готов к запуску. Задайте диапазон N и нажмите 'Запустить замеры'.",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(60, 60, 70))
            };
            Grid.SetRow(_benchStatusText, 1);
            statusGrid.Children.Add(_benchStatusText);

            Grid.SetRow(statusGrid, 2);
            root.Children.Add(statusGrid);

            _benchCanvas.Loaded += (s, e) => RenderComplexityChart();

            return root;
        }

        private void BenchAlgorithmComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string? selected = _benchAlgorithmComboBox.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selected)) return;

            var info = ComplexityBenchmarkService.GetAlgorithmInfo(selected);
            _benchMinNTextBox.Text = info.DefaultMinN.ToString();
            _benchMaxNTextBox.Text = info.DefaultMaxN.ToString();

            _benchPoints.Clear();
            UpdateBenchmarkTable();
            RenderComplexityChart();
            _benchStatusText.Text = $"Выбран алгоритм '{selected}' ({info.ComplexityBigO}). Нажмите 'Запустить замеры'.";
        }

        private async Task StartBenchmarkAsync()
        {
            string? selectedAlgo = _benchAlgorithmComboBox.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedAlgo)) return;

            if (!int.TryParse(_benchMinNTextBox.Text, out int minN) || minN < 0)
            {
                MessageBox.Show("Введите корректное число для начальной глубины N (>= 0).", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(_benchMaxNTextBox.Text, out int maxN) || maxN < minN)
            {
                MessageBox.Show("Конечная глубина N должна быть >= начальной глубины.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(_benchStepTextBox.Text, out int step) || step < 1) step = 1;
            if (!int.TryParse(_benchRepsTextBox.Text, out int reps) || reps < 1) reps = 1;

            _benchPoints.Clear();
            UpdateBenchmarkTable();
            RenderComplexityChart();

            _benchStartButton.IsEnabled = false;
            _benchStopButton.IsEnabled = true;
            _benchClearButton.IsEnabled = false;
            _benchAlgorithmComboBox.IsEnabled = false;
            _benchMinNTextBox.IsEnabled = false;
            _benchMaxNTextBox.IsEnabled = false;
            _benchStepTextBox.IsEnabled = false;
            _benchRepsTextBox.IsEnabled = false;
            _benchLogScaleCheckBox.IsEnabled = false;

            _benchProgressBar.Visibility = Visibility.Visible;
            _benchProgressBar.IsIndeterminate = false;
            _benchProgressBar.Minimum = minN;
            _benchProgressBar.Maximum = maxN;
            _benchProgressBar.Value = minN;

            _benchCts = new CancellationTokenSource();
            var info = ComplexityBenchmarkService.GetAlgorithmInfo(selectedAlgo);

            Stopwatch totalSw = Stopwatch.StartNew();

            try
            {
                int totalPoints = (maxN - minN) / step + 1;
                int currentPoint = 0;

                for (int n = minN; n <= maxN; n += step)
                {
                    if (_benchCts.IsCancellationRequested) break;

                    _benchStatusText.Text = $"Замеряем {selectedAlgo} при N = {n} ({currentPoint + 1}/{totalPoints})...";
                    _benchProgressBar.Value = n;

                    double measuredMs = await ComplexityBenchmarkService.MeasureStepTimeMsAsync(selectedAlgo, n, reps, _benchCts.Token);

                    var pt = new BenchmarkDataPoint
                    {
                        N = n,
                        EmpiricalTimeMs = measuredMs
                    };
                    _benchPoints.Add(pt);

                    double c = ComplexityBenchmarkService.CalculateLeastSquaresC(_benchPoints, info.TheoreticalBaseFunc);
                    foreach (var p in _benchPoints)
                    {
                        p.TheoreticalTimeMs = c * info.TheoreticalBaseFunc(p.N);
                        if (p.EmpiricalTimeMs > 0)
                        {
                            p.ErrorPercent = Math.Abs(p.EmpiricalTimeMs - p.TheoreticalTimeMs) / p.EmpiricalTimeMs * 100.0;
                        }
                    }

                    RenderComplexityChart();
                    UpdateBenchmarkTable();

                    currentPoint++;
                }

                totalSw.Stop();
                if (_benchCts.IsCancellationRequested)
                {
                    _benchStatusText.Text = $"Замеры остановлены пользователем. Получено {_benchPoints.Count} точек за {totalSw.ElapsedMilliseconds} мс.";
                }
                else
                {
                    _benchStatusText.Text = $"Замеры успешно завершены! {_benchPoints.Count} точек измерено за {totalSw.ElapsedMilliseconds} мс.";
                }
            }
            catch (OperationCanceledException)
            {
                _benchStatusText.Text = "Замеры отменены пользователем.";
            }
            catch (Exception ex)
            {
                _benchStatusText.Text = $"Ошибка: {ex.Message}";
            }
            finally
            {
                _benchStartButton.IsEnabled = true;
                _benchStopButton.IsEnabled = false;
                _benchClearButton.IsEnabled = true;
                _benchAlgorithmComboBox.IsEnabled = true;
                _benchMinNTextBox.IsEnabled = true;
                _benchMaxNTextBox.IsEnabled = true;
                _benchStepTextBox.IsEnabled = true;
                _benchRepsTextBox.IsEnabled = true;
                _benchLogScaleCheckBox.IsEnabled = true;
                _benchProgressBar.Visibility = Visibility.Collapsed;
                _benchCts?.Dispose();
                _benchCts = null;
            }
        }

        private void StopBenchmark()
        {
            _benchCts?.Cancel();
            _benchStopButton.IsEnabled = false;
            _benchStatusText.Text = "Остановка замеров...";
        }

        private void ClearBenchmark()
        {
            _benchPoints.Clear();
            UpdateBenchmarkTable();
            RenderComplexityChart();
            _benchStatusText.Text = "График очищен. Готов к новым замерам.";
        }

        private void RenderComplexityChart()
        {
            if (_benchCanvas == null) return;
            _benchCanvas.Children.Clear();

            double width = _benchCanvas.ActualWidth;
            double height = _benchCanvas.ActualHeight;
            if (width < 120 || height < 120) return;

            string? selectedAlgo = _benchAlgorithmComboBox.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedAlgo)) selectedAlgo = "Кривая Леви";
            var info = ComplexityBenchmarkService.GetAlgorithmInfo(selectedAlgo);

            double marginLeft = 85.0;
            double marginRight = 35.0;
            double marginTop = 45.0;
            double marginBottom = 55.0;
            double plotWidth = Math.Max(50.0, width - marginLeft - marginRight);
            double plotHeight = Math.Max(50.0, height - marginTop - marginBottom);

            // Фоновый прямоугольник области графика
            var plotBg = new System.Windows.Shapes.Rectangle
            {
                Width = plotWidth,
                Height = plotHeight,
                Fill = new SolidColorBrush(Color.FromRgb(253, 253, 254)),
                Stroke = new SolidColorBrush(Color.FromRgb(220, 220, 225)),
                StrokeThickness = 1
            };
            Canvas.SetLeft(plotBg, marginLeft);
            Canvas.SetTop(plotBg, marginTop);
            _benchCanvas.Children.Add(plotBg);

            // Диапазон X
            int minN = 1;
            int maxN = 12;
            if (int.TryParse(_benchMinNTextBox?.Text, out int parsedMinN) && parsedMinN >= 0) minN = parsedMinN;
            if (int.TryParse(_benchMaxNTextBox?.Text, out int parsedMaxN) && parsedMaxN > minN) maxN = parsedMaxN;

            if (_benchPoints.Count > 0)
            {
                minN = Math.Min(minN, _benchPoints.Min(p => p.N));
                maxN = Math.Max(maxN, _benchPoints.Max(p => p.N));
            }
            if (maxN <= minN) maxN = minN + 5;

            // Коэффициент C и R^2
            double c = ComplexityBenchmarkService.CalculateLeastSquaresC(_benchPoints, info.TheoreticalBaseFunc);
            bool isPreviewTheory = false;
            if (c <= 0)
            {
                c = 5e-6; // 5 нс на шаг для красивого превью до нажатия кнопки
                isPreviewTheory = true;
            }
            double r2 = _benchPoints.Count >= 2 
                ? ComplexityBenchmarkService.CalculateR2(_benchPoints, info.TheoreticalBaseFunc, c) 
                : 1.0;

            bool isLogScale = _benchLogScaleCheckBox?.IsChecked == true;

            double maxEmp = _benchPoints.Count > 0 ? _benchPoints.Max(p => p.EmpiricalTimeMs) : 0.0;
            double maxTheo = c * info.TheoreticalBaseFunc(maxN);
            double maxY = Math.Max(maxEmp, maxTheo);
            if (maxY <= 0.001) maxY = 1.0;
            maxY *= 1.2;

            double minY = 0.0;
            if (isLogScale)
            {
                double minEmp = _benchPoints.Any(p => p.EmpiricalTimeMs > 0) 
                    ? _benchPoints.Where(p => p.EmpiricalTimeMs > 0).Select(p => p.EmpiricalTimeMs).Min() 
                    : 0.001;
                double minTheo = c * info.TheoreticalBaseFunc(minN);
                minY = Math.Max(1e-5, Math.Min(minEmp, minTheo) * 0.5);
            }

            Func<double, double> mapX = n => marginLeft + ((n - minN) / (double)(maxN - minN)) * plotWidth;
            Func<double, double> mapY;

            if (isLogScale)
            {
                double logMin = Math.Log10(Math.Max(1e-6, minY));
                double logMax = Math.Log10(Math.Max(1e-5, maxY));
                if (Math.Abs(logMax - logMin) < 0.0001) logMax = logMin + 1.0;

                mapY = t =>
                {
                    double logT = Math.Log10(Math.Max(1e-6, t));
                    double frac = (logT - logMin) / (logMax - logMin);
                    return marginTop + plotHeight - Math.Clamp(frac, 0.0, 1.0) * plotHeight;
                };
            }
            else
            {
                mapY = t =>
                {
                    double frac = (t - minY) / (maxY - minY);
                    return marginTop + plotHeight - Math.Clamp(frac, 0.0, 1.0) * plotHeight;
                };
            }

            // 1. Отрисовка сетки и меток по оси X
            int xStep = Math.Max(1, (maxN - minN) / 10);
            for (int n = minN; n <= maxN; n += xStep)
            {
                double cx = mapX(n);

                var gridLine = new System.Windows.Shapes.Line
                {
                    X1 = cx, Y1 = marginTop,
                    X2 = cx, Y2 = marginTop + plotHeight,
                    Stroke = new SolidColorBrush(Color.FromRgb(235, 235, 240)),
                    StrokeThickness = 1
                };
                _benchCanvas.Children.Add(gridLine);

                var tick = new System.Windows.Shapes.Line
                {
                    X1 = cx, Y1 = marginTop + plotHeight,
                    X2 = cx, Y2 = marginTop + plotHeight + 5,
                    Stroke = new SolidColorBrush(Color.FromRgb(140, 140, 150)),
                    StrokeThickness = 1
                };
                _benchCanvas.Children.Add(tick);

                var label = new TextBlock
                {
                    Text = n.ToString(),
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(70, 70, 80))
                };
                label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(label, cx - label.DesiredSize.Width / 2.0);
                Canvas.SetTop(label, marginTop + plotHeight + 8);
                _benchCanvas.Children.Add(label);
            }

            // 2. Отрисовка сетки и меток по оси Y
            int yTicksCount = 6;
            for (int i = 0; i <= yTicksCount; i++)
            {
                double val;
                if (isLogScale)
                {
                    double logMin = Math.Log10(minY);
                    double logMax = Math.Log10(maxY);
                    val = Math.Pow(10, logMin + (logMax - logMin) * i / yTicksCount);
                }
                else
                {
                    val = minY + (maxY - minY) * i / yTicksCount;
                }

                double cy = mapY(val);

                var gridLine = new System.Windows.Shapes.Line
                {
                    X1 = marginLeft, Y1 = cy,
                    X2 = marginLeft + plotWidth, Y2 = cy,
                    Stroke = new SolidColorBrush(Color.FromRgb(235, 235, 240)),
                    StrokeThickness = 1
                };
                _benchCanvas.Children.Add(gridLine);

                var tick = new System.Windows.Shapes.Line
                {
                    X1 = marginLeft - 5, Y1 = cy,
                    X2 = marginLeft, Y2 = cy,
                    Stroke = new SolidColorBrush(Color.FromRgb(140, 140, 150)),
                    StrokeThickness = 1
                };
                _benchCanvas.Children.Add(tick);

                string valText = FormatTimeValue(val);
                var label = new TextBlock
                {
                    Text = valText,
                    FontSize = 10,
                    FontFamily = new FontFamily("Consolas, Segoe UI"),
                    Foreground = new SolidColorBrush(Color.FromRgb(70, 70, 80)),
                    TextAlignment = TextAlignment.Right
                };
                label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(label, marginLeft - label.DesiredSize.Width - 8);
                Canvas.SetTop(label, cy - label.DesiredSize.Height / 2.0);
                _benchCanvas.Children.Add(label);
            }

            // Оси
            var axisX = new System.Windows.Shapes.Line
            {
                X1 = marginLeft, Y1 = marginTop + plotHeight,
                X2 = marginLeft + plotWidth + 8, Y2 = marginTop + plotHeight,
                Stroke = new SolidColorBrush(Color.FromRgb(100, 100, 110)),
                StrokeThickness = 1.5
            };
            _benchCanvas.Children.Add(axisX);

            var axisY = new System.Windows.Shapes.Line
            {
                X1 = marginLeft, Y1 = marginTop - 8,
                X2 = marginLeft, Y2 = marginTop + plotHeight,
                Stroke = new SolidColorBrush(Color.FromRgb(100, 100, 110)),
                StrokeThickness = 1.5
            };
            _benchCanvas.Children.Add(axisY);

            // Подписи осей
            var labelXAxis = new TextBlock
            {
                Text = "Глубина рекурсии (N)",
                FontSize = 12,
                FontWeight = FontWeights.Normal,
                Foreground = new SolidColorBrush(Color.FromRgb(50, 50, 60))
            };
            labelXAxis.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(labelXAxis, marginLeft + (plotWidth - labelXAxis.DesiredSize.Width) / 2.0);
            Canvas.SetTop(labelXAxis, marginTop + plotHeight + 30);
            _benchCanvas.Children.Add(labelXAxis);

            var labelYAxis = new TextBlock
            {
                Text = isLogScale ? "Время (мс, Log₁₀ шкала)" : "Время выполнения (мс)",
                FontSize = 11,
                FontWeight = FontWeights.Normal,
                Foreground = new SolidColorBrush(Color.FromRgb(50, 50, 60))
            };
            Canvas.SetLeft(labelYAxis, marginLeft);
            Canvas.SetTop(labelYAxis, marginTop - 22);
            _benchCanvas.Children.Add(labelYAxis);

            // 3. ТЕОРЕТИЧЕСКАЯ КРИВАЯ (Красная пунктирная линия)
            var theoryPoints = new PointCollection();
            int samples = 120;
            for (int i = 0; i <= samples; i++)
            {
                double nVal = minN + (maxN - minN) * (i / (double)samples);
                double tVal = c * info.TheoreticalBaseFunc((int)Math.Round(nVal));
                if (double.IsInfinity(tVal) || double.IsNaN(tVal)) continue;

                double px = mapX(nVal);
                double py = mapY(tVal);
                theoryPoints.Add(new Point(px, py));
            }

            if (theoryPoints.Count > 1)
            {
                var theoryLine = new System.Windows.Shapes.Polyline
                {
                    Points = theoryPoints,
                    Stroke = new SolidColorBrush(Color.FromRgb(229, 57, 53)),
                    StrokeThickness = 2.2,
                    StrokeDashArray = new DoubleCollection { 5, 3 }
                };
                _benchCanvas.Children.Add(theoryLine);
            }

            // 4. ЭМПИРИЧЕСКАЯ КРИВАЯ (Синяя сплошная линия + точки)
            if (_benchPoints.Count > 0)
            {
                var empPoints = new PointCollection();
                foreach (var pt in _benchPoints)
                {
                    double px = mapX(pt.N);
                    double py = mapY(pt.EmpiricalTimeMs);
                    empPoints.Add(new Point(px, py));
                }

                if (empPoints.Count > 1)
                {
                    var empLine = new System.Windows.Shapes.Polyline
                    {
                        Points = empPoints,
                        Stroke = new SolidColorBrush(Color.FromRgb(30, 136, 229)),
                        StrokeThickness = 2.4
                    };
                    _benchCanvas.Children.Add(empLine);
                }

                foreach (var pt in _benchPoints)
                {
                    double px = mapX(pt.N);
                    double py = mapY(pt.EmpiricalTimeMs);
                    double dotR = 4.5;

                    var dot = new System.Windows.Shapes.Ellipse
                    {
                        Width = dotR * 2,
                        Height = dotR * 2,
                        Fill = Brushes.White,
                        Stroke = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
                        StrokeThickness = 2.0,
                        ToolTip = $"Глубина N = {pt.N}\n" +
                                  $"Фактическое время: {FormatTimeValue(pt.EmpiricalTimeMs)}\n" +
                                  $"Теоретическое время: {FormatTimeValue(pt.TheoreticalTimeMs)}\n" +
                                  $"Погрешность: {pt.ErrorPercent:F1}%"
                    };
                    Canvas.SetLeft(dot, px - dotR);
                    Canvas.SetTop(dot, py - dotR);
                    _benchCanvas.Children.Add(dot);
                }
            }

            // 5. Легенда и карточка аппроксимации
            DrawChartLegend(marginLeft, marginTop, plotWidth, info, c, r2, isPreviewTheory);
        }

        private void DrawChartLegend(double marginLeft, double marginTop, double plotWidth,
            BenchmarkAlgorithmInfo info, double c, double r2, bool isPreview)
        {
            Border legendBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(235, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 210)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 6, 10, 6),
                CornerRadius = new CornerRadius(3)
            };

            StackPanel legendStack = new StackPanel { Orientation = Orientation.Vertical };

            // Синяя линия: эмпирические замеры
            StackPanel empItem = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 3) };
            empItem.Children.Add(new System.Windows.Shapes.Line { X1 = 0, Y1 = 5, X2 = 18, Y2 = 5, Stroke = new SolidColorBrush(Color.FromRgb(30, 136, 229)), StrokeThickness = 2.5, VerticalAlignment = VerticalAlignment.Center });
            empItem.Children.Add(new System.Windows.Shapes.Ellipse { Width = 7, Height = 7, Fill = Brushes.White, Stroke = new SolidColorBrush(Color.FromRgb(30, 136, 229)), StrokeThickness = 1.8, Margin = new Thickness(-12, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center });
            empItem.Children.Add(new TextBlock { Text = "Эмпирические замеры (Т факт)", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(30, 30, 30)) });
            legendStack.Children.Add(empItem);

            // Красная линия: теоретическая аппроксимация
            StackPanel theoItem = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            theoItem.Children.Add(new System.Windows.Shapes.Line { X1 = 0, Y1 = 5, X2 = 18, Y2 = 5, Stroke = new SolidColorBrush(Color.FromRgb(229, 57, 53)), StrokeThickness = 2.2, StrokeDashArray = new DoubleCollection { 4, 3 }, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center });
            string theoTitle = isPreview ? $"Теория: {info.FormulaDescription} (ожидание замеров)" : $"Теория: {info.FormulaDescription}";
            theoItem.Children.Add(new TextBlock { Text = theoTitle, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(180, 20, 20)) });
            legendStack.Children.Add(theoItem);

            string cStr = c < 0.001 ? $"{c:E2}" : $"{c:F4}";
            TextBlock fitInfo = new TextBlock
            {
                Text = isPreview
                    ? $"Сложность: {info.ComplexityBigO}"
                    : $"Сложность: {info.ComplexityBigO} | C = {cStr} ms | R² = {r2:F4}",
                FontSize = 10,
                FontFamily = new FontFamily("Consolas, Segoe UI"),
                Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 90))
            };
            legendStack.Children.Add(fitInfo);

            legendBorder.Child = legendStack;
            legendBorder.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(legendBorder, marginLeft + plotWidth - legendBorder.DesiredSize.Width - 10);
            Canvas.SetTop(legendBorder, marginTop + 10);
            _benchCanvas.Children.Add(legendBorder);
        }

        private void UpdateBenchmarkTable()
        {
            if (_benchListView == null) return;
            var rows = _benchPoints.Select(p => new BenchmarkRowItem
            {
                N = p.N,
                Empirical = FormatTimeValue(p.EmpiricalTimeMs),
                Theoretical = FormatTimeValue(p.TheoreticalTimeMs),
                Error = p.EmpiricalTimeMs > 0 ? $"{p.ErrorPercent:F1}%" : "0%"
            }).ToList();
            _benchListView.ItemsSource = rows;

            string? selectedAlgo = _benchAlgorithmComboBox.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedAlgo)) selectedAlgo = "Кривая Леви";
            var info = ComplexityBenchmarkService.GetAlgorithmInfo(selectedAlgo);

            double c = ComplexityBenchmarkService.CalculateLeastSquaresC(_benchPoints, info.TheoreticalBaseFunc);
            double r2 = ComplexityBenchmarkService.CalculateR2(_benchPoints, info.TheoreticalBaseFunc, c);

            _benchSummaryFormula.Text = $"Формула: {info.FormulaDescription} ({info.ComplexityBigO})";
            if (_benchPoints.Count > 0 && c > 0)
            {
                string cStr = c < 0.001 ? $"{c:E3}" : $"{c:F5}";
                _benchSummaryC.Text = $"Коэффициент C = {cStr} мс/шаг";
                _benchSummaryR2.Text = $"Коэффициент детерминации R² = {r2:F4} (точность {(r2 * 100.0):F1}%)";
            }
            else
            {
                _benchSummaryC.Text = "Коэффициент C: — (вычисляется по МНК)";
                _benchSummaryR2.Text = "Коэффициент детерминации R²: —";
            }
        }

        private static string FormatTimeValue(double ms)
        {
            if (ms < 0.001) return $"{ms * 1000.0:F1} μs";
            if (ms < 1.0) return $"{ms:F3} ms";
            if (ms < 1000.0) return $"{ms:F1} ms";
            return $"{ms / 1000.0:F2} s";
        }
    }

    public class BenchmarkRowItem
    {
        public int N { get; set; }
        public string Empirical { get; set; } = "";
        public string Theoretical { get; set; } = "";
        public string Error { get; set; } = "";
    }
}
