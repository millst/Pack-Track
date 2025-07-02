using System.Windows;
using Pack_Track.ViewModels;
using Pack_Track.Services;

namespace Pack_Track
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Initialize the data service and view model
            var dataService = new JsonDataService();
            var viewModel = new MainViewModel(dataService);

            DataContext = viewModel;
        }
    }
}