using System.Windows;
using System.Windows.Controls;
using TempsAnalyzer.ViewModels;

namespace TempsAnalyzer.Views
{
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel => (MainViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Charger_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LoadFromFile();
        }

    }
}
