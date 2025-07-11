// Views/AccessoryManagementDialog.xaml.cs - Updated to pass data service
using System.Windows;
using Pack_Track.Models;
using Pack_Track.ViewModels;
using Pack_Track.Services;

namespace Pack_Track.Views
{
    public partial class AccessoryManagementDialog : Window
    {
        private readonly AccessoryManagementViewModel _viewModel;

        public AccessoryManagementDialog(Product product, List<Product> allProducts, IDataService dataService)
        {
            InitializeComponent();

            _viewModel = new AccessoryManagementViewModel(product, allProducts, dataService);
            DataContext = _viewModel;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Save changes when OK is clicked
            _viewModel.SaveCommand.Execute(null);
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}