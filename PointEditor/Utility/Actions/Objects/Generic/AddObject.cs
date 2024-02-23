using System.Windows.Media;
using System.Windows.Shapes;

namespace PointEditor.Utility.Actions.Objects.Generic;

public abstract class AddObject : IAction
{
    public Shape? Figure;
    public object Do(object[] args)
    {
        // Args
        // 0 - border color
        // 1 - fill color
        // 2 - border thickness
        // 3 - name
        Figure = CreateFigure((Color)args[0],
                            (Color)args[1],
                            (int)args[2],
                            (string)args[3]);
        MainWindow.MainCanvas.Children.Add(Figure);

        return null;
    }

    public abstract Shape CreateFigure(Color borderColor,
                                       Color fillColor,
                                       int borderThickness,
                                       string name);

    public object Undo() {
        MainWindow.MainCanvas.Children.Remove(Figure);
        return null;
    }

    public override string ToString() => $"Создание {Figure.Name}";
}
