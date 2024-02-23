using System.Windows.Shapes;

namespace PointEditor.Utility.Actions.Objects.Generic;

internal class DeleteObject : IAction
{
    public Shape deletedObject;
    public object Do(object[] args)
    {
        // Args
        // 0 - target object
        deletedObject = (Shape)args[0];
        MainWindow.MainCanvas.Children.Remove(deletedObject);
        return null;
    }
    //89106394339

    public object Undo() => MainWindow.MainCanvas.Children.Add(deletedObject);
    public override string ToString() => $"Удаление {deletedObject.Name}";
}
