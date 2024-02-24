using System.Windows.Media;

namespace PointEditor.Utility
{
    internal static class ColorUtils
    {
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
    }
}
