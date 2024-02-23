using System.Windows.Shapes;

namespace PointEditor.Utility.Actions.Objects.Generic;

internal class RenameObject : IAction
{
    public Shape targetFigure;
    public string oldName;
    public string newName;

    public object Do(object[] args)
    {
        // Args
        // 0 - figure
        // 1 - new name

        targetFigure = (Shape)args[0];
        oldName = targetFigure.Name;
        newName = (string)args[1];
        targetFigure.Name = newName;

        return newName;
    }

    public object Undo()
    {
        targetFigure.Name = oldName;
        return oldName;
    }

    public override string ToString() => $"{oldName} переименован в {newName}";
}
