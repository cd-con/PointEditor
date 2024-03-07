namespace PointEditor.Utility.Actions;

public interface IAction
{
    public void Undo();
    public void Do(object[] args);
}