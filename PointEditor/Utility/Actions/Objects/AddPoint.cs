using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PointEditor.Utility.Actions.Objects
{
    internal class AddPoint : IAction
    {
        Polygon polygon;
        public void Do(object[] args)
        {
            // args
            // 0 - collection
            // 1 - point
            polygon = (Polygon)args[0];
            polygon.Points.Add((Point)args[1]);
        }

        public void Undo() => polygon.Points.Remove(polygon.Points.Last());

        public override string ToString() => "Добавление точки";
    }
}
