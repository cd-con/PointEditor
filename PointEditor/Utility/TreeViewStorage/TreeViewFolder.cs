using System.Windows.Controls;

namespace PointEditor.Utility.TreeViewStorage;

internal class TreeViewFolder : TreeViewGeneric
{
    private string s_folderName;

    public TreeViewFolder(string folderName)
    {
        ViewIcon = VIEWTYPE.Folder;
        s_folderName = folderName;
    }

    public override TreeViewItem Get()
    {
        TreeViewItem t_Item = new()
        {
            Tag = "FOLDER",
            Header = s_folderName,
            Foreground = System.Drawing.Color.White.ToBrush()
        };

        foreach (TreeViewItem item in GetChild())
            t_Item.Items.Add(item);

        return t_Item;
    }

    public override string GetStoredValue() => s_folderName;

    public override void SetStoredValue(object value) => s_folderName = value.ToString();
}
