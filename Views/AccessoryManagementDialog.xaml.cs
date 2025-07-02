using System.Windows;
using Pack_Track.Models;
using Pack_Track.ViewModels;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Pack_Track.Views
{
    public partial class AccessoryManagementDialog : Window
    {
        public AccessoryManagementDialog(Product product, List<Product> allProducts)
        {
            InitializeComponent();

            var viewModel = new AccessoryManagementViewModel(product, allProducts);
            DataContext = viewModel;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
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