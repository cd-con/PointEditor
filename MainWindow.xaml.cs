using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PointEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        public List<Polygon> polygons = new List<Polygon>();
        public Polygon selectedPolygon;
        public Color PenColor = new();
        public SolidColorBrush StrokeBrush = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        private void ColorPicker_HEX_TextChanged(object sender, TextChangedEventArgs e)
        {

            Match regexMatch = Regex.Match(ColorPicker_HEX.Text, @"([0-9A-Fa-f]{6})");

            if (regexMatch.Success)
            {
                byte[] hexColors = StringToByteArray(regexMatch.Value);

                PenColor.R = hexColors[0];
                R.Value = Convert.ToInt32(PenColor.R);

                PenColor.G = hexColors[1];
                G.Value = Convert.ToInt32(PenColor.G);

                PenColor.B = hexColors[2];
                B.Value = Convert.ToInt32(PenColor.B);

                PenColor.A = 0xFF;
                Preview.Fill = new SolidColorBrush(PenColor);

                StrokeBrush.Color = PenColor;
            }
        }

        private void Slider_ValueChanged(object sender, object _)
        {
            Slider slider = sender as Slider;
            switch (slider.Name)
            {
                case "R":
                    PenColor.R = Convert.ToByte(R.Value); break;
                case "G":
                    PenColor.G = Convert.ToByte(G.Value); break;
                case "B":
                    PenColor.B = Convert.ToByte(B.Value); break;
                default:
                    throw new NotImplementedException();
            }
            ColorPicker_HEX.Text = $"#{ByteArrayToString(new byte[] { PenColor.R, PenColor.G, PenColor.B })}";
        }


        // Add new polygon
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Polygon newPolygon = new Polygon { Stroke = StrokeBrush, StrokeThickness = 3, Name = $"newPolygon{polygons.Count}" };
            polygons.Add(newPolygon);
            PolygonList.Items.Add(newPolygon.Name);
            PolygonList.SelectedItem = newPolygon.Name;
            mainCanvas.Children.Add(newPolygon);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (selectedPolygon != null)
            {
                mainCanvas.Children.Remove(selectedPolygon);
                polygons.Remove(selectedPolygon);
                PolygonList.Items.Remove(selectedPolygon.Name);
                selectedPolygon = null;
            }
        }

        private void mainCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed && selectedPolygon != null)
                selectedPolygon.Points.Add(e.GetPosition(this));
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            output.Clear();
            output.Text += "// Скопируй эти значения в свой код\n";
            foreach (Polygon poly in polygons)
            {
                foreach (Point point in poly.Points)
                {
                    output.Text += $"{poly.Name}.Points.Add(new Point({point.X},{point.Y}));\n";
                }
                output.Text += "\n\n";
            }
        }

        private void Scale_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxDialog dialog = new MessageBoxDialog("Изменить размер", "Фактор изменения размера");
            if (dialog.ShowDialog() == true)
            {
                if (double.TryParse(dialog.ResponseText, out double rescale_factor))
                {
                    if (PolygonList.SelectedItem != null)
                        Utility.ResizePolygon(selectedPolygon.Points, rescale_factor);return;
                    foreach (Polygon poly in polygons)
                    {
                        Utility.ResizePolygon(poly.Points, rescale_factor);
                    };
                }
            }
        }

        private void PolygonList_Selected(object sender, RoutedEventArgs e)
        {
            try
            {
                if (PolygonList.SelectedItem != null)
                {
                    selectedPolygon = polygons.Where(x => x.Name == PolygonList.SelectedItem).Single();
                }
            }
            catch (InvalidOperationException err)
            {
                // Просто да
            }
        }

        private void PolygonList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Polygon selected = polygons.Where(x => x.Name == PolygonList.SelectedItem).Single();
            MessageBoxDialog dialog = new MessageBoxDialog("Переименовать", $"{selected.Name} будет переименован в");
            if (dialog.ShowDialog() == true && dialog.ResponseText.Count() > 0)
            {
                string result = dialog.ResponseText.Replace(' ', '_');
                PolygonList.Items[PolygonList.Items.IndexOf(selected.Name)] = result;
                PolygonList.SelectedItem = result;
                selected.Name = result;
            }
        }

        private void ClearPolygonListSelection(object sender, ExecutedRoutedEventArgs e)
        {
            PolygonList.SelectedIndex = -1;
        }

        private void Smooth_Click(object sender, RoutedEventArgs e)
        {
            // Oh boy, here we go
            MessageBoxDialog dialog = new MessageBoxDialog("Смягчить", "Введите значение (больше - дольше)");
            if (dialog.ShowDialog() == true && int.TryParse(dialog.ResponseText, out int smooth_factor))
            {
                if (PolygonList.SelectedItem != null)
                {
                    Utility.SmootherPolygon(selectedPolygon, smooth_factor); 
                    return;
                }
                foreach (Polygon poly in polygons)
                {
                    Utility.SmootherPolygon(selectedPolygon, smooth_factor);
                }
            }
        }

        private void Move_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxDialog dialog = new MessageBoxDialog("Смягчить", "Введите значение (больше - дольше)");
            if (dialog.ShowDialog() == true)
            {
                string[] coords = dialog.ResponseText.Split(';');
                if (int.TryParse(coords[0], out int x) && int.TryParse(coords[1], out int y))
                {
                    if (PolygonList.SelectedItem != null)
                    {
                        Utility.MovePolygon(selectedPolygon.Points, x, y);
                        return;
                    }
                    foreach (Polygon poly in polygons)
                    {
                        Utility.MovePolygon(poly.Points, x, y);
                    }
                }
            }
        }
    }

    public static class Utility
    {
        private static Point Sum(this Point point, Point newPoint) => new Point(point.X + newPoint.X, point.Y + newPoint.Y);
        public static Point Scale(this Point point, double factor) => new Point(point.X * factor, point.Y * factor);
        public static double ToRadians(this double angle) => ((Math.PI / 180) * angle);

        static public double Linear(double x, double x0, double x1, double y0, double y1)
        {
            if ((x1 - x0) == 0)
            {
                return (y0 + y1) / 2;
            }
            return y0 + (x - x0) * (y1 - y0) / (x1 - x0);
        }

        public static void ResizePolygon(PointCollection points, double factor)
        {
            for (int pointID = 0; pointID < points.Count; pointID++)
                points[pointID] = points[pointID].Scale(factor);
        }

        public static void MovePolygon(PointCollection points, int x, int y)
        {
            for (int pointID = 0; pointID < points.Count; pointID++)
                points[pointID] = points[pointID].Sum(new(x, y));
        }

        public static void SmootherPolygon(Polygon poly, int iter_count)
        {
            double[] xs = poly.Points.Select(x => x.X).ToArray();
            double[] ys = poly.Points.Select(x => x.Y).ToArray();
            (double[] xs_res, double[] ys_res) = Cubic.InterpolateXY(xs, ys, iter_count);
            poly.Points.Clear();
            for (int i = 0; i < xs_res.Length; i++)
            {
                poly.Points.Add(new(xs_res[i], ys_res[i]));
            }
        }

    }

    public static class Cubic
    {
        /// <summary>
        /// Generate a smooth (interpolated) curve that follows the path of the given X/Y points
        /// </summary>
        public static (double[] xs, double[] ys) InterpolateXY(double[] xs, double[] ys, int count)
        {
            if (xs is null || ys is null || xs.Length != ys.Length)
                throw new ArgumentException($"{nameof(xs)} and {nameof(ys)} must have same length");

            int inputPointCount = xs.Length;
            double[] inputDistances = new double[inputPointCount];
            for (int i = 1; i < inputPointCount; i++)
            {
                double dx = xs[i] - xs[i - 1];
                double dy = ys[i] - ys[i - 1];
                double distance = Math.Sqrt(dx * dx + dy * dy);
                inputDistances[i] = inputDistances[i - 1] + distance;
            }

            double meanDistance = inputDistances.Last() / (count - 1);
            double[] evenDistances = Enumerable.Range(0, count).Select(x => x * meanDistance).ToArray();
            double[] xsOut = Interpolate(inputDistances, xs, evenDistances);
            double[] ysOut = Interpolate(inputDistances, ys, evenDistances);
            return (xsOut, ysOut);
        }

        private static double[] Interpolate(double[] xOrig, double[] yOrig, double[] xInterp)
        {
            (double[] a, double[] b) = FitMatrix(xOrig, yOrig);

            double[] yInterp = new double[xInterp.Length];
            for (int i = 0; i < yInterp.Length; i++)
            {
                int j;
                for (j = 0; j < xOrig.Length - 2; j++)
                    if (xInterp[i] <= xOrig[j + 1])
                        break;

                double dx = xOrig[j + 1] - xOrig[j];
                double t = (xInterp[i] - xOrig[j]) / dx;
                double y = (1 - t) * yOrig[j] + t * yOrig[j + 1] +
                    t * (1 - t) * (a[j] * (1 - t) + b[j] * t);
                yInterp[i] = y;
            }

            return yInterp;
        }

        private static (double[] a, double[] b) FitMatrix(double[] x, double[] y)
        {
            int n = x.Length;
            double[] a = new double[n - 1];
            double[] b = new double[n - 1];
            double[] r = new double[n];
            double[] A = new double[n];
            double[] B = new double[n];
            double[] C = new double[n];

            double dx1, dx2, dy1, dy2;

            dx1 = x[1] - x[0];
            C[0] = 1.0f / dx1;
            B[0] = 2.0f * C[0];
            r[0] = 3 * (y[1] - y[0]) / (dx1 * dx1);

            for (int i = 1; i < n - 1; i++)
            {
                dx1 = x[i] - x[i - 1];
                dx2 = x[i + 1] - x[i];
                A[i] = 1.0f / dx1;
                C[i] = 1.0f / dx2;
                B[i] = 2.0f * (A[i] + C[i]);
                dy1 = y[i] - y[i - 1];
                dy2 = y[i + 1] - y[i];
                r[i] = 3 * (dy1 / (dx1 * dx1) + dy2 / (dx2 * dx2));
            }

            dx1 = x[n - 1] - x[n - 2];
            dy1 = y[n - 1] - y[n - 2];
            A[n - 1] = 1.0f / dx1;
            B[n - 1] = 2.0f * A[n - 1];
            r[n - 1] = 3 * (dy1 / (dx1 * dx1));

            double[] cPrime = new double[n];
            cPrime[0] = C[0] / B[0];
            for (int i = 1; i < n; i++)
                cPrime[i] = C[i] / (B[i] - cPrime[i - 1] * A[i]);

            double[] dPrime = new double[n];
            dPrime[0] = r[0] / B[0];
            for (int i = 1; i < n; i++)
                dPrime[i] = (r[i] - dPrime[i - 1] * A[i]) / (B[i] - cPrime[i - 1] * A[i]);

            double[] k = new double[n];
            k[n - 1] = dPrime[n - 1];
            for (int i = n - 2; i >= 0; i--)
                k[i] = dPrime[i] - cPrime[i] * k[i + 1];

            for (int i = 1; i < n; i++)
            {
                dx1 = x[i] - x[i - 1];
                dy1 = y[i] - y[i - 1];
                a[i - 1] = k[i - 1] * dx1 - dy1;
                b[i - 1] = -k[i] * dx1 + dy1;
            }

            return (a, b);
        }
    }
}