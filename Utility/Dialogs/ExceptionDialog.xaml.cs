using System.Media;
using System.Windows;

namespace PointEditor.Utility.Dialogs
{
    /// <summary>
    /// Логика взаимодействия для ExceptionWindow.xaml
    /// </summary>
    public partial class ExceptionDialog : Window
    {
        public bool isCancelled = true;
        public ExceptionDialog(string caption = "Exception", string message = "An exception occurred while trying to perform this operation.")
        {
            InitializeComponent();
            SystemSounds.Beep.Play();
            Title = caption;
            MessageBox.Text = message;
        }

        public bool BypassDialog
        {
            get { return Bypass.IsChecked.NotNullBool(); }
            set { Bypass.IsChecked = value; }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            isCancelled = false;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

    }
}

