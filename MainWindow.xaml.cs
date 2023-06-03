using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        public List<Polygon> polygons = new List<Polygon>();
        private int actionRevertCounter;
        
        // TODO Переделать в словарь
        public List<Polygon>[] actionRevertList = new List<Polygon>[10]; // Хранилще наших действий
        public Color PenColor = new();
        public SolidColorBrush StrokeBrush = new();

        bool bypassNoPointsWarning = false;
        bool bypassIncorrectMoveInputWarning = false;
        bool bypassNamingWarning = false;

        public MainWindow()
        {
            InitializeComponent();            
        }

        private void UpdateList()
        {
            PolygonList.ItemsSource = polygons.Select(x => x.Name);
        }

        private void ColorPicker_HEX_TextChanged(object sender, TextChangedEventArgs e)
        {

            Match regexMatch = Regex.Match(ColorPicker_HEX.Text, @"([0-9A-Fa-f]{6})");

            if (regexMatch.Success)
            {
                byte[] hexColors = Utility.Methods.StringToByteArray(regexMatch.Value);

                PenColor.R = hexColors[0];
                R.Value = Convert.ToInt32(PenColor.R);

                PenColor.G = hexColors[1];
                G.Value = Convert.ToInt32(PenColor.G);

                PenColor.B = hexColors[2];
                B.Value = Convert.ToInt32(PenColor.B);

                PenColor.A = 0xFF;
                Preview.Fill = new SolidColorBrush(PenColor);

                StrokeBrush.Color = PenColor;
            }
        }

        private void Slider_ValueChanged(object sender, object _)
        {
            Slider slider = sender as Slider;
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
            ColorPicker_HEX.Text = $"#{Utility.Methods.ByteArrayToString(new byte[] { PenColor.R, PenColor.G, PenColor.B })}";
        }


        // Add new polygon
        private void PolygonAdd_Click(object sender, RoutedEventArgs e)
        {
            Polygon newPolygon = new Polygon { Stroke = StrokeBrush, StrokeThickness = 3, Name = $"newPolygon{polygons.Count}" };

            actionRevertList = Utility.Methods.Shift(actionRevertList, new List<Polygon>() { newPolygon });
            AddCounter();
            UpdateCounterDisplay();

            polygons.Add(newPolygon);
            PolygonList.SelectedItem = newPolygon.Name;
            mainCanvas.Children.Add(newPolygon);
            UpdateList();
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
            if (StrokeBrush.Color == Colors.Transparent)
            {
                // ♿♿♿
                ColorPicker_HEX.Text = "#696969";
            }
            if (e.ButtonState == MouseButtonState.Pressed && PolygonList.SelectedItem != null)
                polygons.Where(x => x.Name == PolygonList.SelectedItem).Single().Points.Add(e.GetPosition(this));
        }

        private void GenerateCode(object sender, RoutedEventArgs e)
        {
            output.Clear();
            output.Text += "// Скопируй эти значения в свой код\n";
            foreach (Polygon poly in polygons)
            {
                foreach (Point point in poly.Points)
                {
                    output.Text += $"{poly.Name}.Points.Add(new Point({point.X},{point.Y}));\n";
                }
                output.Text += "\n\n";
            }
        }

        private void Scale_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxDialog dialog = new MessageBoxDialog("Изменить размер", "Фактор изменения размера");
            if (dialog.ShowDialog() == true)
            {
                if (double.TryParse(dialog.ResponseText, out double rescale_factor))
                {
                    foreach (string itemName in PolygonList.SelectedItems)
                    {
                        Utility.Methods.ResizePolygon(polygons.Where(x => x.Name == itemName).Single().Points, rescale_factor);
                    }
                }
            }
        }

        private void PolygonList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Polygon selected = polygons.Where(x => x.Name == PolygonList.SelectedItem).Single();
            MessageBoxDialog dialog = new MessageBoxDialog("Переименовать", $"{selected.Name} будет переименован в");
            if (dialog.ShowDialog() == true && dialog.ResponseText.Count() > 0)
            {
                string result = dialog.ResponseText.Replace(' ', '_');

                if (result.Length > 16 && !bypassNamingWarning) 
                {
                    Utility.Dialogs.ExceptionDialog exDialog = new("Предупреждение", $"Рекомендуем укоротить название до 16 символов и меньше.");
                    if (exDialog.ShowDialog() == true && exDialog.isCancelled)
                    {
                        PolygonList_MouseDoubleClick(null,null);
                        return;
                    }
                    bypassNamingWarning = exDialog.BypassDialog;
                }

                Polygon[] occurences = polygons.Where(x => x.Name == dialog.ResponseText).ToArray();
                                
                if (occurences.Length > 0)
                    result += occurences.Length;

                // Сохраняем действе для отката
                actionRevertList = Utility.Methods.Shift(actionRevertList, new List<Polygon>() { selected });

                PolygonList.SelectedItem = result;
                selected.Name = result;
                UpdateList();
            }
        }

        private void Smooth_Click(object sender, RoutedEventArgs e)
        {
            // Oh boy, here we go
            MessageBoxDialog dialog = new MessageBoxDialog("Смягчить", "Введите значение (больше - дольше)");
            if (dialog.ShowDialog() == true && int.TryParse(dialog.ResponseText, out int smooth_factor))
            {
                if (PolygonList.SelectedItem != null)
                {
                    // Храним все объекты
                    List<Polygon> revertItems = new List<Polygon>();

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
                            poly.Points = Utility.Methods.SmootherPolygonCubic(poly.Points, smooth_factor);
                        }
                        else
                        {
                            if (!bypassNoPointsWarning)
                            {
                                Utility.Dialogs.ExceptionDialog exDialog = new(message: $"Попытка сглаживания фигуры {itemName} не удалась. Количество точек в фигуре должно быть больше 0");
                                if (exDialog.ShowDialog() == true && exDialog.isCancelled)
                                {
                                    // Логика отката действий
                                    actionRevertList = Utility.Methods.Shift(actionRevertList, revertItems);
                                    AddCounter();
                                    doRevert();
                                    UpdateCounterDisplay();
                                    /*foreach (string revertItemName in PolygonList.SelectedItems)
                                    {
                                        if ()
                                    }*/

                                    return;
                                }
                                bypassNoPointsWarning = exDialog.BypassDialog;
                            }
                        }
                        // Сохраняем действе для отката
                        actionRevertList = Utility.Methods.Shift(actionRevertList, revertItems);
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
                        Utility.Methods.MovePolygon(poly.Points, x, y);
                    }
                }
                else if(dialog.ResponseText.Length > 0 && !bypassIncorrectMoveInputWarning)
                {
                    Utility.Dialogs.ExceptionDialog exDialog = new("Ошибка смещения", $"Неверный ввод.\nИспользуйте маску:\n(-)горизонталь;(-)вертикаль");
                    exDialog.ShowDialog();
                }
            }
        }

        private void revertAction_Click(object sender, RoutedEventArgs e)
        {
            doRevert();
        }

        private void AddCounter() => actionRevertCounter += actionRevertCounter <= 10 ? 1 : 0;

        private void UpdateCounterDisplay()
        {
            revertAction.Content = $"Откатить ({actionRevertCounter})";
        }

        private void doRevert()
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
    } 
}