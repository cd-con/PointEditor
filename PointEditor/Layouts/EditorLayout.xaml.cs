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

public class Scene
{
    public bool isModified = false;
    public ObservableCollection<IAction> l_Actions = new();
    public TreeViewFolder T_treeRoot = new("Корень");
    private string? s_filePath;

    private EditorLayout canvasCtx;

    public Scene()
    {
        canvasCtx = new(this);
        l_Actions.CollectionChanged += (object? _a, NotifyCollectionChangedEventArgs? _b) => { isModified = true; };
    }

    public string? GetPath() => s_filePath;

    public string SetPath(string newPath)
    {
        s_filePath = newPath;
        return System.IO.Path.GetFileName(newPath).Split('.')[0];
    }

    public Canvas GetCanvas() => canvasCtx.c_Canvas;

    public EditorLayout GetLayout() => canvasCtx;
}

/// <summary>
/// Логика взаимодействия для EditorLayout.xaml
/// </summary>
public partial class EditorLayout : Page
{
    public Canvas c_Canvas { get; private set; }
    private Shape? S_selected;
    public Scene S_ctx { get; private set; }
    public EditorLayout(Scene scene)
    {
        InitializeComponent();
        c_Canvas = layoutCanvas;
        S_ctx = scene;
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



    private Shape S_editingShape;
    private void MainCanvas_MouseUp(object sender, MouseButtonEventArgs e) => S_editingShape = null;
    private void MainCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && S_editingShape != null)
        {
            if (S_editingShape.GetType() == typeof(Polygon))
                ((Polygon)S_editingShape).Points[((Polygon)S_editingShape).Points.Count - 1] = e.GetPosition(c_Canvas);
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
                                        e.GetPosition(c_Canvas) });
            S_ctx.l_Actions.Add(newAction);
        }
        else
            MessageBox.Show("Неподдерживаемый тип фигуры", "Ошибка!");
    }
}
