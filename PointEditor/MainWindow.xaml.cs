using PointEditor.Utility;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
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
        public List<Polygon> polygons = new();

        bool bypassNoPointsWarning = false;
        bool bypassIncorrectMoveInputWarning = false;
        bool bypassNamingWarning = false;
        bool bypassInvalidNumber = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void UpdateList() =>PolygonList.ItemsSource = polygons.Select(x => x.Name);

        private void NewColorPicker_ChangedColor(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {

            if (PolygonList == null)
                return;

            Color newColor = e.NewValue.Safe();
            if (PolygonList.SelectedItems == null || PolygonList.SelectedItems.Count == 0 )
            {
                //foreach (var polygon in polygons)
                //    polygon.Stroke = Preview.Fill;
            }
            else
            {
                foreach (string polyName in PolygonList.SelectedItems)
                    polygons.Where(x => x.Name == polyName).Single().Stroke = NewColorPicker.SelectedColor.Safe().ToBrush();
            }
        }

        // Add new polygon
        private void PolygonAdd_Click(object sender, RoutedEventArgs e)
        {
            Polygon newPolygon = new()
            {
                Stroke = NewColorPicker.SelectedColor.Safe().ToBrush(),
                StrokeThickness = 3,
                Name = $"newPolygon{polygons.Count}"
            };

            polygons.Add(newPolygon);
            mainCanvas.Children.Add(newPolygon);

            UpdateList();

            PolygonList.SelectedItem = newPolygon.Name;
        }

        private void DeleteItems(object sender, RoutedEventArgs e)
        {
            foreach (string itemName in PolygonList.SelectedItems)
            {
                Polygon item = polygons.Where(x => x.Name == itemName).Single();
                mainCanvas.Children.Remove(item);
                polygons.Remove(item);
            }

            UpdateList();
        }

        private void MainCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed &&
                PolygonList.SelectedItem != null)
                polygons.Where(x => x.Name == PolygonList.SelectedItem.ToString()).Single().Points.Add(e.GetPosition(mainCanvas));
        }

        private void GenerateCode(object sender, RoutedEventArgs e)
        {
            if (polygons.Count == 0)
            {
                MessageBox.Show("Для генерации кода на сцене необходим минимум один полигон", "Отмена");
                return;
            }

            string result = string.Empty;

            foreach (Polygon poly in polygons)
            {
                if (poly.Points.Count > 0)
                {
                    SolidColorBrush brush = (SolidColorBrush)poly.Stroke;

                    result += $"// Фигура {poly.Name}\n" +
                              $"Polygon {poly.Name} = new();\n" +
                              $"{poly.Name}.Stroke = new SolidColorBrush() " +
                               "{ Color = Color.FromRgb(" + brush.Color.R +
                               ", " + brush.Color.G +
                               ", " + brush.Color.B + ")};" +
                               "\n\n";

                    foreach (Point point in poly.Points)
                        result += $"{poly.Name}.Points.Add(new Point({point.X.ToString().Replace(',', '.')},{point.Y.ToString().Replace(',', '.')}));\n";

                    result += "\n\n";
                }
                else
                {
                    result += $"// {poly.Name} был пропущен - в фигуре нет точек\n\n";
                }
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
                MessageBoxDialog dialog = new("Переименовать", $"{selected.Name} будет переименован в");
                if (dialog.ShowDialog() == true && dialog.ResponseText.Length > 0)
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
                if (int.TryParse(dialog.ResponseText, out int smooth_factor) &&
                    smooth_factor > 0 &&
                    PolygonList.SelectedItem != null)
                {

                    foreach (string itemName in PolygonList.SelectedItems)
                    {
                        Polygon poly = polygons.Where(x => x.Name == itemName).Single();

                        // Проверяем, есть ли в фигуре точки
                        if (poly.Points.Count > 0)
                        {
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
            MessageBoxDialog dialog = new("Сместить", "Пример ввода - горизонталь;вертикаль");
            if (dialog.ShowDialog() == true)
            {
                string[] coords = dialog.ResponseText.Split(';');
                if (coords.Length == 2 && int.TryParse(coords[0], out int x) && int.TryParse(coords[1], out int y))
                {
                    foreach (string itemName in PolygonList.SelectedItems)
                        polygons.Where(x => x.Name == itemName).Single().Points.Move(new Point(x, y));
                }
                else if (dialog.ResponseText.Length > 0 && !bypassIncorrectMoveInputWarning)
                {
                    Utility.Dialogs.ExceptionDialog exDialog = new("Ошибка смещения", $"Неверный ввод.\nИспользуйте маску:\n(-)горизонталь;(-)вертикаль");
                    exDialog.ShowDialog();
                    bypassIncorrectMoveInputWarning = exDialog.BypassDialog;
                }
            }
        }
    }
}