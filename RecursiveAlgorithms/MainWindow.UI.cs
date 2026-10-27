using RecursiveAlgorithms;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

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

            // 1. Стиль для скругленных (овальных) кнопок (с белым фоном)
            Style roundedButtonStyle = new Style(typeof(Button));
            ControlTemplate buttonTemplate = new ControlTemplate(typeof(Button));

            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, Brushes.White); // Белый фон кнопки
            borderFactory.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(200, 200, 210)));
            borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(13));
            borderFactory.SetValue(Border.PaddingProperty, new Thickness(10, 0, 10, 0));

            var contentPresenterBtn = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenterBtn.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenterBtn.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(contentPresenterBtn);

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

            // =========================================================================
            // СТИЛИЗАЦИЯ ПОЛЗУНКА (SLIDER) С СЕРЫМ КОНТУРОМ БЕГУНКА
            // =========================================================================
            string sliderXaml = @"
            <Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                   xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                   TargetType='Slider'>
                <Setter Property='Stylus.IsPressAndHoldEnabled' Value='False'/>
                <Setter Property='Background' Value='Transparent'/>
                <Setter Property='BorderBrush' Value='Transparent'/>
                <Setter Property='Template'>
                    <Setter.Value>
                        <ControlTemplate TargetType='Slider'>
                            <Grid VerticalAlignment='Center'>
                                <Border Height='4' CornerRadius='2' Background='#D7D7E1' VerticalAlignment='Center'/>
                                <Track x:Name='PART_Track'>
                                    <Track.DecreaseRepeatButton>
                                        <RepeatButton Command='{x:Static Slider.DecreaseLarge}' Opacity='0'/>
                                    </Track.DecreaseRepeatButton>
                                    <Track.IncreaseRepeatButton>
                                        <RepeatButton Command='{x:Static Slider.IncreaseLarge}' Opacity='0'/>
                                    </Track.IncreaseRepeatButton>
                                    <Track.Thumb>
                                        <Thumb Width='14' Height='14' Cursor='Hand'>
                                            <Thumb.Template>
                                                <ControlTemplate TargetType='Thumb'>
                                                    <Border Background='White' BorderBrush='#B0B0C0' BorderThickness='1' CornerRadius='7'/>
                                                </ControlTemplate>
                                            </Thumb.Template>
                                        </Thumb>
                                    </Track.Thumb>
                                </Track>
                            </Grid>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
            </Style>";

            Style customSliderStyle = (Style)XamlReader.Parse(sliderXaml);

            // =========================================================================
            // СТИЛИЗАЦИЯ ВЫПАДАЮЩЕГО СПИСКА (COMBOBOX)
            // =========================================================================
            Style comboBoxItemStyle = new Style(typeof(ComboBoxItem));
            ControlTemplate itemTemplate = new ControlTemplate(typeof(ComboBoxItem));

            var itemBorder = new FrameworkElementFactory(typeof(Border));
            itemBorder.Name = "Bd";
            itemBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            itemBorder.SetValue(Border.PaddingProperty, new Thickness(8, 4, 8, 4));
            itemBorder.SetValue(Border.MarginProperty, new Thickness(2, 1, 2, 1));
            itemBorder.SetValue(Border.BackgroundProperty, Brushes.Transparent);

            var itemPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            itemPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            itemBorder.AppendChild(itemPresenter);

            itemTemplate.VisualTree = itemBorder;

            Trigger highlightTrigger = new Trigger { Property = ComboBoxItem.IsHighlightedProperty, Value = true };
            highlightTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(230, 235, 245)), "Bd"));
            highlightTrigger.Setters.Add(new Setter(TextElement.ForegroundProperty, new SolidColorBrush(Color.FromRgb(20, 20, 20)), "Bd"));

            Trigger selectedTrigger = new Trigger { Property = ComboBoxItem.IsSelectedProperty, Value = true };
            selectedTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(220, 228, 242)), "Bd"));
            selectedTrigger.Setters.Add(new Setter(TextElement.ForegroundProperty, new SolidColorBrush(Color.FromRgb(0, 0, 0)), "Bd"));

            itemTemplate.Triggers.Add(highlightTrigger);
            itemTemplate.Triggers.Add(selectedTrigger);
            comboBoxItemStyle.Setters.Add(new Setter(ComboBoxItem.TemplateProperty, itemTemplate));
            comboBoxItemStyle.Setters.Add(new Setter(ComboBoxItem.CursorProperty, Cursors.Hand));

            Style customComboBoxStyle = new Style(typeof(ComboBox));
            ControlTemplate comboTemplate = new ControlTemplate(typeof(ComboBox));

            var rootGrid = new FrameworkElementFactory(typeof(Grid));

            var mainBorder = new FrameworkElementFactory(typeof(Border));
            mainBorder.Name = "MainBorder";
            mainBorder.SetValue(Border.BackgroundProperty, Brushes.White);
            mainBorder.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(200, 200, 210)));
            mainBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            mainBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(13));
            mainBorder.SetValue(Border.PaddingProperty, new Thickness(10, 0, 6, 0));

            var innerGrid = new FrameworkElementFactory(typeof(Grid));
            var col1 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col1.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            var col2 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col2.SetValue(ColumnDefinition.WidthProperty, new GridLength(18));
            innerGrid.AppendChild(col1);
            innerGrid.AppendChild(col2);

            var toggleButton = new FrameworkElementFactory(typeof(ToggleButton));
            toggleButton.Name = "ToggleButton";
            toggleButton.SetValue(ToggleButton.BackgroundProperty, Brushes.Transparent);
            toggleButton.SetValue(ToggleButton.BorderBrushProperty, Brushes.Transparent);
            toggleButton.SetValue(ToggleButton.BorderThicknessProperty, new Thickness(0));
            toggleButton.SetValue(Grid.ColumnSpanProperty, 2);
            toggleButton.SetValue(ToggleButton.IsCheckedProperty, new Binding("IsDropDownOpen")
            {
                Mode = BindingMode.TwoWay,
                RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
            });

            var tbTemplate = new ControlTemplate(typeof(ToggleButton));
            var tbGrid = new FrameworkElementFactory(typeof(Grid));
            tbGrid.SetValue(Grid.BackgroundProperty, Brushes.Transparent);
            tbTemplate.VisualTree = tbGrid;
            toggleButton.SetValue(ToggleButton.TemplateProperty, tbTemplate);

            innerGrid.AppendChild(toggleButton);

            var tbHoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            tbHoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(245, 247, 250)), "MainBorder"));
            comboTemplate.Triggers.Add(tbHoverTrigger);

            var tbPressedTrigger = new Trigger { Property = ToggleButton.IsPressedProperty, Value = true };
            tbPressedTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(235, 240, 245)), "MainBorder"));
            comboTemplate.Triggers.Add(tbPressedTrigger);

            var contentPresenterCb = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenterCb.Name = "PART_ContentSite";
            contentPresenterCb.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            contentPresenterCb.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            contentPresenterCb.SetValue(ContentPresenter.IsHitTestVisibleProperty, false);
            contentPresenterCb.SetValue(ContentPresenter.ContentProperty, new Binding("SelectedItem")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
            });
            contentPresenterCb.SetValue(Grid.ColumnProperty, 0);
            innerGrid.AppendChild(contentPresenterCb);

            var arrowPath = new FrameworkElementFactory(typeof(System.Windows.Shapes.Path));
            arrowPath.SetValue(System.Windows.Shapes.Path.DataProperty, Geometry.Parse("M 0 0 L 4 4 L 8 0 Z"));
            arrowPath.SetValue(System.Windows.Shapes.Path.FillProperty, new SolidColorBrush(Color.FromRgb(110, 110, 120)));
            arrowPath.SetValue(System.Windows.Shapes.Path.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            arrowPath.SetValue(System.Windows.Shapes.Path.VerticalAlignmentProperty, VerticalAlignment.Center);
            arrowPath.SetValue(System.Windows.Shapes.Path.IsHitTestVisibleProperty, false);
            arrowPath.SetValue(Grid.ColumnProperty, 1);
            innerGrid.AppendChild(arrowPath);

            mainBorder.AppendChild(innerGrid);
            rootGrid.AppendChild(mainBorder);

            var popup = new FrameworkElementFactory(typeof(Popup));
            popup.Name = "Popup";
            popup.SetValue(Popup.AllowsTransparencyProperty, true);
            popup.SetValue(Popup.FocusableProperty, false);
            popup.SetValue(Popup.IsOpenProperty, new Binding("IsDropDownOpen")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
            });
            popup.SetValue(Popup.PopupAnimationProperty, PopupAnimation.Slide);
            popup.SetValue(Popup.PlacementProperty, PlacementMode.Bottom);

            var dropDownBorder = new FrameworkElementFactory(typeof(Border));
            dropDownBorder.Name = "DropDownBorder";
            dropDownBorder.SetValue(Border.BackgroundProperty, Brushes.White);
            dropDownBorder.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(200, 200, 210)));
            dropDownBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            dropDownBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            dropDownBorder.SetValue(Border.PaddingProperty, new Thickness(3));
            dropDownBorder.SetValue(Border.MarginProperty, new Thickness(0, 3, 0, 0));
            dropDownBorder.SetValue(FrameworkElement.MaxHeightProperty, 220.0);
            dropDownBorder.SetValue(FrameworkElement.MinWidthProperty, new Binding("ActualWidth")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
            });

            var scrollViewer = new FrameworkElementFactory(typeof(ScrollViewer));
            scrollViewer.SetValue(ScrollViewer.SnapsToDevicePixelsProperty, true);

            var itemsPresenter = new FrameworkElementFactory(typeof(ItemsPresenter));
            itemsPresenter.Name = "ItemsPresenter";
            scrollViewer.AppendChild(itemsPresenter);
            dropDownBorder.AppendChild(scrollViewer);
            popup.AppendChild(dropDownBorder);

            rootGrid.AppendChild(popup);
            comboTemplate.VisualTree = rootGrid;

            customComboBoxStyle.Setters.Add(new Setter(ComboBox.TemplateProperty, comboTemplate));

            // =========================================================================
            // СТИЛИЗАЦИЯ CHECKBOX
            // =========================================================================
            Style customCheckBoxStyle = new Style(typeof(CheckBox));
            ControlTemplate checkBoxTemplate = new ControlTemplate(typeof(CheckBox));

            var cbGrid = new FrameworkElementFactory(typeof(Grid));
            var cbCol1 = new FrameworkElementFactory(typeof(ColumnDefinition));
            cbCol1.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);
            var cbCol2 = new FrameworkElementFactory(typeof(ColumnDefinition));
            cbCol2.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            cbGrid.AppendChild(cbCol1);
            cbGrid.AppendChild(cbCol2);

            var cbBorder = new FrameworkElementFactory(typeof(Border));
            cbBorder.Name = "BoxBorder";
            cbBorder.SetValue(Border.WidthProperty, 16.0);
            cbBorder.SetValue(Border.HeightProperty, 16.0);
            cbBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            cbBorder.SetValue(Border.BackgroundProperty, Brushes.White);
            cbBorder.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(190, 190, 205)));
            cbBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            cbBorder.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);

            var checkMark = new FrameworkElementFactory(typeof(System.Windows.Shapes.Path));
            checkMark.Name = "CheckMark";
            checkMark.SetValue(System.Windows.Shapes.Path.DataProperty, Geometry.Parse("M 2 5 L 6 9 L 12 3"));
            checkMark.SetValue(System.Windows.Shapes.Path.StrokeProperty, new SolidColorBrush(Color.FromRgb(30, 136, 229)));
            checkMark.SetValue(System.Windows.Shapes.Path.StrokeThicknessProperty, 2.0);
            checkMark.SetValue(System.Windows.Shapes.Path.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            checkMark.SetValue(System.Windows.Shapes.Path.VerticalAlignmentProperty, VerticalAlignment.Center);
            checkMark.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);
            cbBorder.AppendChild(checkMark);

            cbGrid.AppendChild(cbBorder);

            var cbContent = new FrameworkElementFactory(typeof(ContentPresenter));
            cbContent.SetValue(Grid.ColumnProperty, 1);
            cbContent.SetValue(FrameworkElement.MarginProperty, new Thickness(6, 0, 0, 0));
            cbContent.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
            cbGrid.AppendChild(cbContent);

            checkBoxTemplate.VisualTree = cbGrid;

            Trigger cbCheckedTrigger = new Trigger { Property = CheckBox.IsCheckedProperty, Value = true };
            cbCheckedTrigger.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Visible, "CheckMark"));
            cbCheckedTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(30, 136, 229)), "BoxBorder"));
            checkBoxTemplate.Triggers.Add(cbCheckedTrigger);

            customCheckBoxStyle.Setters.Add(new Setter(CheckBox.TemplateProperty, checkBoxTemplate));
            customCheckBoxStyle.Setters.Add(new Setter(CheckBox.CursorProperty, Cursors.Hand));

            // =========================================================================

            Grid mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // --- 1. Панель управления ---
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

            controlPanel.Children.Add(new TextBlock
            {
                Text = "Алгоритм:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0),
                FontWeight = FontWeights.Normal
            });

            _algorithmComboBox = new ComboBox
            {
                Width = 200,
                Height = 26,
                Style = customComboBoxStyle,
                ItemContainerStyle = comboBoxItemStyle,
                Margin = new Thickness(0, 0, 16, 0),
                Cursor = Cursors.Hand
            };
            foreach (var key in _algorithms.Keys)
            {
                _algorithmComboBox.Items.Add(key);
            }
            if (_algorithmComboBox.Items.Count > 0) _algorithmComboBox.SelectedIndex = 0;
            _algorithmComboBox.SelectionChanged += AlgorithmComboBox_SelectionChanged;

            controlPanel.Children.Add(_algorithmComboBox);

            // --- Блок рекурсии (N) ---
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
                Style = customSliderStyle,
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
                Style = customCheckBoxStyle,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 16, 0),
                Visibility = Visibility.Collapsed,
                FontWeight = FontWeights.Normal
            };
            controlPanel.Children.Add(_useStackCheckBox);

            // --- Блок регулятора скорости ---
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
                Style = customSliderStyle,
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

            // --- 2. Статусная строка ---
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

            // --- 3. Холст ---
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

            // --- 4. Интеграция с TabControl ---
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