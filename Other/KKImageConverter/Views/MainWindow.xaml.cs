using System.Windows;

namespace KKImageConverter.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
        }

        private void About_OnClick(object sender, RoutedEventArgs e)
        {
            new About().ShowDialog();
        }
    }
}
