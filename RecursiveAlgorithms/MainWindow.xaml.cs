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
        private TextBox _iterationCountTextBox = null!;
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

        private readonly Dictionary<string, IFractalAlgorithm> _algorithms;
        private bool _isAnimating = false;

        // Флаг: был ли уже запущен расчет пользователем через кнопку "Запустить"
        private bool _hasRendered = false;
        private string _lastRenderedAlgorithm = "";
        private int _lastRenderedDepth = 0;
        private int _lastHanoiStep = 0;

        public MainWindow()
        {
            _algorithms = new Dictionary<string, IFractalAlgorithm>
            {
                { "Треугольник Серпинского", new SierpinskiTriangleAlgorithm() },
                { "Кривая Гильберта", new HilbertCurveAlgorithm() },
                { "Ханойская башня", new HanoiTowerAlgorithm() },
                { "Кривая Леви", new LevyCurveFractal() }
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
                FontWeight = FontWeights.Normal // Убираем жирный шрифт у GroupBox
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
                FontWeight = FontWeights.Normal // Обычный шрифт
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

            // N (Глубина / количество дисков)
            controlPanel.Children.Add(new TextBlock
            {
                Text = "Рекурсии (N):",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0),
                FontWeight = FontWeights.Normal
            });

            _iterationCountTextBox = new TextBox
            {
                Text = "8",
                Width = 55,
                Height = 26,
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 15, 0),
                Padding = new Thickness(3),
                FontWeight = FontWeights.Normal
            };
            controlPanel.Children.Add(_iterationCountTextBox);

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

            // Оборачиваем панель с ползунком в рамку (Border)
            Border speedBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(0), 
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
                FontWeight = FontWeights.Normal, // Обычный шрифт вместо SemiBold
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

            this.Content = mainGrid;
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
                _iterationCountTextBox.Text = "4";
                _speedSlider.Value = 120;
            }
            else if (selected == "Треугольник Серпинского")
            {
                _useStackCheckBox.Visibility = Visibility.Collapsed;
                _iterationCountTextBox.Text = "8";
                _speedSlider.Value = 70;
            }
            else if (selected == "Кривая Гильберта")
            {
                _useStackCheckBox.Visibility = Visibility.Collapsed;
                _iterationCountTextBox.Text = "5";
                _speedSlider.Value = 60;
            }
            else // Кривая Леви
            {
                _useStackCheckBox.Visibility = Visibility.Collapsed;
                _iterationCountTextBox.Text = "12";
                _speedSlider.Value = 50;
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

        private async void ZoomFromCenter(double factor)
        {
            int width = (int)(_fractalCanvas.ActualWidth > 50 ? _fractalCanvas.ActualWidth : 800);
            int height = (int)(_fractalCanvas.ActualHeight > 50 ? _fractalCanvas.ActualHeight : 600);
            Point center = new Point(width / 2.0, height / 2.0);

            double oldZoom = _viewZoom;
            double newZoom = Math.Clamp(oldZoom * factor, 0.2, 50000.0);
            if (Math.Abs(newZoom - oldZoom) < 0.00001) return;

            double worldX = (center.X - width / 2.0) / oldZoom + _viewPanX;
            double worldY = (center.Y - height / 2.0) / oldZoom + _viewPanY;

            _viewPanX = worldX - (center.X - width / 2.0) / newZoom;
            _viewPanY = worldY - (center.Y - height / 2.0) / newZoom;
            _viewZoom = newZoom;

            UpdateZoomText();

            if (_hasRendered)
            {
                await RedrawCurrentFrameAsync();
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
            if (_isAnimating || _isRedrawing || !_hasRendered) return;
            _isRedrawing = true;

            try
            {
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
                    // Текст в нижней строке больше не перезаписывается при зуме и сохраняет информацию о времени
                }
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

            if (!int.TryParse(_iterationCountTextBox.Text, out int targetDepth) || targetDepth < 1)
            {
                MessageBox.Show("Введите число рекурсий (больше 0).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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
            long exactTimeMs = sw.ElapsedMilliseconds; // Идеально точное время рекурсии!

            if (!_isAnimating)
            {
                CleanupAfterAnimation("Остановлено");
                return;
            }

            // ==========================================
            // ПРОХОД 2: Красивая покадровая анимация для зрителей
            // ==========================================
            string methodInfo = "";
            if (algorithm is HanoiTowerAlgorithm hanoiAnim)
            {
                hanoiAnim.UseStackAlgorithm = _useStackCheckBox.IsChecked == true;
                int disksCount = Math.Max(1, targetDepth);
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

                        _executionTimeTextBlock.Text = $"[Время: {exactTimeMs} ms] [{methodTitle}] Ход ({Math.Min(move + 1, totalMoves)}/{totalMoves})";

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

                    _executionTimeTextBlock.Text = $"[Время: {exactTimeMs} ms] Уровень рекурсии (N={currentDepth}/{targetDepth})";

                    int delay = (int)_speedSlider.Value;
                    await Task.Delay(delay);
                }
            }

            // На всякий случай выводим финальную точную картинку в конце
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