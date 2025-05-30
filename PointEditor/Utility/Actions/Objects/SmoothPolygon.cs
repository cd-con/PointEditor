using System.Linq;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PointEditor.Utility.Actions.Objects;

/// <summary>
/// TODO
/// </summary>
internal class SmoothPolygon : IAction
{
    PointCollection initial;
    Polygon smoothered;
    string initialName;
    public void Do(object[] args)
    {
        // Args
        // 0 - factor
        // 1 - points
        smoothered = (Polygon)args[1];
        initialName = smoothered.Name.ToString();
        initial = smoothered.Points.Clone();
        // Проверяем, совпадает ли последняя точка с начальной
        if (smoothered.Points.Last() != smoothered.Points.First())
            smoothered.Points.Add(smoothered.Points.First());// Добавляем ещё одну точку с координатами начала, чтобы не было резкой прямой линии
        // Сглаживаем
        smoothered.Points = UtilsGeometry.SmootherPolygonCubic(smoothered.Points, (int)args[0]);
    }

    public void Undo() => smoothered.Points = initial;

    public override string ToString() => $"Смягчение фигуры {initialName}";

}
