using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace PointEditor.Utility.Actions.Objects.Generic;

internal class MovePolyGeneric : IAction
{
    public Point offset;
    public PointCollection points;
    public object Do(object[] args)
    {
        // Args
        // 0 - offset factor
        // 1 - point collection
        offset = (Point)args[0];
        points = (PointCollection)args[1];
        Move(offset, points);
        return null;
    }

    public object Undo()
    {
        Move(offset.Scale(-1), points);
        return null;
    }

    private static void Move(Point factor, PointCollection collection) => collection.Move(factor);

    public override string ToString() => $"Смещение фигуры на {offset.X}:{offset.Y}";
}
