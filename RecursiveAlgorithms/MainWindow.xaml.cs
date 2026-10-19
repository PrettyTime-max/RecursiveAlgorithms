using RecursiveAlgorithms;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FractalDrawer
{
    public partial class MainWindow : Window
    {
        private ComboBox _algorithmComboBox;
        private TextBox _iterationCountTextBox;
        private Button _generateButton;
        private Button _stopButton; // Кнопка остановки

        private Grid _statusGrid;
        private ProgressBar _loadingProgressBar;
        private TextBlock _executionTimeTextBlock;
        private Canvas _fractalCanvas;

        private readonly Dictionary<string, IFractalAlgorithm> _algorithms;
        private bool _isAnimating = false;

        public MainWindow()
        {
            _algorithms = new Dictionary<string, IFractalAlgorithm>
            {
                { "Кривая дракона", new DragonCurveAlgorithm() },
                { "Треугольник Серпинского", new SierpinskiTriangleAlgorithm() },
                { "Ханойская башня", new HanoiTowerAlgorithm() }
            };

            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Title = "Генератор фракталов";
            this.Width = 980;
            this.Height = 670;

            Grid mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // --- 1. Панель управления ---
            GroupBox controlGroupBox = new GroupBox
            {
                Header = "Панель управления фракталами",
                Margin = new Thickness(10)
            };

            StackPanel controlPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(5)
            };

            controlPanel.Children.Add(new TextBlock { Text = "Алгоритм:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 5, 0) });

            _algorithmComboBox = new ComboBox { Width = 200, Height = 25, VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 15, 0) };
            foreach (var key in _algorithms.Keys)
            {
                _algorithmComboBox.Items.Add(key);
            }
            if (_algorithmComboBox.Items.Count > 0) _algorithmComboBox.SelectedIndex = 0;
            controlPanel.Children.Add(_algorithmComboBox);

            controlPanel.Children.Add(new TextBlock { Text = "Рекурсии (N):", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 5, 0) });

            _iterationCountTextBox = new TextBox { Text = "8", Width = 60, Height = 25, VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 15, 0), Padding = new Thickness(3) };
            controlPanel.Children.Add(_iterationCountTextBox);

            // Кнопка Запустить
            _generateButton = new Button
            {
                Content = "Запустить",
                Width = 90,
                Height = 25,
                FontWeight = FontWeights.Normal,
                Margin = new Thickness(0, 0, 5, 0)
            };
            _generateButton.Click += (s, e) => ExecuteDrawAnimation();
            controlPanel.Children.Add(_generateButton);

            // Кнопка Стоп
            _stopButton = new Button
            {
                Content = "Стоп",
                Width = 70,
                Height = 25,
                FontWeight = FontWeights.Normal,
                IsEnabled = false // Изначально выключена, пока анимация не идет
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
                Height = 8,
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
                FontStyle = FontStyles.Italic
            };
            Grid.SetRow(vpnWarningText, 1);
            _statusGrid.Children.Add(vpnWarningText);

            Grid.SetRow(_statusGrid, 1);
            mainGrid.Children.Add(_statusGrid);

            // --- 3. Холст с наложением белой прямоугольной строки для времени ---
            Grid canvasContainer = new Grid
            {
                Margin = new Thickness(10, 5, 10, 10)
            };

            Border canvasBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(1),
                Background = Brushes.White
            };

            _fractalCanvas = new Canvas { ClipToBounds = true };
            canvasBorder.Child = _fractalCanvas;
            canvasContainer.Children.Add(canvasBorder); // Нижний слой — холст

            // Прямоугольная белая строка с острыми углами поверх холста для времени
            Border timeOverlayBorder = new Border
            {
                Height = 28,
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(12),
                Padding = new Thickness(10, 4, 10, 4)
            };

            _executionTimeTextBlock = new TextBlock
            {
                Text = "Время: -",
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 11,
                FontFamily = new FontFamily("Consolas, Segoe UI"),
                Foreground = new SolidColorBrush(Color.FromRgb(40, 40, 40))
            };

            timeOverlayBorder.Child = _executionTimeTextBlock;
            canvasContainer.Children.Add(timeOverlayBorder); // Верхний слой — строка времени

            Grid.SetRow(canvasContainer, 2);
            mainGrid.Children.Add(canvasContainer);

            this.Content = mainGrid;
        }

        private async void ExecuteDrawAnimation()
        {
            string selectedKey = _algorithmComboBox.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(selectedKey) || !_algorithms.ContainsKey(selectedKey)) return;

            if (!int.TryParse(_iterationCountTextBox.Text, out int targetDepth) || targetDepth < 1)
            {
                MessageBox.Show("Введите число рекурсий (больше 0).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _isAnimating = true;
            _fractalCanvas.Children.Clear();
            _statusGrid.Visibility = Visibility.Visible;
            _generateButton.IsEnabled = false;
            _stopButton.IsEnabled = true; // Включаем кнопку стоп
            _executionTimeTextBlock.Text = "Время: расчет...";

            int width = (int)(_fractalCanvas.ActualWidth > 0 ? _fractalCanvas.ActualWidth : 800);
            int height = (int)(_fractalCanvas.ActualHeight > 0 ? _fractalCanvas.ActualHeight : 600);

            var algorithm = _algorithms[selectedKey];

            Stopwatch totalStopwatch = Stopwatch.StartNew();
            long lastFrameTimeMs = 0;
            int completedSteps = 0;

            // Проверяем, Ханойская ли это башня
            if (algorithm is HanoiTowerAlgorithm hanoiAlg)
            {
                int disksCount = targetDepth;
                int totalMoves = hanoiAlg.GetTotalMoves(disksCount);
                int totalSubSteps = totalMoves * HanoiTowerAlgorithm.SubStepsPerMove;

                for (int step = 0; step <= totalSubSteps; step++)
                {
                    if (!_isAnimating) break;

                    Stopwatch frameStopwatch = Stopwatch.StartNew();
                    var bitmap = hanoiAlg.CalculateStepBitmap(width, height, disksCount, step);
                    frameStopwatch.Stop();

                    if (!_isAnimating) break;

                    lastFrameTimeMs = frameStopwatch.ElapsedMilliseconds;

                    _fractalCanvas.Children.Clear();
                    UIElement fractalImage = FractalRenderer.CreateImage(bitmap);
                    if (fractalImage != null)
                    {
                        _fractalCanvas.Children.Add(fractalImage);
                    }

                    int currentMove = Math.Min(step / HanoiTowerAlgorithm.SubStepsPerMove + 1, totalMoves);
                    _executionTimeTextBlock.Text = $"Ход ({currentMove}/{totalMoves}): {lastFrameTimeMs} ms";

                    // Небольшая задержка для плавного движения
                    await Task.Delay(25);
                }
            }
            else
            {
                // Стандартная логика для фракталов (по глубине рекурсии)
                for (int currentDepth = 1; currentDepth <= targetDepth; currentDepth++)
                {
                    if (!_isAnimating) break;

                    Stopwatch frameStopwatch = Stopwatch.StartNew();
                    var bitmap = await algorithm.CalculateBitmapAsync(width, height, currentDepth);
                    frameStopwatch.Stop();

                    if (!_isAnimating) break;

                    lastFrameTimeMs = frameStopwatch.ElapsedMilliseconds;
                    completedSteps = currentDepth;

                    _fractalCanvas.Children.Clear();
                    UIElement fractalImage = FractalRenderer.CreateImage(bitmap);
                    if (fractalImage != null)
                    {
                        _fractalCanvas.Children.Add(fractalImage);
                    }

                    _executionTimeTextBlock.Text = $"Кадр (N={currentDepth}): {lastFrameTimeMs} ms";

                    int delay = currentDepth > 10 ? 40 : 150;
                    await Task.Delay(delay);
                }
            }

            totalStopwatch.Stop();

            if (!_isAnimating)
            {
                _executionTimeTextBlock.Text = "Остановлено";
            }
            else
            {
                _executionTimeTextBlock.Text = $"Готово! | Всего: {totalStopwatch.ElapsedMilliseconds} ms";
            }

            _statusGrid.Visibility = Visibility.Collapsed;
            _generateButton.IsEnabled = true;
            _stopButton.IsEnabled = false;
            _isAnimating = false;
        }
    }
}