using System.Collections.Specialized;
using System.Windows.Controls;

namespace PointEditor.Utility.TreeViewStorage;

public class TreeViewFolder : TreeViewGeneric
{
    public TreeViewFolder(string folderName)
    {
        ViewIcon = VIEWTYPE.Folder;
        s_Name = folderName;

        l_Child.CollectionChanged += (object? sender, NotifyCollectionChangedEventArgs? e) => { Parent?.l_Child.Move(0, 0); };
    }

    public override TreeViewItem Get()
    {
        TreeViewItem t_Item = new()
        {
            Header = s_Name,
            Foreground = System.Drawing.Color.White.ToBrush()
        };

        foreach (TreeViewItem item in GetChild())
            t_Item.Items.Add(item);

        return t_Item;
    }

    public override string GetStoredValue() => s_Name;

    public override void SetStoredValue(object value) => s_Name = value.ToString();
}
