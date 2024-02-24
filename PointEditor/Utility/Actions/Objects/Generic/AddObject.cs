using System.Windows.Media;
using System.Windows.Shapes;

namespace PointEditor.Utility.Actions.Objects.Generic;

public abstract class AddObject : IAction
{
    public string initialName;
    public Shape? Figure;
    public void Do(object[] args)
    {
        // Args
        // 0 - border color
        // 1 - fill color
        // 2 - border thickness
        // 3 - name
        initialName = (string)args[3];

        Figure = CreateFigure((Color)args[0],
                            (Color)args[1],
                            (int)args[2],
                            (string)args[3]);
        MainWindow.MainCanvas.Children.Add(Figure);
    }

    public abstract Shape CreateFigure(Color borderColor,
                                       Color fillColor,
                                       int borderThickness,
                                       string name);

    public void Undo() {
        MainWindow.MainCanvas.Children.Remove(Figure);
        Figure = null;
    }

    public override string ToString() => $"Создание {initialName}";
}
