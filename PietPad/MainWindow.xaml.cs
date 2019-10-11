using Microsoft.Win32;
using PietPad.Classes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PietPad
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Codel[,] image;
        private Rectangle[,] imageRects;
        private readonly (CodelColor Color, Border Border)[,] brushesGrid = new (CodelColor, Border)[6, 3];
        private readonly (CodelColor Color, Border Border)[] brushesWhiteBlack = new (CodelColor, Border)[2];
        private const int codelSize = 20;
        private CodelColor currentColor = CodelColor.LightRed;
        private const double normalStroke = 1;
        private static readonly Thickness normalThickness = new Thickness(normalStroke);
        private const double selectedStroke = 2;
        private static readonly Thickness selectedThickness = new Thickness(selectedStroke);
        private readonly Interpreter interpreter = new Interpreter(debug: true);
        private ColorBlock currentColorBlock;
        private string currentFilePath;
        public MainWindow()
        {
            InitializeComponent();

            // configure listeners for debug interpreter
            interpreter.Stack.CollectionChanged += (sender, e) =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        stackDebugListView.Dispatcher.Invoke(() => stackDebugListView.Items.Insert(0, e.NewItems[0]));
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        stackDebugListView.Dispatcher.Invoke(() => stackDebugListView.Items.RemoveAt(e.OldStartingIndex));
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        stackDebugListView.Dispatcher.Invoke(() => stackDebugListView.Items.Clear());
                        break;
                }
                stackDebugListView.Dispatcher.Invoke(() => stackDebugListView.InvalidateMeasure());
            };
            interpreter.EnterColorBlock += ColorBlockChanged;

            // initialize image
            image = new Codel[pietGrid.ColumnDefinitions.Count, pietGrid.RowDefinitions.Count];
            imageRects = new Rectangle[pietGrid.ColumnDefinitions.Count, pietGrid.RowDefinitions.Count];

            // fill image into grid
            for (int row = 0; row < pietGrid.RowDefinitions.Count; row++)
            {
                for (int col = 0; col < pietGrid.ColumnDefinitions.Count; col++)
                {
                    var rect = GetNewRectangle(col, row);
                    image[col, row] = new Codel(col, row);
                    imageRects[col, row] = rect;
                    pietGrid.Children.Add(rect);
                }
            }

            // load brush buttons into the grid
            for (int hue = 0; hue < 6; hue++)
            {
                for (int l = 0; l < 3; l++)
                {
                    CodelColor codelColor = CodelColor.GetColor((ColorHue)hue, (ColorLightness)l);
                    var tuple = GetBrushGridEntry(codelColor);
                    brushesGrid[hue, l] = tuple;
                }
            }
            brushesWhiteBlack[0] = GetBrushGridEntry(CodelColor.White);
            brushesWhiteBlack[1] = GetBrushGridEntry(CodelColor.Black);

            // set the right background brushes for the codels
            ReloadColors();
        }

        private void ColorBlockChanged(object sender, ColorBlock e)
        {
            void SetStroke(Rectangle rect, double stroke)
            {
                rect.Dispatcher.Invoke(() => rect.StrokeThickness = stroke);
            }
            if (currentColorBlock != null)
            {
                currentColorBlock.Codels.ForEach(x => SetStroke(imageRects[x.Position.x, x.Position.y], normalStroke));
            }
            currentColorBlock = e;
            currentColorBlock.Codels.ForEach(x => SetStroke(imageRects[x.Position.x, x.Position.y], selectedStroke));
        }

        private (CodelColor color, Border border) GetBrushGridEntry(CodelColor codelColor)
        {
            Border border = new Border
            {
                Background = codelColor.Brush,
                BorderBrush = Brushes.Black,
                BorderThickness = normalThickness
            };
            if (codelColor != CodelColor.White && codelColor != CodelColor.Black)
            {
                border.Child = new Label { Content = CodelColor.CalculateOperation(currentColor, codelColor).GetName() };
                Grid.SetColumn(border, (int)codelColor.Hue);
                Grid.SetRow(border, (int)codelColor.Lightness);
            }
            else
            {
                Grid.SetRow(border, 3);
                Grid.SetColumnSpan(border, 3);
                if (codelColor == CodelColor.White)
                {
                    Grid.SetColumn(border, 0);
                }
                else if (codelColor == CodelColor.Black)
                {
                    Grid.SetColumn(border, 3);
                }
            }
            if (codelColor == currentColor) border.BorderThickness = selectedThickness;
            brushButtonsGrid.Children.Add(border);
            border.MouseDown += (sender, e) =>
            {
                ColorSelected(codelColor);
                e.Handled = true;
                Keyboard.ClearFocus();
                Keyboard.Focus(brushButtonsGrid);
            };

            return (codelColor, border);
        }

        private void ColorSelected(CodelColor color)
        {
            for (int hue = 0; hue < 6; hue++)
            {
                for (int l = 0; l < 3; l++)
                {
                    (CodelColor color, Border border) tuple = brushesGrid[hue, l];
                    if (color == tuple.color) tuple.border.BorderThickness = selectedThickness;
                    else tuple.border.BorderThickness = normalThickness;
                    ((Label)tuple.border.Child).Content = CodelColor.CalculateOperation(color, tuple.color).GetName();
                }
            }

            for (int i = 0; i < 2; i++)
            {
                (CodelColor color, Border border) tuple = brushesWhiteBlack[i];
                if (color == tuple.color) tuple.border.BorderThickness = selectedThickness;
                else tuple.border.BorderThickness = normalThickness;
            }

            currentColor = color;
        }

        private Rectangle GetNewRectangle(int col, int row)
        {
            var rect = new Rectangle { VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch, Stroke = Brushes.Black };
            Grid.SetColumn(rect, col);
            Grid.SetRow(rect, row);
            rect.MouseLeftButtonDown += (sender, e) => { RectangleClicked(col, row); e.Handled = true; };
            rect.MouseEnter += (sender, e) => { if (e.LeftButton == MouseButtonState.Pressed) RectangleClicked(col, row); };
            return rect;
        }

        private void RectangleClicked(int col, int row)
        {
            image[col, row].Color = currentColor;
            imageRects[col, row].Fill = image[col, row].Color.Brush;
            Keyboard.ClearFocus();
            Keyboard.Focus(brushButtonsGrid);
        }

        private void ReloadColors()
        {
            for (int col = 0; col < image.GetLength(0); col++)
            {
                for (int row = 0; row < image.GetLength(1); row++)
                {
                    imageRects[col, row].Fill = image[col, row].Color.Brush;
                }
            }
        }

        private void ReloadGrid()
        {
            while (pietGrid.ColumnDefinitions.Count < image.GetLength(0)) pietGrid.ColumnDefinitions.Add(new ColumnDefinition());
            while (pietGrid.ColumnDefinitions.Count > image.GetLength(0)) pietGrid.ColumnDefinitions.RemoveAt(0);
            while (pietGrid.RowDefinitions.Count < image.GetLength(1)) pietGrid.RowDefinitions.Add(new RowDefinition());
            while (pietGrid.RowDefinitions.Count > image.GetLength(1)) pietGrid.RowDefinitions.RemoveAt(0);
            pietGrid.Children.Clear();
            for (int col = 0; col < image.GetLength(0); col++)
            {
                for (int row = 0; row < image.GetLength(1); row++)
                {
                    pietGrid.Children.Add(imageRects[col, row]);
                }
            }
            ReloadColors();
        }

        public void ResizeGrid(int newCols, int newRows)
        {
            pietGrid.Width = newCols * codelSize;
            pietGrid.Height = newRows * codelSize;
            var oldImage = image;
            var oldImageRects = imageRects;
            image = new Codel[newCols, newRows];
            imageRects = new Rectangle[newCols, newRows];
            for (int col = 0; col < image.GetLength(0); col++)
            {
                for (int row = 0; row < image.GetLength(1); row++)
                {
                    if (col < oldImage.GetLength(0) && row < oldImage.GetLength(1))
                    {
                        image[col, row] = oldImage[col, row];
                        imageRects[col, row] = oldImageRects[col, row];
                    }
                    else image[col, row] = new Codel(col, row);
                    imageRects[col, row] = GetNewRectangle(col, row);
                }
            }
            ReloadGrid();
        }

        private void IntegerUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!IsInitialized) return;   // this event is being called even when the window hasn't fully loaded yet
            if (!colCounter.Value.HasValue || !rowCounter.Value.HasValue) return;
            ResizeGrid(colCounter.Value.Value, rowCounter.Value.Value);
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            for (int col = 0; col < image.GetLength(0); col++)
            {
                for (int row = 0; row < image.GetLength(1); row++)
                {
                    image[col, row].Color = Codel.DefaultColor;
                    ReloadColors();
                }
            }
            ColorBlockChanged(this, new ColorBlock());
        }

        private void InterpretButton_Click(object sender, RoutedEventArgs e)
        {
            outputTextBox.Clear();
            string inputText = inputTextBox.Text;
            interpretationButton.IsEnabled = false;
            pauseButton.IsEnabled = true;
            cancelButton.IsEnabled = true;
            Task.Run(() =>
            {
                MemoryStream outputStream = new MemoryStream();
                MemoryStream inputStream = new MemoryStream();
                using (StreamReader outStreamReader = new StreamReader(outputStream, Interpreter.Encoding))
                {
                    CancellationTokenSource cancelSource = new CancellationTokenSource();
                    Task.Run(async () => await ReadContinuouslyAsync(outStreamReader, cancelSource.Token));
                    using (StreamWriter inStreamWriter = new StreamWriter(inputStream, Interpreter.Encoding))
                    {
                        inStreamWriter.Write(inputText);
                        inStreamWriter.Flush();
                        inputStream.Seek(0, SeekOrigin.Begin);
                        interpreter.Interpret(image, outputStream, inputStream);
                    }
                    cancelSource.Cancel();
                }
                interpretationButton.Dispatcher.Invoke(() => interpretationButton.IsEnabled = true);
                pauseButton.Dispatcher.Invoke(() => pauseButton.IsEnabled = false);
                cancelButton.Dispatcher.Invoke(() => cancelButton.IsEnabled = false);
                ColorBlockChanged(this, new ColorBlock());
            });
        }

        private async Task ReadContinuouslyAsync(StreamReader sr, CancellationToken cancellationToken)
        {
            int currentOffset = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                sr.BaseStream.Seek(currentOffset, SeekOrigin.Begin);
                var text = await sr.ReadToEndAsync().ConfigureAwait(false);
                if (!string.IsNullOrEmpty(text)) outputTextBox.Dispatcher.Invoke(() => outputTextBox.Text += text);
                currentOffset += sr.CurrentEncoding.GetByteCount(text);
            }
        }

        private int? previousColumn;
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Handled) return;
            switch (e.Key)
            {
                case Key.Up:
                    if (currentColor.Lightness == 0 && currentColor.Hue.HasValue)
                    {
                        previousColumn = (int)currentColor.Hue.Value;
                        ColorSelected(brushesWhiteBlack[(int)currentColor.Hue.Value / 3].Color);
                    }
                    else if (currentColor.Lightness.HasValue && currentColor.Hue.HasValue)
                    {
                        ColorSelected(CodelColor.GetColor(currentColor.Hue.Value, (ColorLightness)((int)currentColor.Lightness - 1)));
                    }
                    else if (previousColumn.HasValue)
                    {
                        ColorSelected(brushesGrid[previousColumn.Value, 2].Color);
                    }
                    else
                    {
                        if (currentColor == CodelColor.White) ColorSelected(brushesGrid[0, 2].Color);
                        else if (currentColor == CodelColor.Black) ColorSelected(brushesGrid[3, 2].Color);
                    }
                    e.Handled = true;
                    break;
                case Key.Down:
                    if (currentColor.Lightness == (ColorLightness)2 && currentColor.Hue.HasValue)
                    {
                        previousColumn = (int)currentColor.Hue.Value;
                        ColorSelected(brushesWhiteBlack[(int)currentColor.Hue.Value / 3].Color);
                    }
                    else if (currentColor.Lightness.HasValue && currentColor.Hue.HasValue)
                    {
                        ColorSelected(CodelColor.GetColor(currentColor.Hue.Value, (ColorLightness)((int)currentColor.Lightness + 1)));
                    }
                    else if (previousColumn.HasValue)
                    {
                        ColorSelected(brushesGrid[previousColumn.Value, 0].Color);
                    }
                    else
                    {
                        if (currentColor == CodelColor.White) ColorSelected(brushesGrid[0, 0].Color);
                        else if (currentColor == CodelColor.Black) ColorSelected(brushesGrid[3, 0].Color);
                    }
                    e.Handled = true;
                    break;
                case Key.Right:
                    previousColumn = null;
                    if (currentColor.Hue == (ColorHue)5 && currentColor.Lightness.HasValue)
                    {
                        ColorSelected(CodelColor.GetColor(0, currentColor.Lightness.Value));
                    }
                    else if (currentColor.Hue.HasValue && currentColor.Lightness.HasValue)
                    {
                        ColorSelected(CodelColor.GetColor((ColorHue)((int)currentColor.Hue + 1), currentColor.Lightness.Value));
                    }
                    else
                    {
                        if (currentColor == CodelColor.White) ColorSelected(CodelColor.Black);
                        else if (currentColor == CodelColor.Black) ColorSelected(CodelColor.White);
                    }
                    e.Handled = true;
                    break;
                case Key.Left:
                    previousColumn = null;
                    if (currentColor.Hue == 0 && currentColor.Lightness.HasValue)
                    {
                        ColorSelected(CodelColor.GetColor((ColorHue)5, currentColor.Lightness.Value));
                    }
                    else if (currentColor.Hue.HasValue && currentColor.Lightness.HasValue)
                    {
                        ColorSelected(CodelColor.GetColor((ColorHue)((int)currentColor.Hue - 1), currentColor.Lightness.Value));
                    }
                    else
                    {
                        if (currentColor == CodelColor.White) ColorSelected(CodelColor.Black);
                        else if (currentColor == CodelColor.Black) ColorSelected(CodelColor.White);
                    }
                    e.Handled = true;
                    break;
            }
        }

        private void Counter_KeyDown(object sender, KeyEventArgs e)
        {
            // Dummy to prevent right/left arrows to work while Counters are focused
            e.Handled = true;
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            switch (interpreter.State)
            {
                case Interpreter.ExecutionState.Paused:
                    interpreter.ResumeInterpretation();
                    pauseButton.Content = "Pause";
                    break;
                case Interpreter.ExecutionState.Running:
                    interpreter.PauseInterpretation();
                    pauseButton.Content = "Resume";
                    break;
            }
        }

        private void StepButton_Click(object sender, RoutedEventArgs e)
        {
            if (interpreter.State == Interpreter.ExecutionState.Idle) InterpretButton_Click(sender, e);
            interpreter.PauseInterpretation();
            pauseButton.Content = "Resume";
            interpreter.Step();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            interpreter.CancelInterpretation();
        }

        private void MenuItemContribute_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Olfi01/PietPad");
        }

        private void CommandBindingSave_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentFilePath)) CommandBindingSaveAs_Executed(sender, e);
            else SaveFile();
        }

        private void CommandBindingSaveAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var sfd = new SaveFileDialog()
            {
                Title = "Save file",
                DefaultExt = "*.piet",
                Filter = "PietPad Files (*.piet)|*.piet|All Files (*.*)|*.*",
                ValidateNames = true
            };
            sfd.ShowDialog();
            if (!string.IsNullOrEmpty(sfd.FileName))
            {
                currentFilePath = sfd.FileName;
                SaveFile();
            }
        }

        private void SaveFile()
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var stream = File.OpenWrite(currentFilePath))
            {
                bf.Serialize(stream, image);
            }
        }

        private void CommandBindingOpen_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var ofd = new OpenFileDialog()
            {
                Title = "Open file",
                DefaultExt = "*.piet",
                Filter = "PietPad Files (*.piet)|*.piet|All Files (*.*)|*.*",
                ValidateNames = true,
                CheckFileExists = true,
                Multiselect = false
            };
            ofd.ShowDialog();
            if (!string.IsNullOrEmpty(ofd.FileName))
            {
                LoadFile(ofd.FileName);
            }
        }

        private void LoadFile(string filePath)
        {
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                using (var stream = File.OpenRead(filePath))
                {
                    image = (Codel[,])bf.Deserialize(stream);
                }
            }
            catch
            {
                MessageBox.Show("Failed to open file.");
                return;
            }
            ResizeGrid(image.GetLength(0), image.GetLength(1));
            currentFilePath = filePath;
        }
    }
}
