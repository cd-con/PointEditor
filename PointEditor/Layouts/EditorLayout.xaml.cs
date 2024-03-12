using PointEditor.Utility;
using PointEditor.Utility.Actions;
using PointEditor.Utility.Actions.Objects;
using PointEditor.Utility.TreeViewStorage;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PointEditor.Layouts;

/// <summary>
/// Логика взаимодействия для EditorLayout.xaml
/// </summary>
public partial class EditorLayout : Page
{
    public bool isModified = false;
    private string? s_filePath;
    
    public TreeViewFolder T_treeRoot = new("Корень");
    public ObservableCollection<IAction> l_Actions = new();

    public string? GetPath() => s_filePath;

    public string SetPath(string newPath)
    {
        s_filePath = newPath;
        return System.IO.Path.GetFileName(newPath).Split('.')[0];
    }

    public Canvas GetCanvas() => layoutCanvas;
    
    private Shape? S_selected;
    private Shape S_editingShape;

    public EditorLayout()
    {
        InitializeComponent();
        l_Actions.CollectionChanged += (object? _a, NotifyCollectionChangedEventArgs? _b) => { isModified = true; };
        MainWindow.ToolsInstance.OnSelectionChange += SelectionChangeEvent;
        MainWindow.ToolsInstance.OnColorChange += (Color color) => { S_selected.Stroke = color.ToBrush(); };
    }

    public void SelectionChangeEvent(TreeViewGeneric? item)
    {
        if (item != null && item is TreeViewShape t_Shape)
            S_selected = t_Shape.GetStoredValue();
        else 
            S_selected = null;            
    }

    private void MainCanvas_MouseUp(object sender, MouseButtonEventArgs e) => S_editingShape = null;
    private void MainCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && S_editingShape != null)
        {
            if (S_editingShape.GetType() == typeof(Polygon))
                ((Polygon)S_editingShape).Points[((Polygon)S_editingShape).Points.Count - 1] = e.GetPosition(layoutCanvas);
            else
                MessageBox.Show("Неподдерживаемый тип фигуры", "Ошибка!");
        }
    }

    private void MainCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (S_selected == null)
            return;

        S_editingShape = S_selected;

        if (S_editingShape.GetType() == typeof(Polygon))
        {
            AddPoint newAction = new();
            newAction.Do(new object[] { (Polygon)S_editingShape, 
                                        e.GetPosition(layoutCanvas) });
            l_Actions.Add(newAction);
        }
        else
            MessageBox.Show("Неподдерживаемый тип фигуры", "Ошибка!");
    }
}
