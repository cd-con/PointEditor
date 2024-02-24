using System.Windows.Shapes;

namespace PointEditor.Utility.Actions.Objects.Generic;

internal class DeleteObject : IAction
{
    public Shape deletedObject;
    public void Do(object[] args)
    {
        // Args
        // 0 - target object
        deletedObject = (Shape)args[0];
        MainWindow.MainCanvas.Children.Remove(deletedObject);
    }

    public void Undo()
    {
        MainWindow.MainCanvas.Children.Add(deletedObject);
        deletedObject = null;
    }
    public override string ToString() => $"Удаление {deletedObject.Name}";
}
