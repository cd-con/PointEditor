using PointEditor.Utility.TreeViewStorage;

namespace PointEditor.Utility.Actions.Objects.Generic;

public class AddObject : IAction
{
    public string s_Name;
    public void Do(object[] args)
    {
        // Args
        // 0 - name
        s_Name = (string)args[0];
    }

    public void Undo() {
        MainWindow.Instance.Dispatcher.Invoke(() => {
            MainWindow.ToolsInstance.TreeViewItem_Remove(s_Name);
        });
    }

    public override string ToString() => $"Создание {s_Name}";
}
