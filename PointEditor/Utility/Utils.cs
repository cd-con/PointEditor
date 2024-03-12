using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml;

namespace PointEditor.Utility;

public static class Settings
{
    public static bool b_bypassNamingWarning = false;
    public static bool b_bypassNoPointsWarning = false;
    public static bool b_bypassInvalidNumber = false;
    public static bool b_bypassIncorrectMoveInputWarning = false;
}

/// <summary>
/// Вспомогательные методы
/// </summary>
public static class Utils
{
    /// <summary>
    /// Найти сумму двух точек.
    /// </summary>
    /// <param name="point">Исходная точка</param>
    /// <param name="other">Смещение</param>
    /// <returns>Смещённая точка</returns>
    public static Point Add(this Point point, Point other) => new(point.X + other.X, point.Y + other.Y);

    /// <summary>
    /// Умножает точку на значение.
    /// </summary>
    /// <param name="point">Исходная точка</param>
    /// <param name="factor">Множитель</param>
    /// <returns>Смещённая точка</returns>
    public static Point Scale(this Point point, double factor) => new(point.X * factor, point.Y * factor);

    /// <summary>
    /// Делит точку на значение.
    /// </summary>
    /// <param name="point">Исходная точка</param>
    /// <param name="factor">Множитель</param>
    /// <returns>Смещённая точка</returns>
    public static Point Shrink(this Point point, double factor) => new(point.X / factor, point.Y / factor);

    public static Point GetCenter(PointCollection collection) => collection.Aggregate(new Point(0, 0), (current, point) => current.Add(point)).Shrink(collection.Count);

    /// <summary>
    /// Конвертирует градусы в радианы.
    /// </summary>
    /// <param name="angle">Исходный угол</param>
    /// <returns>Угол в радианах</returns>
    
    [Obsolete("Метод помечен на удаление как неиспользуемый.")]
    public static double ToRadians(this double angle) => ((Math.PI / 180) * angle);

    /// <summary>
    /// Превращает безопасным способом bool? в bool.
    /// Если bool? == null, возвращает false
    /// </summary>
    /// <param name="value">Исходное значение</param>
    /// <returns>Исходное значение bool? конвертированное в bool</returns>
    public static bool NotNullBool(this bool? value) => value != null && (bool)value;


    /// <summary>
    /// Увеличивает фигуру в размерах. Не сохраняет исходную точку и смещает фигуру.
    /// </summary>
    /// <param name="points">Коллекция точек фигуры</param>
    /// <param name="factor">Множитель</param>
    public static void ResizePolygon(PointCollection points, double factor)
    {
        for (int pointID = 0; pointID < points.Count; pointID++)
            points[pointID] = points[pointID].Scale(factor);
    }

    /// <summary>
    /// Увеличивает точку с сохранением центра.
    /// </summary>
    /// <param name="point">Исходная точка</param>
    /// <param name="factor">Множитель</param>
    /// <returns>Смещённая точка</returns>
    public static void Rescale(this PointCollection collection, double factor)
    {
        // Ищем исходный центр
        Point oldCenter = GetCenter(collection);

        // Бегаем по всем точкам
        ResizePolygon(collection, factor);

        // Ищем новый центр
        Point newCenter = GetCenter(collection);
        Point offset = (Point)(oldCenter - newCenter);

        // Смещаем
        collection.Move(offset);
    }

    /// <summary>
    /// Смещает фигуру по координатам.
    /// </summary>
    /// <param name="offset">Смещение</param>
    public static void Move(this PointCollection points, Point offset)
    {
        for (int pointID = 0; pointID < points.Count; pointID++)
            points[pointID] = points[pointID].Add(offset);
    }

    /// <summary>
    /// Сглаживание фигуры при помощи кубической интерполяции.
    /// </summary>
    /// <param name="points">Коллекция точек фигуры</param>
    /// <param name="quality">Качество итоговой фигуры</param>
    /// <returns>Новая коллекция точек фигуры</returns>        
    public static PointCollection SmootherPolygonCubic(PointCollection points, int quality)
    {
        double[] xs = points.Select(x => x.X).ToArray();
        double[] ys = points.Select(x => x.Y).ToArray();
        (double[] xs_res, double[] ys_res) = Interopolation.Cubic.InterpolateXY(xs, ys, quality).Round();
        
        PointCollection res = new();
        for (int i = 0; i < xs_res.Length; i++)
            res.Add(new(xs_res[i], ys_res[i]));

        return res;
    }

    /// <summary>
    /// Функция округления кортеж, состоящего из двух массивов типа double
    /// </summary>
    /// <param name="input">Кортеж</param>
    /// <param name="roundFactor">Кол-во знаков после запятой</param>
    /// <returns>Округлённый кортеж, состоящий из двух массивов типа double</returns>
    public static (double[], double[]) Round(this (double[], double[]) input, int roundFactor = 2) => (input.Item1.Select(x => Math.Round(x, roundFactor)).ToArray(), input.Item2.Select(x => Math.Round(x, roundFactor)).ToArray());

    /// <summary>
    /// Генерация кода для полигона
    /// </summary>
    /// <param name="poly"></param>
    /// <returns></returns>
    public static string GetCode(this Polygon poly)
    {
        if (poly.Points.Count > 0)
        {
            string result = string.Empty;
            SolidColorBrush brush = (SolidColorBrush)poly.Stroke;

            result += $"// Полигон {poly.Name}\n" +
                      $"Polygon {poly.Name} = new();\n" +
                      $"{poly.Name}.Stroke = new SolidColorBrush() " +
                       "{ Color = Color.FromRgb(" + brush.Color.R +
                       ", " + brush.Color.G +
                       ", " + brush.Color.B + ")};\n" +
                      $"{poly.Name}.StrokeThickness = {poly.StrokeThickness};" +
                       "\n\n";

            foreach (Point point in poly.Points)
                result += $"{poly.Name}.Points.Add(new Point({point.X.ToString().Replace(',', '.')},{point.Y.ToString().Replace(',', '.')}));\n";

            result += "\n\n";

            return result;
        }
        else
            return $"// {poly.Name} был пропущен - в полигоне нет точек\n\n";
    }

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

    /// <summary>
    /// Конвертирование Color? в Color. Если Color? == null, то возвращается Colors.White
    /// </summary>
    /// <param name="color"></param>
    /// <returns></returns>
    public static Color Safe(this Color? color) => color ?? Colors.White;

    /// <summary>
    /// Конвертация Color в SolidColorBrush
    /// </summary>
    /// <param name="color"></param>
    /// <returns></returns>
    public static SolidColorBrush ToBrush(this Color color) => new(color);

    public static SolidColorBrush ToBrush(this System.Drawing.Color color) => new() { Color = Color.FromArgb(color.A, color.R, color.G, color.B) };
}
