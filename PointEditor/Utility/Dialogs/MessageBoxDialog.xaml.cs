using System.Windows;

namespace PointEditor
{
    /// <summary>
    /// Логика взаимодействия для MessageBoxDialog.xaml
    /// </summary>
    public partial class MessageBoxDialog : Window
    {
        public MessageBoxDialog(string caption = "Input", string message = "Enter text")
        {
            InitializeComponent();
            Title = caption;
            MessageBox.Text = message;
        }

        public string ResponseText
        {
            get { return ResponseTextBox.Text; }
            set { ResponseTextBox.Text = value; }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
