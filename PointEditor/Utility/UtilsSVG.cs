using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml;

namespace PointEditor.Utility;

public static class UtilsSVG
{
    /// <summary>
    /// Генерация кода для ломаной
    /// </summary>
    /// <param name="poly"></param>
    /// <returns></returns>
    public static string GetCode(this Polyline poly)
    {
        if (poly.Points.Count > 0)
        {
            string result = string.Empty;
            SolidColorBrush brush = (SolidColorBrush)poly.Stroke;

            result += $"// Ломаная {poly.Name}\n" +
                      $"Polyline {poly.Name} = new();\n" +
                      $"{poly.Name}.Stroke = new SolidColorBrush() " +
                       "{ Color = Color.FromRgb(" + brush.Color.R +
                       ", " + brush.Color.G +
                       ", " + brush.Color.B + ")};" +
                      $"{poly.Name}.StrokeThickness = {poly.StrokeThickness};" +
                       "\n\n";

            foreach (Point point in poly.Points)
                result += $"{poly.Name}.Points.Add(new Point({point.X.ToString().Replace(',', '.')},{point.Y.ToString().Replace(',', '.')}));\n";

            result += "\n\n";

            return result;
        }
        else
            return $"// {poly.Name} был пропущен - в ломаной нет точек\n\n";
    }

    public static string GetSVG(this Polygon poly)
    {
        SolidColorBrush brush = (SolidColorBrush)poly.Stroke;

        NumberFormatInfo nfi = new()
        {
            NumberDecimalSeparator = "."
        };

        return   $"<polygon points=\"{string.Join(" ", poly.Points.Select(point => $"{point.X.ToString(nfi)},{point.Y.ToString(nfi)}"))}\"\r\n" +
                  "fill=\"rgba(0, 0, 0, 0)\"\r\n" +
                  "stroke=\"rgb(" + brush.Color.R +
                       ", " + brush.Color.G +
                       ", " + brush.Color.B + ")\"\r\n" +
                  "stroke-width=\"" + poly.StrokeThickness + "\" /> ";
    }

    public static List<Shape>? ParseSVG(string path)
    {
        XmlDocument doc = new();
        doc.Load(path);
        if (doc.DocumentElement != null)
        {
            List<Shape> importList = new();
            int counter = 0;
            foreach(XmlNode childNode in doc.DocumentElement.ChildNodes)
            {

                var match = Regex.Match(childNode.Attributes["stroke"].Value, @"\((\d{1,3})\,?\s*(\d{1,3})\,?\s*(\d{1,3})\)");
                var groups = match.Groups.Values.ToList();
                groups.RemoveAt(0);
                List<byte> rgbfill = groups.Select(x => byte.Parse(x.Value)).ToList();


                switch (childNode.Name)
                {
                    case "polygon":
                        importList.Add(new Polygon()
                        {
                            Name = $"importedPolygon{counter}",
                            Points = PointCollection.Parse(childNode.Attributes["points"].Value),
                            StrokeThickness = int.Parse(childNode.Attributes["stroke-width"].Value),
                            Stroke = new SolidColorBrush(Color.FromRgb(rgbfill[0], rgbfill[1], rgbfill[2]))
                        });
                        break;
                    default:
                        break;
                }
                counter++;
            }
            return importList;
        }
        return null;
    }
}
