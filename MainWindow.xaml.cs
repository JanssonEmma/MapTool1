using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace MapTool1
{
    public partial class MainWindow : Window
    {
        private int rows = 0; // Initial number of rows
        private int columns = 0; // Initial number of columns
        private double cellSize;
        private double xOffset;
        private double yOffset;
        private Dictionary<Tuple<int, int>, string> mappedGrid = new Dictionary<Tuple<int, int>, string>();
        public MainWindow()
        {
            InitializeComponent();
            DrawGrid(rows, columns);
        }

        private void DrawGrid(int rows, int columns)
        {
            if (rows > 0 && columns > 0)
            {
                canvas.Children.Clear();

                double canvasWidth = canvas.ActualWidth;
                double canvasHeight = canvas.ActualHeight;

                double gridWidth = canvasWidth * 0.75;
                double gridHeight = canvasHeight * 0.75;

                cellSize = Math.Min(gridWidth / columns, gridHeight / rows);

                xOffset = (canvasWidth - (cellSize * columns)) / 2;
                yOffset = (canvasHeight - (cellSize * rows)) / 2;

                // Draw grid cells
                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < columns; col++)
                    {
                        var cell = new Rectangle
                        {
                            Width = cellSize,
                            Height = cellSize,
                            Fill = Brushes.White,
                            Stroke = Brushes.Black,
                            StrokeThickness = 1
                        };
                        
                        Canvas.SetLeft(cell, xOffset + col * cellSize);
                        Canvas.SetTop(cell, yOffset + row * cellSize);
                        TextBlock textBlock = new TextBlock();
                        Tuple<int, int> key = new Tuple<int, int>(row, col);
                        if (mappedGrid.TryGetValue(key, out string value))
                        {
                            textBlock.Text = value;
                        }
                        else
                        {
                            textBlock.Text = "#error";
                        }
                        textBlock.TextAlignment = TextAlignment.Center;
                        Canvas.SetLeft(textBlock, xOffset + col * cellSize + cell.Width / 2);
                        Canvas.SetTop(textBlock, yOffset + row * cellSize + cell.Height / 2);

                        canvas.Children.Add(cell);
                        canvas.Children.Add(textBlock);
                    }
                }

                // Draw additional row buttons
                var buttonBeforeFirstRow = new Button()
                {
                    Width = cellSize * columns,
                    Height = cellSize / 2,
                    Content = "+",
                    FontSize = 16,
                    Background = Brushes.DarkGray,
                    BorderThickness = new Thickness(0),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                Canvas.SetLeft(buttonBeforeFirstRow, xOffset);
                Canvas.SetTop(buttonBeforeFirstRow, yOffset - cellSize / 2);
                canvas.Children.Add(buttonBeforeFirstRow);

                var buttonAfterLastRow = new Button()
                {
                    Width = cellSize * columns,
                    Height = cellSize / 2,
                    Content = "+",
                    FontSize = 16,
                    Background = Brushes.DarkGray,
                    BorderThickness = new Thickness(0),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                Canvas.SetLeft(buttonAfterLastRow, xOffset);
                Canvas.SetTop(buttonAfterLastRow, yOffset + (rows * cellSize));
                canvas.Children.Add(buttonAfterLastRow);

                // Draw additional column buttons
                var buttonFirstColumn = new Button()
                {
                    Width = cellSize / 2,
                    Height = cellSize * rows,
                    Content = "+",
                    FontSize = 16,
                    Background = Brushes.DarkGray,
                    BorderThickness = new Thickness(0),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                Canvas.SetLeft(buttonFirstColumn, xOffset - cellSize / 2);
                Canvas.SetTop(buttonFirstColumn, yOffset);
                canvas.Children.Add(buttonFirstColumn);

                var buttonLastColumn = new Button()
                {
                    Width = cellSize / 2,
                    Height = cellSize * rows,
                    Content = "+",
                    FontSize = 16,
                    Background = Brushes.DarkGray,
                    BorderThickness = new Thickness(0),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                };
                buttonLastColumn.Click += LastColumnButton_Click;
                Canvas.SetLeft(buttonLastColumn, xOffset + (columns * cellSize));
                Canvas.SetTop(buttonLastColumn, yOffset);
                canvas.Children.Add(buttonLastColumn);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DrawGrid(rows, columns); // Example: 4 rows and 3 columns
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawGrid(rows, columns); // Redraw the grid whenever the window size changes
        }

        private void FirstRowButton_Click(object sender, RoutedEventArgs e)
        {
            // Handle first row button click event
        }

        private void LastRowButton_Click(object sender, RoutedEventArgs e)
        {
            // Handle last row button click event
        }

        private void FirstColumnButton_Click(object sender, RoutedEventArgs e)
        {
            // Handle first column button click event
        }

        private void LastColumnButton_Click(object sender, RoutedEventArgs e)
        {
            columns++;
            DrawGrid(rows, columns);
        }

        private void MenuItemOpen_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog();
            dlg.Title = "My Title";
            dlg.IsFolderPicker = true;
            dlg.InitialDirectory = "C:\\Users";

            dlg.AddToMostRecentlyUsedList = false;
            dlg.AllowNonFileSystemItems = false;
            dlg.DefaultDirectory = "C:\\Users";
            dlg.EnsureFileExists = true;
            dlg.EnsurePathExists = true;
            dlg.EnsureReadOnly = false;
            dlg.EnsureValidNames = true;
            dlg.Multiselect = false;
            dlg.ShowPlacesList = true;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                mappedGrid.Clear();
                var folder = dlg.FileName;
                var directories = Directory.GetDirectories(folder);
                List<string> cleanedNamesList = new List<string>();
                foreach (var directory in directories)
                {
                    cleanedNamesList.Add(directory.Substring(directory.LastIndexOf('\\') + 1));
                }
                string[] searchObjects = cleanedNamesList.ToArray();
                int maxColumnIndex = 0;
                int maxRowIndex = 0;
                foreach (string element in searchObjects)
                {
                    string pattern = @"^(\d{3})(\d{3})$";
                    Match match = Regex.Match(element, pattern);

                    if (match.Success)
                    {
                        int columnIndex = int.Parse(match.Groups[1].Value);
                        int rowIndex = int.Parse(match.Groups[2].Value);

                        maxColumnIndex = Math.Max(maxColumnIndex, columnIndex);
                        maxRowIndex = Math.Max(maxRowIndex, rowIndex);

                        mappedGrid.Add(Tuple.Create(rowIndex, columnIndex), element);
                    }
                }
                columns = maxColumnIndex + 1;
                rows = maxRowIndex + 1;
                DrawGrid(rows, columns);
            }
        }
    }
}
