using System.Windows.Shapes;

namespace PointEditor.Utility.Actions.Objects.Generic;

internal class RenameObject : IAction
{
    public Shape targetFigure;
    public string oldName;
    public string newName;

    public void Do(object[] args)
    {
        // Args
        // 0 - figure
        // 1 - new name

        targetFigure = (Shape)args[0];
        oldName = targetFigure.Name;
        newName = (string)args[1];
        targetFigure.Name = newName;
    }

    public void Undo()
    {
        targetFigure.Name = oldName;
        targetFigure = null;
    }

    public override string ToString() => $"{oldName} переименован в {newName}";
}
