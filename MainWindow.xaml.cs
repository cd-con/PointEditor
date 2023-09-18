using PointEditor.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PointEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        public static MainWindow? instance;
        public List<Polygon> polygons = new();
        private int actionRevertCounter;

        // TODO Переделать в словарь
        public List<Polygon>[] actionRevertList = new List<Polygon>[10]; // Хранилще наших действий
        public Color PenColor = new();

        bool bypassNoPointsWarning = false;
        bool bypassIncorrectMoveInputWarning = false;
        bool bypassNamingWarning = false;
        bool bypassInvalidNumber = false;

        public MainWindow()
        {
            InitializeComponent();
            instance = this;
            ColorPicker_HEX.Text = "#696969";
        }

        public void UpdateList()
        {
            PolygonList.ItemsSource = polygons.Select(x => x.Name);
        }

        private void ColorPicker_HEX_TextChanged(object sender, TextChangedEventArgs e)
        {

            Match regexMatch = Regex.Match(ColorPicker_HEX.Text, @"([0-9A-Fa-f]{6})");

            if (regexMatch.Success)
            {
                byte[] hexColors = Methods.StringToByteArray(regexMatch.Value);

                PenColor.R = hexColors[0];
                R.Value = Convert.ToInt32(PenColor.R);

                PenColor.G = hexColors[1];
                G.Value = Convert.ToInt32(PenColor.G);

                PenColor.B = hexColors[2];
                B.Value = Convert.ToInt32(PenColor.B);

                PenColor.A = 0xFF;


                Preview.Fill = new SolidColorBrush(PenColor);
                // Костыли
                Palette.currentBrush.Color = PenColor;
            }
        }

        private void Slider_ValueChanged(object sender, object _)
        {
            if (sender is Slider slider)
            {
                switch (slider.Name)
                {
                    case "R":
                        PenColor.R = Convert.ToByte(R.Value); break;
                    case "G":
                        PenColor.G = Convert.ToByte(G.Value); break;
                    case "B":
                        PenColor.B = Convert.ToByte(B.Value); break;
                    default:
                        throw new NotImplementedException();
                }
                ColorPicker_HEX.Text = $"#{Methods.ByteArrayToString(new byte[] { PenColor.R, PenColor.G, PenColor.B })}";
            }
        }


        // Add new polygon
        private void PolygonAdd_Click(object sender, RoutedEventArgs e)
        {
            Polygon newPolygon = new Polygon
            {
                // Временное решение
                Stroke = Palette.currentBrush,
                StrokeThickness = 3,
                Name = $"newPolygon{polygons.Count}"
            };

            // Логика отката
            //actionRevertList = Utility.Methods.Shift(actionRevertList, new List<Polygon>() { newPolygon });
            //AddCounter();
            //UpdateCounterDisplay();

            polygons.Add(newPolygon);

            mainCanvas.Children.Add(newPolygon);

            UpdateList();

            PolygonList.SelectedItem = newPolygon.Name;
        }

        private void DeleteItems(object sender, RoutedEventArgs e)
        {
            foreach (string itemName in PolygonList.SelectedItems)
            {
                Polygon poly = polygons.Where(x => x.Name == itemName).Single();
                mainCanvas.Children.Remove(poly);
                polygons.Remove(poly);
            }
            UpdateList();
        }

        private void mainCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed && PolygonList.SelectedItem != null)
                polygons.Where(x => x.Name == PolygonList.SelectedItem.ToString()).Single().Points.Add(e.GetPosition(this));
        }

        private void GenerateCode(object sender, RoutedEventArgs e)
        {
            string result = "// Скопируй эти значения в свой код\n";

            if (polygons.Count == 0)
            {
                MessageBox.Show("Для генерации кода на сцене необходим минимум один полигон", "Отмена");
                return;
            }

            foreach (Polygon poly in polygons)
            {
                if (poly.Points.Count == 0)
                {
                    result += $"// {poly.Name} был пропущен - в фигуре нет точек\n\n";
                    continue;
                }

                result += $"// Фигура {poly.Name}\nPolygon {poly.Name} = new();\n{poly.Name}.Stroke = new SolidColorBrush() " + "{ Color = Color.FromRgb(" + Palette.currentBrush.Color.R + ", " + Palette.currentBrush.Color.G + ", " + Palette.currentBrush.Color.B + ")};\n\n";
                foreach (Point point in poly.Points)
                {
                    result += $"{poly.Name}.Points.Add(new Point({Math.Round(point.X, 2).ToString().Replace(',', '.')},{Math.Round(point.Y, 2).ToString().Replace(',', '.')}));\n";
                }

                result += "\n\n";
            }
            Clipboard.SetText(result);
            MessageBox.Show("Код успешно скопирован в буфер обмена", "Код скопирован");
        }

        private void Scale_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxDialog dialog = new("Изменить размер", "Фактор изменения размера");
            if (dialog.ShowDialog() == true && double.TryParse(dialog.ResponseText.Replace('.', ','), out double rescale_factor) && rescale_factor != 0)
            {
                foreach (string itemName in PolygonList.SelectedItems)
                    polygons.Where(x => x.Name == itemName).Single().Points.Rescale(rescale_factor);
            }
        }

        private void PolygonList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Rename();
        }

        private void Rename()
        {
            if (PolygonList.SelectedItems.Count > 0)
            {
                Polygon selected = polygons.Where(x => x.Name == PolygonList.SelectedItem.ToString()).Single();
                MessageBoxDialog dialog = new MessageBoxDialog("Переименовать", $"{selected.Name} будет переименован в");
                if (dialog.ShowDialog() == true && dialog.ResponseText.Count() > 0)
                {
                    string result = dialog.ResponseText.Replace(' ', '_');

                    if (result.Length > 16 && !bypassNamingWarning)
                    {
                        Utility.Dialogs.ExceptionDialog exDialog = new("Предупреждение", $"Рекомендуем укоротить название до 16 символов и меньше.");
                        if (exDialog.ShowDialog() == true && exDialog.isCancelled)
                        {
                            Rename();
                            return;
                        }
                        bypassNamingWarning = exDialog.BypassDialog;
                    }

                    Polygon[] occurences = polygons.Where(x => x.Name == dialog.ResponseText).ToArray();

                    if (occurences.Length > 0)
                        result += occurences.Length;

                    // Сохраняем действе для отката
                    // actionRevertList = Methods.Shift(actionRevertList, new List<Polygon>() { selected });

                    PolygonList.SelectedItem = result;
                    selected.Name = result;
                    UpdateList();
                }
            }
        }

        private void Smooth_Click(object sender, RoutedEventArgs e)
        {
            // Oh boy, here we go
            MessageBoxDialog dialog = new("Смягчить", "Введите значение");
            if (dialog.ShowDialog() == true && dialog.ResponseText.Length > 0)
            {
                if (int.TryParse(dialog.ResponseText, out int smooth_factor) && smooth_factor > 0)
                {
                    if (PolygonList.SelectedItem != null)
                    {
                        // Храним все объекты
                        List<Polygon> revertItems = new();

                        foreach (string itemName in PolygonList.SelectedItems)
                        {
                            Polygon poly = polygons.Where(x => x.Name == itemName).Single();

                            // Проверяем, есть ли в фигуре точки
                            if (poly.Points.Count > 0)
                            {
                                revertItems.Add(poly); // Добавляем в список отката

                                // Проверяем, совпадает ли последняя точка с начальной
                                if (poly.Points.Last() != poly.Points[0])
                                    // Добавляем ещё одну точку с координатами начала, чтобы не было резкой прямой линии
                                    poly.Points.Add(poly.Points[0]);
                                // Сглаживаем
                                poly.Points = Methods.SmootherPolygonCubic(poly.Points, smooth_factor);
                            }
                            else
                            {
                                if (!bypassNoPointsWarning)
                                {
                                    Utility.Dialogs.ExceptionDialog exDialog = new(message: $"Попытка сглаживания фигуры {itemName} не удалась. Количество точек в фигуре должно быть больше 0");
                                    if (exDialog.ShowDialog() == true && exDialog.isCancelled)
                                    {
                                        // Логика отката действий
                                    }
                                    bypassNoPointsWarning = exDialog.BypassDialog;
                                }
                            }
                        }
                        // Сохраняем действе для отката
                    }
                }
                else
                {
                    if (!bypassInvalidNumber)
                    {
                        Utility.Dialogs.ExceptionDialog exDialog = new(message: $"Значение `{dialog.ResponseText}` не является валидным для этой операции.");
                        exDialog.ShowDialog();
                        bypassInvalidNumber = exDialog.BypassDialog;
                    }
                }
            }
        }

        private void Move_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxDialog dialog = new MessageBoxDialog("Сместить", "Пример ввода - горизонталь;вертикаль");
            if (dialog.ShowDialog() == true)
            {
                string[] coords = dialog.ResponseText.Split(';');
                if (coords.Length == 2 && int.TryParse(coords[0], out int x) && int.TryParse(coords[1], out int y))
                {
                    foreach (string itemName in PolygonList.SelectedItems)
                    {
                        Polygon poly = polygons.Where(x => x.Name == itemName).Single();
                        poly.Points.Move(new Point(x, y));
                    }
                }
                else if (dialog.ResponseText.Length > 0 && !bypassIncorrectMoveInputWarning)
                {
                    Utility.Dialogs.ExceptionDialog exDialog = new("Ошибка смещения", $"Неверный ввод.\nИспользуйте маску:\n(-)горизонталь;(-)вертикаль");
                    exDialog.ShowDialog();
                    bypassIncorrectMoveInputWarning = exDialog.BypassDialog;
                }
            }
        }

        private void revertAction_Click(object sender, RoutedEventArgs e)
        {
            DoRevert();
        }

        private void AddCounter() => actionRevertCounter += actionRevertCounter <= 10 ? 1 : 0;

        private void UpdateCounterDisplay()
        {
            revertAction.Content = $"Откатить ({actionRevertCounter})";
        }

        private void DoRevert()
        {
            if (actionRevertCounter == 0)
            {
                MessageBox.Show("Нечего отменять!");
            }
            else
            {
                actionRevertCounter--;
                var action = actionRevertList[actionRevertList.Length - actionRevertCounter - 1];
                // Not implemented
                //polygons.Concat(action);
            }
            UpdateCounterDisplay();
            UpdateList();
        }

        private void NewColorPicker_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Это окно является экспериментальной функцией.\nФункционал этого окна может отсутствовать/ломаться\nРекомендую пользоваться старой панелью выбора цвета.");
            Utility.Dialogs.ColorPicker dialog = new();
            dialog.ShowDialog();
        }
    }
}