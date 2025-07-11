// Views/ProductManagementWindow.xaml.cs - Enhanced Version
using System.Windows;
using Pack_Track.ViewModels;
using Pack_Track.Services;

namespace Pack_Track.Views
{
    public partial class ProductManagementWindow : Window
    {
        public ProductManagementWindow()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== ProductManagementWindow Constructor START ===");

                InitializeComponent();
                System.Diagnostics.Debug.WriteLine("InitializeComponent completed");

                // Initialize the ViewModel with DataService
                var dataService = new JsonDataService();
                var viewModel = new ProductManagementViewModel(dataService);
                DataContext = viewModel;

                System.Diagnostics.Debug.WriteLine("=== ProductManagementWindow Constructor END ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== ProductManagementWindow Constructor ERROR: {ex.Message} ===");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}