using PointEditor.Utility.TreeViewStorage;
using PointEditor.Utility;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using PointEditor.Utility.Actions.Objects.Generic;
using PointEditor.Utility.Actions;
using PointEditor.Utility.Actions.Objects;
using System.Globalization;

namespace PointEditor.Layouts;

/// <summary>
/// Логика взаимодействия для ToolsWindow.xaml
/// </summary>
public partial class ToolsWindow : Window
{
    public static ToolsWindow? Instance { get; private set; }
    private EditorLayout? S_ctx;

    // Events for plugins?
    public delegate void ColorChangeEvent(Color color);
    public ColorChangeEvent? OnColorChange;

    public delegate void SelectionChangeEvent(TreeViewGeneric? item);
    public SelectionChangeEvent? OnSelectionChange;

    public delegate void RenameEvent(TreeViewGeneric item);
    public RenameEvent? OnRename;

    public delegate void ContextOpenEvent(TreeViewGeneric? item);
    public ContextOpenEvent? OnContextOpen;

    public ToolsWindow()
    {
        InitializeComponent();
        Instance = this;
    }
    

    public void LoadContext(EditorLayout ctx)
    {
        if (S_ctx != null)
            throw new InvalidOperationException("Контекст был уже задан ранее");

        S_ctx = ctx;

        S_ctx.l_Actions.CollectionChanged += ActionsUpdateHandler;

        S_ctx.T_treeRoot.l_Child.CollectionChanged += TreeViewUpdateHandler;

        // Костыль
        UpdateActionsList();
        UpdateTreeView();
    }

    private void ActionsUpdateHandler(object? sender, NotifyCollectionChangedEventArgs? e) => UpdateActionsList();

    private void TreeViewUpdateHandler(object? sender, NotifyCollectionChangedEventArgs? e) => UpdateTreeView();


    public void UnloadContext()
    {
        if (S_ctx == null)
            return;

        S_ctx.l_Actions.CollectionChanged -= ActionsUpdateHandler;
        S_ctx.T_treeRoot.l_Child.CollectionChanged -= TreeViewUpdateHandler;

        S_ctx = null;

        SceneTreeView.Items.Clear();
        ActionsList.ItemsSource = null;

        AddToTreeRoot.IsEnabled = false;
        ClearContextActions();
    }

    public void ReloadContext(EditorLayout ctx)
    {
        UnloadContext();
        LoadContext(ctx);
    }

    /// <summary>
    /// UI-Update functions
    /// </summary>

    private void UpdateTreeView()
    {
        SceneTreeView.Items.Clear();
        SceneTreeView.Items.Add(S_ctx?.T_treeRoot.Get());
    }

    public void UpdateActionsList() => ActionsList.ItemsSource = S_ctx.l_Actions.Select(x => $"{S_ctx.l_Actions.IndexOf(x) + 1}. " + x.ToString());

    public bool b_IsCtxPresent()
    {
        if (S_ctx == null)
            MessageBox.Show("Невозможно совершить это действие -- отсутствует открытая сцена!", "Ошибка отсутствия контекста");

        return S_ctx != null;
    }

    private void CP_Change_Invoker(object sender, RoutedPropertyChangedEventArgs<Color?> e)
    {
        if (SceneTreeView != null) 
            OnColorChange?.Invoke(NewColorPicker.SelectedColor.Safe());
    }

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void TreeView_AddFolder(object sender, RoutedEventArgs e)
    {
        if (b_IsCtxPresent())
        {
            MessageBoxDialog addFolderDialog = new("Новая группа", "Введите название новой группы");

            if (addFolderDialog.ShowDialog() == true)
            {
                AddObject newAction = new();
                newAction.Do(new object[] { addFolderDialog.ResponseText });
                S_ctx?.l_Actions.Add(newAction);

                AddToTreeView<TreeViewFolder>(addFolderDialog.ResponseText);
            }
        }
    }

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void TreeView_AddPoly(object sender, RoutedEventArgs e)
    {
        if (b_IsCtxPresent())
        {
            MessageBoxDialog addShape = new("Новая фигура", "Введите название новой фигуры");
            if (addShape.ShowDialog() == true)
            {
                AddObject newAction = new AddObject();
                string result = addShape.ResponseText.Replace(' ', '_').Replace('.', '_');
                result = string.IsNullOrEmpty(result) ? $"newPolygon{S_ctx.GetCanvas().Children.OfType<Shape>().Count()}" : result;

                if (char.IsDigit(result[0]))
                    result = result.Replace(result[0], '_');

                newAction.Do(new object[] { result });

                Polygon newPolygon = new()
                {
                    Stroke = NewColorPicker.SelectedColor.Safe().ToBrush(),
                    StrokeThickness = 3,
                    Name = result
                };

                AddShapeToDraw(newPolygon);

                S_ctx.l_Actions.Add(newAction);

            }
        }
    }

    internal void AddShapeToDraw(Shape shape)
    {
        S_ctx.GetCanvas().Children.Add(shape);
        AddToTreeView<TreeViewShape>(shape.Name, new object[] { shape });
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
        parent ??= S_ctx.T_treeRoot;

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
        }
    }

    private void SceneTreeView_Rename(object sender, MouseButtonEventArgs e) => TreeViewRename();

    private TreeViewGeneric? FindSelected()
    {
        string? searchName = (string?)((TreeViewItem)SceneTreeView.SelectedItem)?.Header;

        if (string.IsNullOrEmpty(searchName)) return null;

        TreeViewGeneric? selected = S_ctx?.T_treeRoot.s_Name == searchName ? S_ctx?.T_treeRoot : null;

        selected ??= S_ctx?.T_treeRoot.FindChild(searchName);

        return selected;
    }

    private void TreeViewRename()
    {
        if (b_IsCtxPresent())
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
                    !Settings.b_bypassNamingWarning)
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

                    Settings.b_bypassNamingWarning = nameLengthWarnDialog.BypassDialog;
                }

                TreeViewGeneric? parent = selected.GetParent();
                int occurences = 0;
                if (parent != null)
                {
                    occurences = parent.l_Child.Where(x => x.s_Name == result).Count();
                    result += occurences > 0 ? occurences : "";

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
                    occurences = S_ctx.T_treeRoot.l_Child.Where(x => x.s_Name == result).Count();
                    result += occurences > 0 ? occurences : "";
                }
                IAction newRenameAction = new RenameObject();

                newRenameAction.Do(new object[] { selected, result });

                S_ctx.l_Actions.Add(newRenameAction);
            }
        }
    }

    private void TreeView_DeleteItem(object sender, RoutedEventArgs e)
    {
        if (b_IsCtxPresent())
        {
            TreeViewGeneric? selected = FindSelected();

            TreeViewItem_Remove(selected);

            DeleteObject newAction = new();

            newAction.Do(new object[] { selected });

            S_ctx.l_Actions.Add(newAction);
        }
    }

    internal void TreeViewItem_Remove(string name)
    {
        if (string.IsNullOrEmpty(name)) return;

        TreeViewGeneric? selected = S_ctx?.T_treeRoot.l_Child.Where(x => x.s_Name == name).SingleOrDefault();

        selected ??= S_ctx?.T_treeRoot.FindChild(name);

        TreeViewItem_Remove(selected);
    }

    internal void TreeViewItem_Remove(TreeViewGeneric? item)
    {
        if (item == null) return;

        if (item.GetType() == typeof(TreeViewShape))
            S_ctx?.GetCanvas().Children.Remove((Shape)item.GetStoredValue());

        item.GetParent()?.l_Child.Remove(item);
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

        AddToTreeRoot.IsEnabled = selected != null;

        if (selected is TreeViewShape)
            ShapeContextActions();
        else
            ClearContextActions();
    }

    private void ClearSelection(object sender, MouseButtonEventArgs e)
    {
        if (SceneTreeView.SelectedItem != null) ClearTreeViewItemsControlSelection(SceneTreeView.Items, SceneTreeView.ItemContainerGenerator);
    }

    // Мне не нравится
    private static void ClearTreeViewItemsControlSelection(ItemCollection ic, ItemContainerGenerator icg)
    {
        if ((ic != null) && (icg != null))
            for (int i = 0; i < ic.Count; i++)
            {
                TreeViewItem tvi = icg.ContainerFromIndex(i) as TreeViewItem;
                if (tvi != null)
                {
                    ClearTreeViewItemsControlSelection(tvi.Items, tvi.ItemContainerGenerator);
                    tvi.IsSelected = false;
                }
            }
    }





    private void GetCode_Click(object sender, RoutedEventArgs e)
    {
        if (b_IsCtxPresent())
        {
            if (!S_ctx.GetCanvas().Children.OfType<Shape>().Any())
            {
                MessageBox.Show("Для генерации кода на сцене необходим минимум один полигон", "Отмена");
                return;
            }

            string result = string.Empty;

            foreach (Polygon poly in S_ctx.GetCanvas().Children.OfType<Shape>().Cast<Polygon>())
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
    }


    private void Resize_Click(object sender, RoutedEventArgs e)
    {
        if (b_IsCtxPresent())
        {
            MessageBoxDialog resizeDialog = new("Изменить размер", "Фактор изменения размера");
            if (resizeDialog.ShowDialog() == true &&
                double.TryParse(resizeDialog.ResponseText, NumberStyles.Float, CultureInfo.InvariantCulture, out double factor) &&
                factor != 0)
            {
                TreeViewGeneric? selected = FindSelected();

                if (selected is TreeViewShape)
                {
                    Polygon? shape = selected.GetStoredValue() as Polygon;
                    IAction newResizeAction = new ResizePolyGeneric();
                    newResizeAction.Do(new object[] { factor, shape.Points });
                    S_ctx.l_Actions.Add(newResizeAction);
                }
            }
        }
    }

    private void Smooth_Click(object sender, RoutedEventArgs e)
    {
        if (b_IsCtxPresent())
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

                            S_ctx.l_Actions.Add(newAction);
                        }
                        else
                        {
                            if (!Settings.b_bypassNoPointsWarning)
                            {
                                Utility.Dialogs.ExceptionDialog exDialog = new(message: $"Попытка сглаживания фигуры {selectedFigure.Name} не удалась. Количество точек в фигуре должно быть больше 0");
                                Settings.b_bypassNoPointsWarning = exDialog.BypassDialog;
                            }
                        }
                    }
                }
            }
            else
            {
                if (!Settings.b_bypassInvalidNumber)
                {
                    Utility.Dialogs.ExceptionDialog exDialog = new(message: $"Значение `{smootherDialog.ResponseText}` не является валидным для этой операции.");
                    exDialog.ShowDialog();
                    Settings.b_bypassInvalidNumber = exDialog.BypassDialog;
                }
            }
        }
    }

    private void Move_Click(object sender, RoutedEventArgs e)
    {
        if (b_IsCtxPresent())
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
                    if (selected is TreeViewShape)
                    {
                        IAction newAction = new MovePolyGeneric();

                        newAction.Do(new object[] { new Point(x, y), (selected.GetStoredValue() as Polygon).Points });

                        S_ctx.l_Actions.Add(newAction);
                    }
                    UpdateActionsList();
                }
                else if (moveDialog.ResponseText.Length > 0 && !Settings.b_bypassIncorrectMoveInputWarning)
                {
                    Utility.Dialogs.ExceptionDialog exDialog = new("Ошибка смещения", $"Неверный ввод.\nИспользуйте маску:\n(-)горизонталь;(-)вертикаль");
                    exDialog.ShowDialog();
                    Settings.b_bypassIncorrectMoveInputWarning = exDialog.BypassDialog;
                }
            }
        }
    }

    private void Revert_Click(object sender, RoutedEventArgs e)
    {
        if (b_IsCtxPresent() && ActionsList.SelectedItem != null)
        {
            int revertPoint = ActionsList.Items.IndexOf(ActionsList.SelectedItem);
            while (revertPoint != S_ctx.l_Actions.Count)
            {
                IAction revertAction = S_ctx.l_Actions.Last();
                revertAction.Undo();
                S_ctx.l_Actions.Remove(revertAction);
            }
        }
    }

    private void FixRollback_Click(object sender, RoutedEventArgs e)
    {
        S_ctx.l_Actions.Clear();
        UpdateActionsList();
        UpdateTreeView();
    }

    private void SceneTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) => OnSelectionChange?.Invoke(FindSelected());

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        Hide();
        e.Cancel = true;
    }
}
