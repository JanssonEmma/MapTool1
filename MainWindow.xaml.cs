using System;
using System.Collections.Generic;
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
        private int rows = 0;
        private int columns = 0;
        private double cellSize;
        private double xOffset;
        private double yOffset;
        private Dictionary<Tuple<int, int>, Tuple<string, string, bool>> mappedGrid = new Dictionary<Tuple<int, int>, Tuple<string, string, bool>>();
        private bool resized;
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
                        Rectangle cell = new Rectangle
                        {
                            Width = cellSize,
                            Height = cellSize,
                            Fill = Brushes.White,
                            Stroke = Brushes.Black,
                            StrokeThickness = 1
                        };

                        Canvas.SetLeft(cell, xOffset + (col * cellSize));
                        Canvas.SetTop(cell, yOffset + (row * cellSize));

                        Tuple<int, int> key = new Tuple<int, int>(row, col);
                        if (mappedGrid.TryGetValue(key, out Tuple<string, string, bool> value))
                        {
                            if (value.Item3)
                            {
                                cell.Uid = resized ? string.Format("{0} ({1})", value.Item2, value.Item1) : value.Item2;
                                cell.Fill = Brushes.LightGray;
                            }
                            else
                            {
                                cell.Uid = value.Item2;
                                cell.Fill = Brushes.White;
                            }
                            cell.MouseEnter += Cell_MouseEnter;
                        }
                        _ = canvas.Children.Add(cell);
                    }
                }

                // Draw additional row buttons
                Button buttonBeforeFirstRow = new Button()
                {
                    Width = cellSize * columns - 20,
                    Height = 20,
                    Content = "+",
                    FontSize = 16,
                    Background = Brushes.CornflowerBlue,
                    BorderThickness = new Thickness(0),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                buttonBeforeFirstRow.Click += FirstRowButton_Click;
                Canvas.SetLeft(buttonBeforeFirstRow, xOffset + 20);
                Canvas.SetTop(buttonBeforeFirstRow, yOffset - 20);
                _ = canvas.Children.Add(buttonBeforeFirstRow);

                Button buttonAfterLastRow = new Button()
                {
                    Width = cellSize * columns - 20,
                    Height = 20,
                    Content = "+",
                    FontSize = 16,
                    Background = Brushes.CornflowerBlue,
                    BorderThickness = new Thickness(0),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                buttonAfterLastRow.Click += LastRowButton_Click;
                Canvas.SetLeft(buttonAfterLastRow, xOffset + 20);
                Canvas.SetTop(buttonAfterLastRow, yOffset + (rows * cellSize));
                _ = canvas.Children.Add(buttonAfterLastRow);

                // Draw additional column buttons
                Button buttonFirstColumn = new Button()
                {
                    Width = 20,
                    Height = cellSize * rows,
                    Content = "+",
                    FontSize = 16,
                    Background = Brushes.CornflowerBlue,
                    BorderThickness = new Thickness(0),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                buttonFirstColumn.Click += FirstColumnButton_Click;
                Canvas.SetLeft(buttonFirstColumn, xOffset);
                Canvas.SetTop(buttonFirstColumn, yOffset);
                _ = canvas.Children.Add(buttonFirstColumn);

                Button buttonLastColumn = new Button()
                {
                    Width = 20,
                    Height = cellSize * rows,
                    Content = "+",
                    FontSize = 16,
                    Background = Brushes.CornflowerBlue,
                    BorderThickness = new Thickness(0),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                };
                buttonLastColumn.Click += LastColumnButton_Click;
                Canvas.SetLeft(buttonLastColumn, xOffset + (columns * cellSize));
                Canvas.SetTop(buttonLastColumn, yOffset);
                _ = canvas.Children.Add(buttonLastColumn);
            }
        }

        private void Cell_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Rectangle s = (Rectangle)sender;
            ToolTip cellToolTip = new ToolTip
            {
                Content = s.Uid,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Center
            };
            s.ToolTip = cellToolTip;
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
            rows++;
            MoveGridRows();
            DrawGrid(rows, columns);
        }

        private void LastRowButton_Click(object sender, RoutedEventArgs e)
        {
            rows++;
            AttachAddedRows(false);
            DrawGrid(rows, columns);
        }

        private void FirstColumnButton_Click(object sender, RoutedEventArgs e)
        {
            columns++;
            MoveGridColumns();
            DrawGrid(rows, columns);
        }

        private void LastColumnButton_Click(object sender, RoutedEventArgs e)
        {
            columns++;
            AttachedAddedColumn(false);
            DrawGrid(rows, columns);
        }

        private void MenuItemOpen_Click(object sender, RoutedEventArgs e)
        {
            resized = false;
            CommonOpenFileDialog dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                InitialDirectory = "C:\\Users",
                AddToMostRecentlyUsedList = false,
                AllowNonFileSystemItems = false,
                DefaultDirectory = "C:\\Users",
                EnsureFileExists = true,
                EnsurePathExists = true,
                EnsureReadOnly = false,
                EnsureValidNames = true,
                Multiselect = false,
                ShowPlacesList = true
            };

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                mappedGrid.Clear();
                string folder = dlg.FileName;
                string[] directories = Directory.GetDirectories(folder);
                List<string> cleanedNamesList = new List<string>();
                foreach (string directory in directories)
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

                        mappedGrid.Add(Tuple.Create(rowIndex, columnIndex), Tuple.Create(element, element, true));
                    }
                }
                columns = maxColumnIndex + 1;
                rows = maxRowIndex + 1;
                DrawGrid(rows, columns);
            }
        }

        private void MenuItemSave_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MoveGridColumns()
        {
            if (!resized)
            {
                resized = true;
            }
            Dictionary<Tuple<int, int>, Tuple<string,string,bool>> newMappedGrid = new Dictionary<Tuple<int, int>, Tuple<string, string,bool>>();
            foreach (Tuple<int, int> key in mappedGrid.Keys)
            {
                _ = mappedGrid.TryGetValue(key, out Tuple<string, string, bool> value);
                newMappedGrid.Add(Tuple.Create(key.Item1, key.Item2 + 1), Tuple.Create(value.Item1, GetFolderName(key.Item1, key.Item2 + 1), value.Item3));
            }
            mappedGrid.Clear();
            mappedGrid = newMappedGrid;
            //add new columns to mapped grid
            AttachAddedRows(true);
        }

        private void MoveGridRows()
        {
            if (!resized)
            {
                resized = true;
            }
            Dictionary<Tuple<int, int>, Tuple<string, string, bool>> newMappedGrid = new Dictionary<Tuple<int, int>, Tuple<string, string, bool>>();
            foreach (Tuple<int, int> key in mappedGrid.Keys)
            {
                _ = mappedGrid.TryGetValue(key, out Tuple<string, string, bool> value);
                newMappedGrid.Add(Tuple.Create(key.Item1 + 1, key.Item2), Tuple.Create(value.Item1, GetFolderName(key.Item1 + 1, key.Item2), value.Item3));
            }
            mappedGrid.Clear();
            mappedGrid = newMappedGrid;
            //add new rows to mapped grid
            AttachedAddedColumn(true);
        }

        private string GetFolderName(int rowIndex, int columnIndex)
        {
            string columnStr = columnIndex.ToString("D3");
            string rowStr = rowIndex.ToString("D3");
            return columnStr + rowStr;
        }

        private void AttachAddedRows(bool infront)
        {
            if (infront)
            {
                for (int row = rows - 1; row >= 0; row--)
                {
                    mappedGrid.Add(Tuple.Create(row, 0), Tuple.Create(GetFolderName(row, 0), GetFolderName(row, 0), false));
                }
            }
            else
            {
                for (int column = columns - 1; column >= 0; column--)
                {
                    mappedGrid.Add(Tuple.Create(rows - 1, column), Tuple.Create(GetFolderName(rows - 1, column), GetFolderName(rows - 1, column), false));
                }
            }
            
        }

        private void AttachedAddedColumn(bool infront)
        {
            if (infront)
            {
                for (int column = columns - 1; column >= 0; column--)
                {
                    mappedGrid.Add(Tuple.Create(0, column), Tuple.Create(GetFolderName(0, column), GetFolderName(0, column), false));
                }
            }
            else
            {
                for (int row = rows - 1; row >= 0; row--)
                {
                    mappedGrid.Add(Tuple.Create(row, columns - 1), Tuple.Create(GetFolderName(row, columns - 1), GetFolderName(row, columns - 1), false));
                }
                
            }
        }
    }
}
