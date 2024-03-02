using System.Windows.Controls;
using System.Windows.Shapes;

namespace PointEditor.Utility.TreeViewStorage
{
    internal class TreeViewShape : TreeViewGeneric
    {
        private Shape s_Shape;

        public TreeViewShape(Shape shape)
        {
            s_Shape = shape;
            s_Name = shape.Name;
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

        public override Shape GetStoredValue() => s_Shape;

        public override void SetStoredValue(object value) => s_Shape = (Shape)value;
    }
}
