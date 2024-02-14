using System.Windows.Media;

namespace PointEditor.Utility
{
    internal static class ColorUtils
    {
        public static Color Safe(this Color? color) => color ?? Colors.White;

        public static SolidColorBrush ToBrush(this Color color) => new(color);
    }
}
