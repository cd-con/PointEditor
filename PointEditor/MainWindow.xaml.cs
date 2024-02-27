﻿using PointEditor.Utility;
using PointEditor.Utility.Actions;
using PointEditor.Utility.Actions.Objects;
using PointEditor.Utility.Actions.Objects.Generic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
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
        private List<IAction> l_Actions = new();

        private const int APP_VERSION = 20;


        bool bypassNoPointsWarning = false;
        bool bypassIncorrectMoveInputWarning = false;
        bool bypassNamingWarning = false;
        bool bypassInvalidNumber = false;

        public static Canvas MainCanvas { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            CheckUpdates();
            MainCanvas = mainCanvas;
        }

        public async void CheckUpdates()
        {
            using (HttpClient client = new()
            {
                BaseAddress = new Uri("https://raw.githubusercontent.com/cd-con/PointEditor/master/currentVersion.txt")
            })
            {
                try
                {
                    string s_version = await client.GetStringAsync(client.BaseAddress);
                    s_version = s_version.TrimEnd();
                    if (int.TryParse(s_version, out int version) && version > APP_VERSION)
                    {

                        if (MessageBox.Show("Вышла новая версия приложения!\nОбновить сейчас?", "Обновление", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            Process.Start("Updater.exe");
                            Application.Current.Shutdown();
                        }

                    }
                }
                catch (HttpRequestException ex)
                {
                    MessageBox.Show(ex.Message);
                    // Kwuh
                }
            }
        }

        public void UpdateList() => PolygonList.ItemsSource = MainCanvas.Children.OfType<Shape>().Select(x => x.Name);
        public void UpdateActionsList() => ActionsList.ItemsSource = l_Actions.Select(x => $"{l_Actions.IndexOf(x) + 1}. " + x.ToString());

        private void NewColorPicker_ChangedColor(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {

            if (PolygonList == null)
                return;

            Color newColor = e.NewValue.Safe();
            if (PolygonList.SelectedItems == null || PolygonList.SelectedItems.Count == 0)
            {
                //foreach (var polygon in polygons)
                //    polygon.Stroke = Preview.Fill;
            }
            else
            {
                foreach (string polyName in PolygonList.SelectedItems)
                    MainCanvas.Children.OfType<Shape>().Where(x => x.Name == polyName).Single().Stroke = NewColorPicker.SelectedColor.Safe().ToBrush();
            }
        }

        // Add new polygon
        private void PolygonAdd_Click(object sender, RoutedEventArgs e)
        {
            IAction newAction = new AddPolygon();

            newAction.Do(new object[] { NewColorPicker.SelectedColor.Safe(),
                                        Colors.Transparent, // TODO: Fill color
                                        3,
                                        $"newPolygon{MainCanvas.Children.OfType<Shape>().Count()}"});

            l_Actions.Add(newAction);

            UpdateList();
            UpdateActionsList();

            PolygonList.SelectedItem = mainCanvas.Children.OfType<Shape>().Last().Name;
        }

        private void DeleteItems(object sender, RoutedEventArgs e)
        {
            foreach (string itemName in PolygonList.SelectedItems)
            {
                Shape item = MainCanvas.Children.OfType<Shape>().Where(x => x.Name == itemName).Single();

                IAction newAction = new DeleteObject();

                newAction.Do(new object[] { item });
                l_Actions.Add(newAction);
            }

            UpdateList();
            UpdateActionsList();
        }

        private void MainCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed &&
                PolygonList.SelectedItem != null)
            {
                Shape selectedShape = MainCanvas.Children.OfType<Shape>().Where(x => x.Name == PolygonList.SelectedItem.ToString()).Single();

                if (selectedShape.GetType() == typeof(Polygon))
                {
                    IAction newAction = new AddPoint();
                    newAction.Do(new object[] { ((Polygon)selectedShape).Points, e.GetPosition(mainCanvas) });
                    l_Actions.Add((newAction));
                    UpdateActionsList();
                }
                else
                    MessageBox.Show("Неподдерживаемый тип фигуры", "Ошибка!");
            }
        }

        private void GenerateCode(object sender, RoutedEventArgs e)
        {
            if (!MainCanvas.Children.OfType<Shape>().Any())
            {
                MessageBox.Show("Для генерации кода на сцене необходим минимум один полигон", "Отмена");
                return;
            }

            string result = string.Empty;

            foreach (Polygon poly in MainCanvas.Children.OfType<Shape>().Cast<Polygon>())
                result += poly.GetCode();

            Cursor = Cursors.Wait; // Дадим подумать...

            try
            {
                Clipboard.SetText(result);
                MessageBox.Show("Код успешно скопирован в буфер обмена", "Код скопирован");
            } 
            catch (System.Runtime.InteropServices.COMException)
            {
                MessageBoxDialog copypasteDialog = new("Произошла ошибка", 
                                                       "Невозможно скопировать код в буфер обмена.\nКод предоставлен ниже.");
                copypasteDialog.ResponseTextBox.Text = result;

                Cursor = null; // Не забываем сбросить курсор

                copypasteDialog.ShowDialog();
            }
        }

        private void Resize_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxDialog resizeDialog = new("Изменить размер", "Фактор изменения размера");
            if (resizeDialog.ShowDialog() == true &&
                double.TryParse(resizeDialog.ResponseText, NumberStyles.Float, CultureInfo.InvariantCulture, out double factor) &&
                factor != 0)
            {
                foreach (string itemName in PolygonList.SelectedItems)
                {
                    IAction newResizeAction = new ResizePolyGeneric();
                    newResizeAction.Do(new object[] { factor, ((Polygon)MainCanvas.Children.OfType<Shape>().Where(x => x.Name == itemName).Single()).Points });
                    l_Actions.Add(newResizeAction);
                }
                UpdateActionsList();
            }
        }

        private void PolygonList_MouseDoubleClick(object sender, MouseButtonEventArgs e) => Rename();

        private void Rename()
        {
            if (PolygonList.SelectedItems.Count > 0)
            {
                Shape selected = MainCanvas.Children.OfType<Shape>()
                                .Where(x => x.Name == PolygonList.SelectedItem.ToString()).Single();

                MessageBoxDialog renameDialog = new("Переименовать", $"{selected.Name} будет переименован в");

                if (renameDialog.ShowDialog() == true &&
                    renameDialog.ResponseText.Length > 0)
                {
                    string result = renameDialog.ResponseText.Replace(' ', '_').Replace('.', '_');

                    if (result.Length > 16 &&
                        !bypassNamingWarning)
                    {
                        Utility.Dialogs.ExceptionDialog nameLengthWarnDialog = new("Предупреждение",
                                                                      $"Рекомендуем укоротить название до 16 символов и меньше.");

                        if (nameLengthWarnDialog.ShowDialog() == true &&
                            nameLengthWarnDialog.isCancelled)
                        {
                            // TODO Убрать рекурсию
                            Rename();
                            return;
                        }

                        bypassNamingWarning = nameLengthWarnDialog.BypassDialog;
                    }

                    Shape[] occurences = MainCanvas.Children.OfType<Shape>()
                                        .Where(x => x.Name == renameDialog.ResponseText).ToArray();

                    if (occurences.Length > 0)
                        result += occurences.Length;


                    PolygonList.SelectedItem = result;

                    IAction newRenameAction = new RenameObject();

                    newRenameAction.Do(new object[] { selected, result });

                    l_Actions.Add(newRenameAction);
                    UpdateList();
                    UpdateActionsList();
                }
            }
        }

        private void Smooth_Click(object sender, RoutedEventArgs e)
        {
            // Oh boy, here we go
            MessageBoxDialog smootherDialog = new("Смягчить", "Введите значение");
            if (smootherDialog.ShowDialog() == true &&
                smootherDialog.ResponseText.Length > 0)
            {
                if (int.TryParse(smootherDialog.ResponseText, out int smooth_factor) &&
                    smooth_factor > 0 &&
                    PolygonList.SelectedItem != null)
                {

                    foreach (string itemName in PolygonList.SelectedItems)
                    {
                        Polygon poly = (Polygon)MainCanvas.Children.OfType<Shape>()
                                               .Where(x => x.Name == itemName).Single();
                        // Проверяем, есть ли в фигуре точки
                        if (poly.Points.Count > 0)
                        {
                            IAction newAction = new SmoothPolygon();

                            newAction.Do(new object[] { smooth_factor, poly });

                            l_Actions.Add(newAction);
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
                    UpdateActionsList();
                }
                else
                {
                    if (!bypassInvalidNumber)
                    {
                        Utility.Dialogs.ExceptionDialog exDialog = new(message: $"Значение `{smootherDialog.ResponseText}` не является валидным для этой операции.");
                        exDialog.ShowDialog();
                        bypassInvalidNumber = exDialog.BypassDialog;
                    }
                }
            }
        }

        private void Move_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxDialog moveDialog = new("Сместить", "Пример ввода - горизонталь;вертикаль");
            if (moveDialog.ShowDialog() == true)
            {
                string[] coords = moveDialog.ResponseText.Split(';');
                if (coords.Length == 2 &&
                    int.TryParse(coords[0], out int x) &&
                    int.TryParse(coords[1], out int y))
                {
                    foreach (string itemName in PolygonList.SelectedItems)
                    {
                        IAction newAction = new MovePolyGeneric();

                        newAction.Do(new object[] { new Point(x, y), ((Polygon)MainCanvas.Children.OfType<Shape>().Where(x => x.Name == itemName).Single()).Points });

                        l_Actions.Add(newAction);
                    }
                    UpdateActionsList();
                }
                else if (moveDialog.ResponseText.Length > 0 && !bypassIncorrectMoveInputWarning)
                {
                    Utility.Dialogs.ExceptionDialog exDialog = new("Ошибка смещения", $"Неверный ввод.\nИспользуйте маску:\n(-)горизонталь;(-)вертикаль");
                    exDialog.ShowDialog();
                    bypassIncorrectMoveInputWarning = exDialog.BypassDialog;
                }
            }
        }

        private void Revert_Click(object sender, RoutedEventArgs e)
        {
            if (ActionsList.SelectedItem != null)
            {
                int revertPoint = ActionsList.Items.IndexOf(ActionsList.SelectedItem);
                while (revertPoint != l_Actions.Count)
                {
                    IAction revertAction = l_Actions.Last();
                    revertAction.Undo();
                    l_Actions.Remove(revertAction);
                }
            }
            UpdateList();
            UpdateActionsList();
        }

        private void FixRollback_Click(object sender, RoutedEventArgs e)
        {
            l_Actions.Clear();
            UpdateActionsList();
        }
    }
}