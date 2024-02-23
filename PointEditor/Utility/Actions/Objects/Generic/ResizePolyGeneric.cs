using System.Windows.Media;

namespace PointEditor.Utility.Actions.Objects.Generic;

internal class ResizePolyGeneric : IAction
{
    public double resizeFactor;
    public PointCollection points;
    public object Do(object[] args)
    {
        // Args
        // 0 - resize factor
        // 1 - point collection
        resizeFactor = (double)args[0];
        points = (PointCollection)args[1];
        Resize(resizeFactor, points);
        return null;
    }

    public object Undo()
    {
        Resize(1 / resizeFactor, points);
        return null;
    }

    private static void Resize(double factor, PointCollection collection) => collection.Rescale(factor);

    public override string ToString() => $"Изменение размера {resizeFactor}х";
}
