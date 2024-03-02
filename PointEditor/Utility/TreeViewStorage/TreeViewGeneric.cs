using System.Collections.Generic;
using System.Windows.Controls;

namespace PointEditor.Utility.TreeViewStorage;

internal abstract class TreeViewGeneric
{
    public List<TreeViewGeneric> l_Child = new();

    internal enum VIEWTYPE
    {
        None = 0,
        Folder = 1
    }


    public VIEWTYPE ViewIcon = VIEWTYPE.None;

    public abstract TreeViewItem Get();

    public abstract object GetStoredValue();

    public abstract void SetStoredValue(object value);

    public IEnumerable<TreeViewItem> GetChild()
    {
        foreach (TreeViewGeneric item in l_Child)
            yield return item.Get();
    }
}
