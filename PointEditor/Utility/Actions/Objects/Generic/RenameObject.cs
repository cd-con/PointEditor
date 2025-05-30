using PointEditor.Utility.TreeViewStorage;
using System.Windows.Shapes;

namespace PointEditor.Utility.Actions.Objects.Generic;

internal class RenameObject : IAction
{
    public TreeViewGeneric t_Target;
    public string s_old;
    public string s_new;

    public void Do(object[] args)
    {
        // Args
        // 0 - figure
        // 1 - new name

        t_Target = (TreeViewGeneric)args[0];
        s_old = t_Target.s_Name.ToString();
        t_Target.s_Name = s_new = (string)args[1];
    }

    public void Undo()
    {
        t_Target.s_Name = s_old;

        // TODO quickfix
        var a = t_Target.GetStoredValue();

        if (a is Shape shape)
            shape.Name = s_old;

        t_Target = null;
    }

    public override string ToString() => $"{s_old} переименован в {s_new}";
}
