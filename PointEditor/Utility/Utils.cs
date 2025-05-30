namespace PointEditor.Utility
{
    public static class Utils
    {
        public static string MakeSafe(this string origin)
        {
            // TODO FIND BETTER WAY
            origin = origin.Trim().Replace(' ', '_').Replace('.', '_');

            if (char.IsDigit(origin[0]))
                origin = origin.Replace(origin[0], '_');

            return origin;
        }
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
