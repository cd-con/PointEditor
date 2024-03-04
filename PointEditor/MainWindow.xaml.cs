using PointEditor.Utility;
using PointEditor.Utility.Actions;
using PointEditor.Utility.Actions.Objects;
using PointEditor.Utility.Actions.Objects.Generic;
using PointEditor.Utility.TreeViewStorage;
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
        private List<TreeViewGeneric> l_TreeItems = new();

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
            l_TreeItems.Add(new TreeViewFolder("Сцена"));
            UpdateTreeView();
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

        public void UpdateTreeView()
        {
            SceneTreeView.Items.Clear();

            // Этот код нужен на будущее
            // Когда я сделаю импорт нескольких сцен
            foreach (TreeViewGeneric item in l_TreeItems)
                SceneTreeView.Items.Add(item.Get());
        }

        public void UpdateActionsList() => ActionsList.ItemsSource = l_Actions.Select(x => $"{l_Actions.IndexOf(x) + 1}. " + x.ToString());

        private void NewColorPicker_ChangedColor(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (SceneTreeView == null)
                return;

            TreeViewGeneric? selected = FindSelected();

            if (selected == null || selected is not TreeViewShape)
                return;

            Color newColor = e.NewValue.Safe();

            // TODO Вынести в отдельную настройку
            /*if (PolygonList.SelectedItems == null || PolygonList.SelectedItems.Count == 0)
            {
                //foreach (var polygon in polygons)
                //    polygon.Stroke = Preview.Fill;
            }
            else
            {
               foreach (string polyName in PolygonList.SelectedItems)*/
            (selected.GetStoredValue() as Polygon).Stroke = NewColorPicker.SelectedColor.Safe().ToBrush();
            //}
        }

        private void MainCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                TreeViewGeneric selected = FindSelected();

                if (selected == null || selected.GetType() != typeof(TreeViewShape))
                    return;

                Shape selectedShape = ((TreeViewShape)selected).GetStoredValue();
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


                copypasteDialog.ShowDialog();
            }
            finally
            {
                Cursor = null; // Не забываем сбросить курсор
            }
        }

        private void Resize_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxDialog resizeDialog = new("Изменить размер", "Фактор изменения размера");
            if (resizeDialog.ShowDialog() == true &&
                double.TryParse(resizeDialog.ResponseText, NumberStyles.Float, CultureInfo.InvariantCulture, out double factor) &&
                factor != 0)
            {
                TreeViewGeneric? selected = FindSelected();

                if (selected is TreeViewShape) { 
                    Polygon? shape = selected.GetStoredValue() as Polygon;
                    IAction newResizeAction = new ResizePolyGeneric();
                    newResizeAction.Do(new object[] { factor, shape.Points });
                    l_Actions.Add(newResizeAction);
                }
                UpdateActionsList();
            }
        }

        private void Smooth_Click(object sender, RoutedEventArgs e)
        {
            // Oh boy, here we go
            MessageBoxDialog smootherDialog = new("Смягчить", "Введите значение");

            if (smootherDialog.ShowDialog() == true &&
                !string.IsNullOrEmpty(smootherDialog.ResponseText))
            {
                TreeViewGeneric? selected = FindSelected();

                if (int.TryParse(smootherDialog.ResponseText, out int smooth_factor) &&
                    smooth_factor > 0 && selected != null)
                {

                    if (selected.GetType() == typeof(TreeViewShape))
                    {
                        selected = selected as TreeViewShape;
                        Shape? selectedFigure = selected?.GetStoredValue() as Shape;
                        // Проверяем, есть ли в фигуре точки


                        if (selectedFigure is Polygon { Points.Count: > 0 })
                        {
                            IAction newAction = new SmoothPolygon();

                            newAction.Do(new object[] { smooth_factor, selectedFigure });

                            l_Actions.Add(newAction);
                        }
                        else
                        {
                            if (!bypassNoPointsWarning)
                            {
                                Utility.Dialogs.ExceptionDialog exDialog = new(message: $"Попытка сглаживания фигуры {selectedFigure.Name} не удалась. Количество точек в фигуре должно быть больше 0");
                                if (exDialog.ShowDialog() == true && exDialog.isCancelled) { }
                                bypassNoPointsWarning = exDialog.BypassDialog;
                            }
                        }
                        UpdateActionsList();
                    }
                }
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
                    TreeViewGeneric? selected = FindSelected();
                    if (selected is TreeViewShape) {
                        IAction newAction = new MovePolyGeneric();

                        newAction.Do(new object[] { new Point(x, y), (selected.GetStoredValue() as Polygon).Points});

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
            UpdateActionsList();
        }

        private void FixRollback_Click(object sender, RoutedEventArgs e)
        {
            l_Actions.Clear();
            UpdateActionsList();
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TreeView_AddFolder(object sender, RoutedEventArgs e)
        {
            MessageBoxDialog addFolderDialog = new("Новая группа", "Введите название новой группы");

            if (addFolderDialog.ShowDialog() == true)
                AddToTreeView<TreeViewFolder>(addFolderDialog.ResponseText);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TreeView_AddPoly(object sender, RoutedEventArgs e)
        {
            MessageBoxDialog addShape = new("Новая фигура", "Введите название новой фигуры");
            if (addShape.ShowDialog() == true)
            {
                AddPolygon newAction = new AddPolygon();
                string result = addShape.ResponseText.Replace(' ', '_').Replace('.', '_');
                result = string.IsNullOrEmpty(result) ? $"newPolygon{MainCanvas.Children.OfType<Shape>().Count()}" : result;

                if (char.IsDigit(result[0]))
                    result = result.Replace(result[0], '_');

                newAction.Do(new object[] { NewColorPicker.SelectedColor.Safe(),
                                        Colors.Transparent, // TODO: Fill color
                                        3, result });

                l_Actions.Add(newAction);

                AddToTreeView<TreeViewShape>(newAction.Figure.Name, new object[] { newAction.Figure });

                UpdateActionsList();
            }
        }

        /// <summary>
        /// Универсальный метод добавление TreeView элементов в TreeView
        /// </summary>
        /// <typeparam name="T">Тип, наследуемый от TreeViewGeneric</typeparam>
        /// <param name="name"></param>
        /// <param name="args"></param>
        internal void AddToTreeView<T>(string name, object[]? args = null) where T : TreeViewGeneric
        {
            name = string.IsNullOrEmpty(name) ? "Новый элемент" : name;
            int occurences = 0;

            TreeViewGeneric? parent = FindSelected();

            if (parent != null)
            {
                occurences = parent.l_Child.Where(x => x.s_Name == name).Count();
                name += occurences > 0 ? occurences : "";

                if (typeof(T) == typeof(TreeViewFolder))
                {
                    TreeViewFolder folder = new(name);
                    folder.SetParent(parent);
                    parent.l_Child.Add(folder);
                }
                else if (typeof(T) == typeof(TreeViewShape))
                {
                    TreeViewShape shape = new((Shape)args[0]);
                    shape.SetParent(parent);
                    parent.l_Child.Add(shape);
                }

                UpdateTreeView();
            }
        }


        private void SceneTreeView_Rename(object sender, MouseButtonEventArgs e) => TreeViewRename();
        private TreeViewGeneric? FindSelected()
        {
            string? searchName = (string?)((TreeViewItem)SceneTreeView.SelectedItem)?.Header;

            if (string.IsNullOrEmpty(searchName)) return null;

            TreeViewGeneric? parent = l_TreeItems.Where(x => x.s_Name == searchName).SingleOrDefault();

            parent ??= l_TreeItems[0].FindChild(searchName);

            return parent;
        }

        private void TreeViewRename()
        {
            TreeViewGeneric? selected = FindSelected();

            if (selected == null)
                return;

            MessageBoxDialog renameDialog = new("Переименовать", $"{selected.s_Name} будет переименован в");

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
                        TreeViewRename();
                        return;
                    }

                    bypassNamingWarning = nameLengthWarnDialog.BypassDialog;
                }

                TreeViewGeneric? parent = selected.GetParent();
                int occurences = 0;
                if (parent != null)
                {
                    occurences = parent.l_Child.Where(x => x.s_Name == result).Count();
                    result += occurences > 0 ? occurences : "";
                    selected.s_Name = result;

                    // Костыль
                    if (selected.GetType() == typeof(TreeViewShape))
                    {

                        if (char.IsDigit(result[0]))
                            result = result.Replace(result[0], '_');

                        ((Shape)selected.GetStoredValue()).Name = result;
                    }
                }
                else
                {
                    // Если у нас оказался корневой каталог
                    occurences = l_TreeItems.Where(x => x.s_Name == result).Count();
                    result += occurences > 0 ? occurences : "";

                    selected.s_Name = result;
                }
                //IAction newRenameAction = new RenameObject();

                //newRenameAction.Do(new object[] { selected, result });

                //l_Actions.Add(newRenameAction);
                UpdateTreeView();
                //UpdateActionsList();
            }
        }

        private void TreeView_DeleteItem(object sender, RoutedEventArgs e)
        {
            TreeViewGeneric? selected = FindSelected();

            if (selected == null)
                return;

            if (selected.GetType() == typeof(TreeViewShape))
                MainCanvas.Children.Remove((Shape)selected.GetStoredValue());

            selected.GetParent()?.l_Child.Remove(selected);

            UpdateTreeView();
        }

        private void ClearContextActions()
        {
            ItemActionsRoot.Items.Clear();
            ItemActionsRoot.Items.Add(new MenuItem() { Header = "Пусто", IsEnabled = false });
        }

        private void ShapeContextActions()
        {
            ItemActionsRoot.Items.Clear();

            MenuItem scale = new() { Header = "Изменить размер" };
            scale.Click += Resize_Click;

            MenuItem move = new() { Header = "Сместить" };
            move.Click += Move_Click;

            MenuItem smooth = new() { Header = "Смягчить" };
            smooth.Click += Smooth_Click;

            ItemActionsRoot.Items.Add(move);
            ItemActionsRoot.Items.Add(scale);
            ItemActionsRoot.Items.Add(smooth);
        }

        private void MenuContentHandler(object sender, MouseButtonEventArgs e)
        {
            TreeViewGeneric? selected = FindSelected();

            if (selected is TreeViewShape)
            {
                AddToTreeRoot.IsEnabled = false;
                ShapeContextActions();
            }
            else
            {
                AddToTreeRoot.IsEnabled = true;
                ClearContextActions();
            }
        }
    }
}