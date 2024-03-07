using PointEditor.Utility.TreeViewStorage;
using PointEditor.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using PointEditor.Utility.Scenes;
using PointEditor.Utility.Actions.Objects.Generic;
using PointEditor.Utility.Actions;

namespace PointEditor.Layouts
{
    /// <summary>
    /// Логика взаимодействия для ToolsWindow.xaml
    /// </summary>
    public partial class ToolsWindow : Window
    {
        private Scene ctx;

        // Events for plugins?
        public delegate void ColorChangeEvent(Color color);
        public static ColorChangeEvent? OnColorChange;

        public delegate void SelectionChangeEvent(TreeViewGeneric item);
        public static SelectionChangeEvent? OnSelectionChange;

        public delegate void RenameEvent(TreeViewGeneric item);
        public static RenameEvent? OnRename;

        public delegate void ContextOpenEvent(TreeViewGeneric? item);
        public static ContextOpenEvent? OnContextOpen;

        public ToolsWindow(Scene currentScene)
        {
            InitializeComponent();
            ctx = currentScene;
        }

        /// <summary>
        /// UI-Update functions
        /// </summary>

        private void UpdateTreeView()
        {
            SceneTreeView.Items.Clear();

            // Этот код нужен на будущее
            // Когда я сделаю импорт нескольких сцен
            foreach (TreeViewGeneric item in ctx.l_TreeItems)
                SceneTreeView.Items.Add(item.Get());
        }

        public void UpdateActionsList() => ActionsList.ItemsSource = ctx.l_Actions.Select(x => $"{ctx.l_Actions.IndexOf(x) + 1}. " + x.ToString());

        private void ColorPicker_Change(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (SceneTreeView != null) OnColorChange?.Invoke(NewColorPicker.SelectedColor.Safe());
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
            {
                AddObject newAction = new();
                newAction.Do(new object[] { addFolderDialog.ResponseText });
                ctx.l_Actions.Add(newAction);

                AddToTreeView<TreeViewFolder>(addFolderDialog.ResponseText);

                UpdateActionsList();
            }
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
                AddObject newAction = new AddObject();
                string result = addShape.ResponseText.Replace(' ', '_').Replace('.', '_');
                result = string.IsNullOrEmpty(result) ? $"newPolygon{MainCanvas.Children.OfType<Shape>().Count()}" : result;

                if (char.IsDigit(result[0]))
                    result = result.Replace(result[0], '_');

                newAction.Do(new object[] { result });

                Polygon newPolygon = new()
                {
                    Stroke = NewColorPicker.SelectedColor.Safe().ToBrush(),
                    StrokeThickness = 3,
                    Name = result
                };

                mainCanvas.Children.Add(newPolygon);

                l_Actions.Add(newAction);

                AddToTreeView<TreeViewShape>(newAction.s_Name, new object[] { newPolygon });

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

            TreeViewGeneric? selected = l_TreeItems.Where(x => x.s_Name == searchName).SingleOrDefault();

            for (int i = 0; i < l_TreeItems.Count; i++)
            {
                selected ??= l_TreeItems[i].FindChild(searchName);

                if (selected != null)
                    return selected;
            }

            return null;
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
                }
                IAction newRenameAction = new RenameObject();

                newRenameAction.Do(new object[] { selected, result });

                l_Actions.Add(newRenameAction);
                UpdateTreeView();
                UpdateActionsList();
            }
        }

        private void TreeView_DeleteItem(object sender, RoutedEventArgs e)
        {
            TreeViewGeneric? selected = FindSelected();

            TreeViewItem_Remove(selected);

            DeleteObject newAction = new();

            newAction.Do(new object[] { selected });

            l_Actions.Add(newAction);

            UpdateActionsList();
            UpdateTreeView();
        }

        internal void TreeViewItem_Remove(string name)
        {
            if (string.IsNullOrEmpty(name)) return;

            TreeViewGeneric? selected = l_TreeItems.Where(x => x.s_Name == name).SingleOrDefault();

            for (int i = 0; i < l_TreeItems.Count; i++)
            {
                selected ??= l_TreeItems[i].FindChild(name);

                TreeViewItem_Remove(selected);
            }
        }

        internal void TreeViewItem_Remove(TreeViewGeneric? item)
        {
            if (item == null) return;

            if (item.GetType() == typeof(TreeViewShape))
                MainCanvas.Children.Remove((Shape)item.GetStoredValue());

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
            if (SceneTreeView.SelectedItem != null)
                ClearTreeViewItemsControlSelection(SceneTreeView.Items, SceneTreeView.ItemContainerGenerator);
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
    }
}
}
