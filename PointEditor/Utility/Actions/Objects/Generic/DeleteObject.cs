using PointEditor.Utility.TreeViewStorage;
using System;

namespace PointEditor.Utility.Actions.Objects.Generic;

internal class DeleteObject : IAction
{
    public TreeViewGeneric t_Item;
    private Type itemType;
    public void Do(object[] args)
    {
        // Args
        // 0 - target object
        itemType = args[0].GetType();
        t_Item = (TreeViewGeneric)args[0];

        MainWindow.ToolsInstance.TreeViewItem_Remove(t_Item);
    }

    public void Undo()
    {
       if (itemType == typeof(TreeViewFolder))
            MainWindow.ToolsInstance.AddToTreeView<TreeViewFolder>(t_Item.s_Name);
        else if(itemType == typeof(TreeViewShape))
            MainWindow.ToolsInstance.AddToTreeView<TreeViewShape>(t_Item.s_Name, new object[] { ((TreeViewShape)t_Item).GetStoredValue() });


    }
    public override string ToString() => $"Удаление {t_Item.s_Name}";
}
