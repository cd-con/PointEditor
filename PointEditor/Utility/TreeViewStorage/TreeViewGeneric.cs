using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Xml.Linq;

namespace PointEditor.Utility.TreeViewStorage;
public abstract class TreeViewGeneric
{
    protected TreeViewGeneric? Parent;
    public ObservableCollection<TreeViewGeneric> l_Child = new();
    public string s_Name = string.Empty;
    public enum VIEWTYPE
    {
        None = 0,
        Folder = 1,
        Scene = 2
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

    public TreeViewGeneric? FindChild(string name)
    {
        foreach(TreeViewGeneric item in l_Child)
        {
            if (item.s_Name == name)
                return item;

            // Я забыл возвращать...
            TreeViewGeneric? childItem = item.FindChild(name);

            if (childItem != null)
                return childItem;
        }
        return null;
    }

    public void SetParent(TreeViewGeneric newParent) => Parent = newParent;

    public List<TreeViewGeneric> FindChildrenOf<T>() => FindChildrenOf(typeof(T));
    public List<TreeViewGeneric> FindChildrenOf(Type type)
    {
        List<TreeViewGeneric> result = new();

        foreach (TreeViewGeneric item in l_Child)
        {
            if (item.GetType() == type)
                result.Add(item);

            result = result.Concat(item.FindChildrenOf(type)).ToList();
        }
        return result;
    }

    public TreeViewGeneric? GetParent() => Parent;
}
