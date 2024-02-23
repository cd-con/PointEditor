using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace PointEditor.Utility.Actions.Objects
{
    internal class AddPoint : IAction
    {
        PointCollection points;
        public object Do(object[] args)
        {
            // args
            // 0 - collection
            // 1 - point
            points = (PointCollection)args[0];
            points.Add((Point)args[1]);

            return 0;
        }

        public object Undo()
        {
            points.Remove(points.Last());
            return 0;
        }

        public override string ToString() => "Добавление точки";
    }
}
