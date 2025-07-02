using System.Windows;
using Pack_Track.Models;
using Pack_Track.ViewModels;
using Pack_Track.Services;

namespace Pack_Track.Views
{
    public partial class AllocationManagementDialog : Window
    {
        public AllocationManagementDialog(Show show, List<Product> products)
        {
            InitializeComponent();

            var dataService = new JsonDataService();
            var viewModel = new AllocationManagementViewModel(show, products, dataService);
            DataContext = viewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class AllocationDisplayItem
    {
        public Guid AllocationId { get; set; }
        public string ActorName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string AssetInfo { get; set; } = string.Empty;
        public int FromScene { get; set; }
        public int ToScene { get; set; }
        public string SceneRange { get; set; } = string.Empty;
        public string DisplayText => string.IsNullOrEmpty(AssetInfo)
            ? ProductName
            : $"{ProductName} ({AssetInfo})";
    }
}