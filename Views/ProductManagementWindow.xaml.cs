// Views/ProductManagementWindow.xaml.cs - Fixed Constructor
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

            // Initialize the ViewModel with DataService
            var dataService = new JsonDataService();
            var viewModel = new ProductManagementViewModel(dataService);
            DataContext = viewModel;
        }
    }
}