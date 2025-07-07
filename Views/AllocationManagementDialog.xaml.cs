// Views/AllocationManagementWindow.xaml.cs - Updated constructor
using System.Windows;
using Pack_Track.Models;
using Pack_Track.ViewModels;
using Pack_Track.Services;

namespace Pack_Track.Views
{
    public partial class AllocationManagementWindow : Window
    {
        public AllocationManagementWindow(Show show, List<Product> products)
        {
            InitializeComponent();

            var dataService = new JsonDataService();
            var viewModel = new AllocationManagementViewModel(show, products, dataService);
            DataContext = viewModel;
        }
    }
}