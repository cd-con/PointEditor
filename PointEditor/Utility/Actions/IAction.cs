namespace PointEditor.Utility.Actions;

internal interface IAction
{
    public void Undo();
    public void Do(object[] args);
}