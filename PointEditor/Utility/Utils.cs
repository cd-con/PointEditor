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
}
