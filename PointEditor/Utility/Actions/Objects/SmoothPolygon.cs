using System;
using System.Linq;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PointEditor.Utility.Actions.Objects;

/// <summary>
/// TODO
/// </summary>
internal class SmoothPolygon : IAction
{
    int initialFactor;
    Polygon poly;
    public void Do(object[] args)
    {
        // Args
        // 0 - factor
        // 1 - points
        poly = (Polygon)args[1];

        initialFactor = poly.Points.Count;

        // Проверяем, совпадает ли последняя точка с начальной
        if (poly.Points.Last() != poly.Points.First())
            // Добавляем ещё одну точку с координатами начала, чтобы не было резкой прямой линии
            poly.Points.Add(poly.Points.First());
        // Сглаживаем
        poly.Points = Utils.SmootherPolygonCubic(poly.Points, (int)args[0]);
    }

    public void Undo()
    {
        poly.Points = Utils.SmootherPolygonCubic(poly.Points, initialFactor);
    }

    public override string ToString() => "Смягчение фигуры";

}
