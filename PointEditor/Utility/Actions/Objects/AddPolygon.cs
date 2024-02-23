using System.Windows.Media;
using System.Windows.Shapes;
using PointEditor.Utility.Actions.Objects.Generic;

namespace PointEditor.Utility.Actions.Objects;

internal class AddPolygon : AddObject
{
    public override Shape CreateFigure(Color borderColor, Color fillColor, int borderThickness, string name)
    {
        Polygon newPolygon = new()
        {
            Stroke = borderColor.ToBrush(),
            StrokeThickness = borderThickness,
            Name = name
        };

        return newPolygon;
    }
}
