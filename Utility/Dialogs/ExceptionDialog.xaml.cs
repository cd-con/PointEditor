using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PointEditor.Utility.Dialogs
{
    /// <summary>
    /// Логика взаимодействия для ExceptionWindow.xaml
    /// </summary>
    public partial class ExceptionDialog : Window
    {
        public bool isCancelled = true;
        public ExceptionDialog(string caption = "Exception", string message = "An exception occured while trying to perform an operation.")
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
