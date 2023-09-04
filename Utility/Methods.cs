using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace PointEditor.Utility
{
    /// <summary>
    /// Вспомогательные методы
    /// </summary>
    static class Methods
    {
        /// <summary>
        /// Найти сумму двух точек.
        /// </summary>
        /// <param name="point">Исходная точка</param>
        /// <param name="newPoint">Смещение</param>
        /// <returns>Смещённая точка</returns>
        private static Point Sum(this Point point, Point newPoint) => new Point(point.X + newPoint.X, point.Y + newPoint.Y);
        
        /// <summary>
        /// Умножает точку на значение.
        /// </summary>
        /// <param name="point">Исходная точка</param>
        /// <param name="factor">Множитель</param>
        /// <returns>Смещённая точка</returns>
        public static Point Scale(this Point point, double factor) => new Point(point.X * factor, point.Y * factor);

        /// <summary>
        /// Делит точку на значение.
        /// </summary>
        /// <param name="point">Исходная точка</param>
        /// <param name="factor">Множитель</param>
        /// <returns>Смещённая точка</returns>
        public static Point Shrink(this Point point, double factor) => new Point(point.X / factor, point.Y / factor);

        public static Point GetCenter(PointCollection collection) => collection.Aggregate(new Point(0, 0), (current, point) => current.Sum(point)).Shrink(collection.Count);

        /// <summary>
        /// Конвертирует градусы в радианы.
        /// </summary>
        /// <param name="angle">Исходный угол</param>
        /// <returns>Угол в радианах</returns>
        public static double ToRadians(this double angle) => ((Math.PI / 180) * angle);

        /// <summary>
        /// Превращает безопасным способом bool? в bool.
        /// Если bool? == null, возвращает false
        /// </summary>
        /// <param name="value">Исходное значение</param>
        /// <returns>Исходное значение bool? конвертированное в bool</returns>
        public static bool NotNullBool(this bool? value) => value == null ? false : (bool)value;

        public static T[] Shift<T>(this T[] arr, T newObject)
        {
            for (int i = 1; i < arr.Length; i++)
            {
                arr[i - 1] = arr[i];
            }

            arr[arr.Length - 1] = newObject;

            return arr;
        }

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
                points[pointID] = points[pointID].Sum(offset);
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
            (double[] xs_res, double[] ys_res) = Interopolation.Cubic.InterpolateXY(xs, ys, quality);
            
            PointCollection res = new PointCollection();
            for (int i = 0; i < xs_res.Length; i++)
            {
                res.Add(new(xs_res[i], ys_res[i]));
            }

            return res;
        }

        /// <summary>
        /// Конвертирование строки в массив байт.
        /// </summary>
        /// <param name="hex">Строка, содержащая число в шестнадцатиричной системе счисления</param>
        /// <returns>Массив байтов, представляющих исходное число</returns>
        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        /// <summary>
        /// Конвертирование массива байт в строку.
        /// </summary>
        /// <param name="bytes">Массив байт, представляющих число в шестнадцатиричной системе счисления</param>
        /// <returns>Строка, содержащая исходное число</returns>
        public static string ByteArrayToString(byte[] bytes)
        {
            StringBuilder hex = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

    }
}
