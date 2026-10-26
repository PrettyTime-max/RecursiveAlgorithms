using RecursiveAlgorithms;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FractalDrawer
{
    public partial class MainWindow : Window
    {
        private void InitializeUI()
        {
            this.Title = "Генератор фракталов и рекурсивных алгоритмов";
            this.Width = 1040;
            this.Height = 720;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // 1. Стиль для скругленных (овальных) кнопок
            Style roundedButtonStyle = new Style(typeof(Button));
            ControlTemplate buttonTemplate = new ControlTemplate(typeof(Button));

            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(240, 240, 245)));
            borderFactory.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(200, 200, 210)));
            borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(13));
            borderFactory.SetValue(Border.PaddingProperty, new Thickness(10, 0, 10, 0));

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(contentPresenter);

            buttonTemplate.VisualTree = borderFactory;
            roundedButtonStyle.Setters.Add(new Setter(Button.TemplateProperty, buttonTemplate));
            roundedButtonStyle.Setters.Add(new Setter(Button.ForegroundProperty, new SolidColorBrush(Color.FromRgb(40, 40, 40))));
            roundedButtonStyle.Setters.Add(new Setter(Button.CursorProperty, Cursors.Hand));

            this.Resources.Add(typeof(Button), roundedButtonStyle);

            // 2. Стиль для скругленных полей ввода (TextBox)
            Style roundedTextBoxStyle = new Style(typeof(TextBox));
            ControlTemplate textBoxTemplate = new ControlTemplate(typeof(TextBox));

            var tbBorderFactory = new FrameworkElementFactory(typeof(Border));
            tbBorderFactory.SetValue(Border.BackgroundProperty, Brushes.White);
            tbBorderFactory.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(200, 200, 210)));
            tbBorderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            tbBorderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(12));
            tbBorderFactory.SetValue(Border.PaddingProperty, new Thickness(6, 0, 6, 0));

            var tbScrollViewer = new FrameworkElementFactory(typeof(ScrollViewer));
            tbScrollViewer.Name = "PART_ContentHost";
            tbBorderFactory.AppendChild(tbScrollViewer);

            textBoxTemplate.VisualTree = tbBorderFactory;
            roundedTextBoxStyle.Setters.Add(new Setter(TextBox.TemplateProperty, textBoxTemplate));
            roundedTextBoxStyle.Setters.Add(new Setter(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Center));
            roundedTextBoxStyle.Setters.Add(new Setter(TextBox.ForegroundProperty, new SolidColorBrush(Color.FromRgb(40, 40, 40))));
            this.Resources.Add(typeof(TextBox), roundedTextBoxStyle);

            Grid mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // --- Панель управления ---
            Border controlOuterBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(250, 250, 252)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(230, 230, 235)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(8, 6, 8, 6)
            };

            WrapPanel controlPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Алгоритм
            controlPanel.Children.Add(new TextBlock
            {
                Text = "Алгоритм:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0),
                FontWeight = FontWeights.Normal
            });

            // 3. Создаем стандартный ComboBox в скругленной капсуле
            _algorithmComboBox = new ComboBox
            {
                Width = 192,
                Height = 24,
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0)
            };

            foreach (var key in _algorithms.Keys)
            {
                _algorithmComboBox.Items.Add(key);
            }
            if (_algorithmComboBox.Items.Count > 0) _algorithmComboBox.SelectedIndex = 0;
            _algorithmComboBox.SelectionChanged += AlgorithmComboBox_SelectionChanged;

            // Обертка-капсула, которая идеально совпадает по стилю с полями ввода и кнопками
            Border comboWrapper = new Border
            {
                Width = 200,
                Height = 26,
                CornerRadius = new CornerRadius(13),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 210)),
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                Margin = new Thickness(0, 0, 16, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(4, 0, 2, 0),
                Child = _algorithmComboBox
            };

            controlPanel.Children.Add(comboWrapper);

            // Рекурсии (N)
            StackPanel depthInnerStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 16, 0)
            };

            depthInnerStack.Children.Add(new TextBlock
            {
                Text = "Рекурсии (N):",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0),
                FontWeight = FontWeights.Normal
            });

            _depthSlider = new Slider
            {
                Minimum = 1,
                Maximum = 16,
                Value = 8,
                Width = 85,
                IsSnapToTickEnabled = true,
                TickFrequency = 1,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0),
                SmallChange = 1,
                LargeChange = 2
            };

            _depthLabel = new TextBlock
            {
                Text = "8",
                Width = 24,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
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
            controlPanel.Children.Add(depthInnerStack);

            _useStackCheckBox = new CheckBox
            {
                Content = "Через Стек",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 16, 0),
                Visibility = Visibility.Collapsed,
                FontWeight = FontWeights.Normal
            };
            controlPanel.Children.Add(_useStackCheckBox);

            // Задержка
            StackPanel speedInnerStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 16, 0)
            };

            speedInnerStack.Children.Add(new TextBlock
            {
                Text = "Задержка:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0),
                FontWeight = FontWeights.Normal
            });

            _speedSlider = new Slider
            {
                Minimum = 10,
                Maximum = 400,
                Value = 50,
                Width = 85,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0),
                SmallChange = 10,
                LargeChange = 50
            };
            _speedLabel = new TextBlock
            {
                Text = "50 мс",
                Width = 42,
                VerticalAlignment = VerticalAlignment.Center,
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
            controlPanel.Children.Add(speedInnerStack);

            _generateButton = new Button
            {
                Content = "Запустить",
                Width = 90,
                Height = 26,
                Margin = new Thickness(0, 0, 6, 0)
            };
            _generateButton.Click += (s, e) => ExecuteDrawAnimation();
            controlPanel.Children.Add(_generateButton);

            _stopButton = new Button
            {
                Content = "Стоп",
                Width = 70,
                Height = 26,
                IsEnabled = false,
                Margin = new Thickness(0, 0, 5, 0)
            };
            _stopButton.Click += (s, e) => { _isAnimating = false; };
            controlPanel.Children.Add(_stopButton);

            controlOuterBorder.Child = controlPanel;
            Grid.SetRow(controlOuterBorder, 0);
            mainGrid.Children.Add(controlOuterBorder);

            // --- Статусная строка ---
            _statusGrid = new Grid
            {
                Margin = new Thickness(10, 4, 10, 4),
                Visibility = Visibility.Collapsed
            };
            _statusGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _statusGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _loadingProgressBar = new ProgressBar
            {
                Height = 4,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 2),
                IsIndeterminate = true
            };
            Grid.SetRow(_loadingProgressBar, 0);
            _statusGrid.Children.Add(_loadingProgressBar);

            Grid.SetRow(_statusGrid, 1);
            mainGrid.Children.Add(_statusGrid);

            // --- Холст ---
            Grid canvasContainer = new Grid
            {
                Margin = new Thickness(10, 6, 10, 10)
            };

            Border canvasBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(215, 215, 220)),
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                ClipToBounds = true,
                CornerRadius = new CornerRadius(4)
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

            _fractalCanvas.MouseWheel += FractalCanvas_MouseWheel;
            _fractalCanvas.MouseLeftButtonDown += FractalCanvas_MouseLeftButtonDown;
            _fractalCanvas.MouseMove += FractalCanvas_MouseMove;
            _fractalCanvas.MouseLeftButtonUp += FractalCanvas_MouseLeftButtonUp;
            _fractalCanvas.MouseLeave += FractalCanvas_MouseLeave;

            Border infoOverlayBorder = new Border
            {
                Height = 28,
                Background = new SolidColorBrush(Color.FromArgb(245, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 210)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(12),
                Padding = new Thickness(14, 4, 14, 4)
            };

            _executionTimeTextBlock = new TextBlock
            {
                Text = "Выберите алгоритм и нажмите 'Запустить'",
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 11,
                FontFamily = new FontFamily("Consolas, Segoe UI"),
                Foreground = new SolidColorBrush(Color.FromRgb(50, 50, 60)),
                FontWeight = FontWeights.Normal
            };
            infoOverlayBorder.Child = _executionTimeTextBlock;
            canvasContainer.Children.Add(infoOverlayBorder);

            Grid.SetRow(canvasContainer, 2);
            mainGrid.Children.Add(canvasContainer);

            TabControl rootTabControl = new TabControl
            {
                Background = new SolidColorBrush(Color.FromRgb(248, 249, 250)),
                BorderThickness = new Thickness(0)
            };

            TabItem fractalTab = new TabItem
            {
                Header = "  🎨 Визуализация  ",
                Content = mainGrid,
                FontSize = 12,
                Padding = new Thickness(12, 6, 12, 6)
            };
            rootTabControl.Items.Add(fractalTab);

            if (this.Content is TabControl existingTabs && existingTabs.Items.Count > 1)
            {
                var complexityTab = existingTabs.Items[1] as TabItem;
                if (complexityTab != null) rootTabControl.Items.Add(complexityTab);
            }
            else
            {
                TabItem complexityTab = new TabItem
                {
                    Header = "  📊 Анализ сложности  ",
                    Content = CreateComplexityTabContent(),
                    FontSize = 12,
                    Padding = new Thickness(12, 6, 12, 6)
                };
                rootTabControl.Items.Add(complexityTab);
            }

            this.Content = rootTabControl;
        }
    }
}