using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
        private string mapDirectory;
        private string settingsFilePath;
        private string[] settingsFileContent;
        private bool changesSaved = false;
        private bool mapDimensionsChanged = false;

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
            mapDimensionsChanged = true;
        }

        private void LastRowButton_Click(object sender, RoutedEventArgs e)
        {
            rows++;
            AttachAddedRows(false);
            DrawGrid(rows, columns);
            mapDimensionsChanged = true;
        }

        private void FirstColumnButton_Click(object sender, RoutedEventArgs e)
        {
            columns++;
            MoveGridColumns();
            DrawGrid(rows, columns);
            mapDimensionsChanged = true;
        }

        private void LastColumnButton_Click(object sender, RoutedEventArgs e)
        {
            columns++;
            AttachedAddedColumn(false);
            DrawGrid(rows, columns);
            mapDimensionsChanged = true;
        }

        private void MenuItemOpen_Click(object sender, RoutedEventArgs e)
        {
            if (!ChangesSaved())
            {
                mapDimensionsChanged = false;
                changesSaved = false;
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
                    mapDirectory = dlg.FileName;
                    Title = string.Format("{0} - {1}", Title, mapDirectory.Substring(mapDirectory.LastIndexOf('\\') + 1));
                    //Get and remember settings file
                    settingsFilePath = System.IO.Path.Combine(mapDirectory, "setting.txt");
                    settingsFileContent = File.ReadAllLines(settingsFilePath);
                    string[] directories = Directory.GetDirectories(mapDirectory);
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
        }

        private void MainWindow_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void MenuItemSave_Click(object sender, RoutedEventArgs e)
        {
            UpdateSettings();
            UpdateDirectories();
            changesSaved = true;
        }

        private void MoveGridColumns()
        {
            if (!resized)
            {
                resized = true;
            }
            Dictionary<Tuple<int, int>, Tuple<string, string, bool>> newMappedGrid = new Dictionary<Tuple<int, int>, Tuple<string, string, bool>>();
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

        private Tuple<int, int> GetXYFromFoldername(string dir)
        {
            int x = int.Parse(dir.Substring(0, 3));
            int y = int.Parse(dir.Substring(3));
            return Tuple.Create(x,y);
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

        private void UpdateSettings()
        {
            for (int i = 0; i < settingsFileContent.Length; i++)
            {
                string[] parts = settingsFileContent[i].Split(new char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length > 0 && parts[0] == "MapSize")
                {
                    if (parts.Length >= 3 && int.TryParse(parts[1], out _) && int.TryParse(parts[2], out _))
                    {
                        parts[1] = columns.ToString();
                        parts[2] = rows.ToString();

                        settingsFileContent[i] = string.Join("\t", parts);
                    }
                    else
                    {
                        _ = MessageBox.Show("Error parsing MapSize values.");
                    }
                    break;
                }
            }
            File.WriteAllLines(settingsFilePath, settingsFileContent);
        }

        private void UpdateDirectories()
        {
            for (int row = rows - 1; row >= 0; row--)
            {
                for (int column = columns - 1; column >= 0; column--)
                {
                    Tuple<int, int> key = new Tuple<int, int>(row, column);
                    if (mappedGrid.TryGetValue(key, out Tuple<string, string, bool> value))
                    {
                        if (value.Item3)
                        {
                            if (value.Item1 != value.Item2)
                            {
                                UpdateBaseGridDirectories(value.Item1, value.Item2);
                                UpdateAreaData(value.Item2);
                                UpdateAreaAmbienceData(value.Item2);
                            }
                        }
                        else
                        {
                            CreateNewGridDirectoriesWithTemplate(value.Item2);
                        }
                    }
                }
            }
            MessageBox.Show("Map saved.");
        }
        private void UpdateBaseGridDirectories(string originalName, string newName)
        {
            Directory.Move(System.IO.Path.Combine(mapDirectory, originalName), System.IO.Path.Combine(mapDirectory, newName));
        }

        private void CreateNewGridDirectoriesWithTemplate(string name)
        {
            string[] resourceFiles = {
                "AreaAmbienceData.txt",
                "AreaData.txt",
                "AreaProperty.txt",
                "attr.atr",
                "height.raw",
                "minimap.dds",
                "shadowmap.dds",
                "shadowmap.raw",
                "tile.raw",
                "water.wtr"
            };
            DirectoryInfo directoryInfo = Directory.CreateDirectory(System.IO.Path.Combine(mapDirectory, name));
            foreach (string file in resourceFiles)
            {
                FileStream fs = File.Create(System.IO.Path.Combine(directoryInfo.FullName, file));
                fs.Close();
                switch (file)
                {
                    case "AreaAmbienceData.txt":
                        File.WriteAllText(fs.Name, Properties.Resources.AreaAmbienceData);
                        break;
                    case "AreaData.txt":
                        File.WriteAllText(fs.Name, Properties.Resources.AreaData);
                        break;
                    case "AreaProperty.txt":
                        File.WriteAllText(fs.Name, Properties.Resources.AreaProperty);
                        break;
                    case "attr.atr":
                        File.WriteAllBytes(fs.Name, Properties.Resources.attr);
                        break;
                    case "height.raw":
                        File.WriteAllBytes(fs.Name, Properties.Resources.height);
                        break;
                    case "minimap.dds":
                        File.WriteAllBytes(fs.Name, Properties.Resources.minimap);
                        break;
                    case "shadowmap.dds":
                        File.WriteAllBytes(fs.Name, Properties.Resources.shadowmap);
                        break;
                    case "shadowmap.raw":
                        File.WriteAllBytes(fs.Name, Properties.Resources.shadowmap1);
                        break;
                    case "tile.raw":
                        File.WriteAllBytes(fs.Name, Properties.Resources.tile);
                        break;
                    case "water.wtr":
                        File.WriteAllBytes(fs.Name, Properties.Resources.water);
                        break;
                    default:
                        break;
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = ChangesSaved();
        }

        private bool ChangesSaved()
        {
            if (mapDimensionsChanged && !changesSaved)
            {
                MessageBoxResult res = MessageBox.Show(string.Format("You have unsaved changes to the map:\n\n{0}\n\nDo you want to continue?", mapDirectory.Substring(mapDirectory.LastIndexOf('\\') + 1)), "Configuration", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                return res != MessageBoxResult.Yes;
            }
            return false;
        }

        private void UpdateAreaData(string dir)
        {
            string areadataPath = System.IO.Path.Combine(mapDirectory, dir, "areadata.txt");
            string tempAreadataPath = System.IO.Path.Combine(mapDirectory, dir, "tempareadata.txt");

            if (File.Exists(areadataPath))
            {
                try
                {
                    List<Tuple<double, double, double>> objectCoordinates = new List<Tuple<double, double, double>>();
                    List<Tuple<double, double, double>> newObjectCoordinates = new List<Tuple<double, double, double>>();
                    using (StreamReader reader = new StreamReader(areadataPath))
                    {
                        string line;
                        string objectName = null;
                        double x = 0, y = 0, z = 0, xNew = 0, yNew = 0, zNew = 0;
                        bool objectHandled = false;

                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.StartsWith("Start"))
                            {
                                objectName = line.Substring(6); //"Start" + whitespace char
                            }
                            else if (line.StartsWith("End Object") && objectName != null)
                            {
                                //End of object block, add the coordinates to the list
                                objectCoordinates.Add(new Tuple<double, double, double>(x, y, z));
                                newObjectCoordinates.Add(new Tuple<double, double, double>(xNew, yNew, zNew));
                                objectName = null; //Reset objectName
                                objectHandled = false; //Reset objectHandled
                            }
                            else if (objectName != null && !objectHandled)
                            {
                                // Parse x, y, z coordinates from the line
                                line = line.Trim();
                                string[] parts = line.Split(' ');
                                if (parts.Length == 3 &&
                                    double.TryParse(parts[0], NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out x) &&
                                    double.TryParse(parts[1], NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out y) &&
                                    double.TryParse(parts[2], NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out z))
                                {
                                    objectHandled = true;
                                    //recalc x,y,z for updated grid
                                    Tuple<int, int> chunkPos = GetXYFromFoldername(dir);
                                    if (chunkPos.Item1 == 0)
                                        xNew = x;
                                    else
                                        xNew = x + (chunkPos.Item1 * 25600);

                                    if (chunkPos.Item2 == 0)
                                        yNew = y;
                                    else
                                        yNew = y + (chunkPos.Item2 * 25600);
                                    zNew = z;
                                }
                                else
                                {
                                    _ = MessageBox.Show("Error parsing the coordinates. Try again or report as bug.");
                                }
                            }
                        }
                        reader.Close();
                    }
                    //Update coordinates of objects
                    if (objectCoordinates.Count == newObjectCoordinates.Count)
                    {
                        //Create a temporary copy of the file
                        File.Copy(areadataPath, tempAreadataPath, true);

                        using(StreamWriter writer = new StreamWriter(areadataPath, false, System.Text.Encoding.UTF8))
                        {
                            using (StreamReader reader = new StreamReader(tempAreadataPath))
                            {
                                string line;
                                int objectIndex = 0;
                                string xValue, yValue, zValue = "";
                                bool coordinatesInNextLine = false;

                                while ((line = reader.ReadLine()) != null)
                                {
                                    if (line.StartsWith("Start Object"))
                                    {
                                        // Write the updated coordinates from 'updatedCoordinates'
                                        writer.WriteLine(line); // Start Object line
                                        coordinatesInNextLine = true; 
                                    }
                                    else
                                    {
                                        if (coordinatesInNextLine)
                                        {
                                            xValue = newObjectCoordinates[objectIndex].Item1.ToString("0.######",CultureInfo.GetCultureInfo("en-US"));
                                            yValue = newObjectCoordinates[objectIndex].Item2.ToString("0.######", CultureInfo.GetCultureInfo("en-US"));
                                            zValue = newObjectCoordinates[objectIndex].Item3.ToString("0.######", CultureInfo.GetCultureInfo("en-US"));
                                            writer.WriteLine($"    {xValue} {yValue} {zValue}");
                                            coordinatesInNextLine = false;
                                            objectIndex++;
                                        }
                                        else
                                            writer.WriteLine(line);
                                    }
                                }
                                reader.Close();
                            }
                            writer.Close();
                            File.Delete(tempAreadataPath);
                        }
                    }
                }
                catch (IOException e)
                {
                    _ = MessageBox.Show($"An error occurred while reading the file: {e.Message}");
                }
            }
            else
            {
                _ = MessageBox.Show("The file 'areadata.txt' does not exist in the directory.");
            }
        }

        private void UpdateAreaAmbienceData(string dir)
        {
            string areaambiencedataPath = System.IO.Path.Combine(mapDirectory, dir, "areaambiencedata.txt");
            string tempAreaambiencedataPath = System.IO.Path.Combine(mapDirectory, dir, "tempareaambiencedata.txt");

            if (File.Exists(areaambiencedataPath))
            {
                try
                {
                    List<Tuple<double, double, double>> objectCoordinates = new List<Tuple<double, double, double>>();
                    List<Tuple<double, double, double>> newObjectCoordinates = new List<Tuple<double, double, double>>();
                    using (StreamReader reader = new StreamReader(areaambiencedataPath))
                    {
                        string line;
                        string objectName = null;
                        double x = 0, y = 0, z = 0, xNew = 0, yNew = 0, zNew = 0;
                        bool objectHandled = false;

                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.StartsWith("Start"))
                            {
                                objectName = line.Substring(6); //"Start" + whitespace char
                            }
                            else if (line.StartsWith("End Object") && objectName != null)
                            {
                                //End of object block, add the coordinates to the list
                                objectCoordinates.Add(new Tuple<double, double, double>(x, y, z));
                                newObjectCoordinates.Add(new Tuple<double, double, double>(xNew, yNew, zNew));
                                objectName = null; //Reset objectName
                                objectHandled = false; //Reset objectHandled
                            }
                            else if (objectName != null && !objectHandled)
                            {
                                // Parse x, y, z coordinates from the line
                                line = line.Trim();
                                string[] parts = line.Split(' ');
                                if (parts.Length == 3 &&
                                    double.TryParse(parts[0], NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out x) &&
                                    double.TryParse(parts[1], NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out y) &&
                                    double.TryParse(parts[2], NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out z))
                                {
                                    objectHandled = true;
                                    //recalc x,y,z for updated grid
                                    Tuple<int, int> chunkPos = GetXYFromFoldername(dir);
                                    if (chunkPos.Item1 == 0)
                                        xNew = x;
                                    else
                                        xNew = x + (chunkPos.Item1 * 25600);

                                    if (chunkPos.Item2 == 0)
                                        yNew = y;
                                    else
                                        yNew = y + (chunkPos.Item2 * 25600);
                                    zNew = z;
                                }
                                else
                                {
                                    _ = MessageBox.Show("Error parsing the coordinates. Try again or report as bug.");
                                }
                            }
                        }
                        reader.Close();
                    }
                    //Update coordinates of objects
                    if (objectCoordinates.Count == newObjectCoordinates.Count)
                    {
                        //Create a temporary copy of the file
                        File.Copy(areaambiencedataPath, tempAreaambiencedataPath, true);

                        using (StreamWriter writer = new StreamWriter(areaambiencedataPath, false, System.Text.Encoding.UTF8))
                        {
                            using (StreamReader reader = new StreamReader(tempAreaambiencedataPath))
                            {
                                string line;
                                int objectIndex = 0;
                                string xValue, yValue, zValue = "";
                                bool coordinatesInNextLine = false;

                                while ((line = reader.ReadLine()) != null)
                                {
                                    if (line.StartsWith("Start Object"))
                                    {
                                        // Write the updated coordinates from 'updatedCoordinates'
                                        writer.WriteLine(line); // Start Object line
                                        coordinatesInNextLine = true;
                                    }
                                    else
                                    {
                                        if (coordinatesInNextLine)
                                        {
                                            xValue = newObjectCoordinates[objectIndex].Item1.ToString("0.######", CultureInfo.GetCultureInfo("en-US"));
                                            yValue = newObjectCoordinates[objectIndex].Item2.ToString("0.######", CultureInfo.GetCultureInfo("en-US"));
                                            zValue = newObjectCoordinates[objectIndex].Item3.ToString("0.######", CultureInfo.GetCultureInfo("en-US"));
                                            writer.WriteLine($"    {xValue} {yValue} {zValue}");
                                            coordinatesInNextLine = false;
                                            objectIndex++;
                                        }
                                        else
                                            writer.WriteLine(line);
                                    }
                                }
                                reader.Close();
                            }
                            writer.Close();
                            File.Delete(tempAreaambiencedataPath);
                        }
                    }
                }
                catch (IOException e)
                {
                    _ = MessageBox.Show($"An error occurred while reading the file: {e.Message}");
                }
            }
            else
            {
                _ = MessageBox.Show("The file 'areaambiencedata.txt' does not exist in the directory.");
            }
        }
    }
}
