using System.Windows;
using Pack_Track.ViewModels;
using Pack_Track.Services;

namespace Pack_Track.Views
{
    public partial class ProductManagementWindow : Window
    {
        public ProductManagementWindow()
        {
            InitializeComponent();

            // Initialize with ViewModel
            var dataService = new JsonDataService();
            var viewModel = new ProductManagementViewModel(dataService);
            DataContext = viewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}