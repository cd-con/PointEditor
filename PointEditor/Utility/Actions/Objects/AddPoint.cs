using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace PointEditor.Utility.Actions.Objects
{
    internal class AddPoint : IAction
    {
        PointCollection points;
        public void Do(object[] args)
        {
            // args
            // 0 - collection
            // 1 - point
            points = (PointCollection)args[0];
            points.Add((Point)args[1]);
        }

        public void Undo() => points.Remove(points.Last());

        public override string ToString() => "Добавление точки";
    }
}
