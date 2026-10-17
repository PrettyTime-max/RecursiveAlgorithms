using System.Collections.Generic;
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

        private Grid _statusGrid;
        private ProgressBar _loadingProgressBar;
        private Canvas _fractalCanvas;

        private readonly Dictionary<string, IFractalAlgorithm> _algorithms;
        private bool _isAnimating = false;

        public MainWindow()
        {
            _algorithms = new Dictionary<string, IFractalAlgorithm>
            {
                { "Кривая дракона", new DragonCurveAlgorithm() },
                { "Треугольник Серпинского", new SierpinskiTriangleAlgorithm() }
            };

            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Title = "Генератор фракталов";
            this.Width = 950;
            this.Height = 650;

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

            _iterationCountTextBox = new TextBox { Text = "10", Width = 60, Height = 25, VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 15, 0), Padding = new Thickness(3) };
            controlPanel.Children.Add(_iterationCountTextBox);

            _generateButton = new Button
            {
                Content = "Запустить",
                Width = 100,
                Height = 25,
                FontWeight = FontWeights.Normal
            };
            _generateButton.Click += (s, e) => ExecuteDrawAnimation();
            controlPanel.Children.Add(_generateButton);

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

            // --- 3. Холст ---
            Border canvasBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(10, 5, 10, 10),
                Background = Brushes.White
            };

            _fractalCanvas = new Canvas { ClipToBounds = true };
            canvasBorder.Child = _fractalCanvas;
            Grid.SetRow(canvasBorder, 2);
            mainGrid.Children.Add(canvasBorder);

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

            if (targetDepth > 20) targetDepth = 20;

            _isAnimating = true;
            _fractalCanvas.Children.Clear();
            _statusGrid.Visibility = Visibility.Visible;
            _generateButton.IsEnabled = false;

            int width = (int)(_fractalCanvas.ActualWidth > 0 ? _fractalCanvas.ActualWidth : 800);
            int height = (int)(_fractalCanvas.ActualHeight > 0 ? _fractalCanvas.ActualHeight : 600);

            var algorithm = _algorithms[selectedKey];

            for (int currentDepth = 1; currentDepth <= targetDepth; currentDepth++)
            {
                if (!_isAnimating) break;

                var bitmap = await algorithm.CalculateBitmapAsync(width, height, currentDepth);

                _fractalCanvas.Children.Clear();
                UIElement fractalImage = FractalRenderer.CreateImage(bitmap);
                if (fractalImage != null)
                {
                    _fractalCanvas.Children.Add(fractalImage);
                }

                int delay = currentDepth > 10 ? 40 : 150;
                await Task.Delay(delay);
            }

            _statusGrid.Visibility = Visibility.Collapsed;
            _generateButton.IsEnabled = true;
            _isAnimating = false;
        }
    }
}