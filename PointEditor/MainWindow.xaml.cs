using Microsoft.Win32;
using PointEditor.Layouts;
using PointEditor.Utility;
using PointEditor.Utility.Actions;
using PointEditor.Utility.Actions.Objects;
using PointEditor.Utility.Actions.Objects.Generic;
using PointEditor.Utility.TreeViewStorage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit;

namespace PointEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        private const int APP_VERSION = 20;

        public static Canvas MainCanvas { get; private set; }
        public static MainWindow Instance { get; private set; }

        public static ToolsWindow ToolsInstance { get; private set; } = new();

        private List<Scene> openScenes = new();

        public MainWindow()
        {
            InitializeComponent();
            CheckUpdates();
            Instance = this;
        }

        public async void CheckUpdates()
        {

            using (HttpClient client = new()
            {
                BaseAddress = new Uri("https://raw.githubusercontent.com/cd-con/PointEditor/master/currentVersion.txt")
            })
            {
                try
                {
                    string s_version = await client.GetStringAsync(client.BaseAddress);
                    s_version = s_version.TrimEnd();
                    if (int.TryParse(s_version, out int version) && version > APP_VERSION)
                    {
                        if (System.Windows.MessageBox.Show("Вышла новая версия приложения!\nОбновить сейчас?", "Обновление", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            Process.Start("Updater.exe");
                            Application.Current.Shutdown();
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    System.Windows.MessageBox.Show(ex.Message);
                    // Kwuh
                }
            }
        }

        private void newScene_Click(object sender, RoutedEventArgs e)
        {
            Scene newScene = new();
            openScenes.Add(newScene);

            TabItem m = new()
            {
                Header = "Новая сцена",
                Tag = openScenes.IndexOf(newScene),
                Content = new Frame()
                {
                    Content = newScene.GetLayout()// Я хз почему так
                }
            };

            SceneContainer.Items.Add(m);
            SceneContainer.SelectedIndex = SceneContainer.Items.Count - 1;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ToolsInstance.Owner = this;
            ToolsInstance.Show();
        }

        private void SceneContainer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SceneContainer.SelectedItem != null)
                ToolsInstance.ReloadContext(openScenes[(int)(SceneContainer.SelectedItem as TabItem).Tag]);
            else
                ToolsInstance.UnloadContext();
        }

        private void OpenTools_Click(object sender, RoutedEventArgs e)
        {
            ToolsInstance.Show();
        }

        private void HeaderClose_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button)
            {
                TabItem tabItem = FindParent<TabItem>((Button)sender);
                if (tabItem != null)
                {
                    if (GetOpenScene().isModified)
                    {
                        MessageBoxResult result = System.Windows.MessageBox.Show("Сцена была изменена!\n\nСохранить изменения?", "Вы уверены?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                        switch (result)
                        {
                            case MessageBoxResult.Yes:
                                Export();
                                break;
                            case MessageBoxResult.No:
                                break;
                            default:
                                return;
                        }
                    }
                    openScenes.Remove(GetOpenScene());
                    SceneContainer.Items.Remove(tabItem);


                }
            }
        }

        private T FindParent<T>(DependencyObject obj) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(obj);
            if (parent == null) return null;
            T parentT = parent as T;
            return parentT ?? FindParent<T>(parent);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (SceneContainer.SelectedItem != null)
                Export();
        }

        private Scene GetOpenScene() => openScenes[(int)(SceneContainer.SelectedItem as TabItem).Tag];

        private void Export(bool bypassPathCheck = false)
        {
            if (!bypassPathCheck && GetOpenScene().GetPath() != null)
            {
                ExportSVG(GetOpenScene(), GetOpenScene().GetPath());
                return;
            }

            SaveFileDialog saveFileDialog = new()
            {
                Filter = "SVG file (*.svg)|*.svg",
                ValidateNames = true,
                FileName = "myDrawing.svg"
            };

            if (saveFileDialog.ShowDialog() == true)
                ExportSVG(GetOpenScene(), saveFileDialog.FileName);
        }

        private void ExportSVG(Scene scene, string filename)
        {
            string acc = $"<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
             "<!-- Сгенерировано в PointEditor -->\r\n" +
            $"<svg width=\"{scene.GetCanvas().RenderSize.Width}px\" height=\"{scene.GetCanvas().RenderSize.Height}px\" " +
            $"viewBox=\"0 0 {scene.GetCanvas().RenderSize.Width} {scene.GetCanvas().RenderSize.Height}\" class=\"icon\"  version=\"1.1\" xmlns=\"http://www.w3.org/2000/svg\">"; ;

            foreach (TreeViewShape shape in scene.T_treeRoot.FindChildrenOf<TreeViewShape>())
                if (shape.GetStoredValue() is Polygon)
                    acc += (shape.GetStoredValue() as Polygon).GetSVG();

            acc += "</svg>";

            File.WriteAllText(filename, acc);

            scene.isModified = false;

            ((TabItem)SceneContainer.SelectedItem).Header = scene.SetPath(filename);
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            if (openFileDialog.ShowDialog() == true)
            {
                if (SceneContainer.SelectedItem == null) {
                    newScene_Click(null, null);
                    ((TabItem)SceneContainer.SelectedItem).Header = System.IO.Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                }

                foreach (Shape shape in UtilsSVG.ParseSVG(openFileDialog.FileName))
                    ToolsInstance.AddShapeToDraw(shape, GetOpenScene().T_treeRoot);
            }
        }
    }
}
