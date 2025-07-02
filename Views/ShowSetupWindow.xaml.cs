// Views/ShowSetupWindow.xaml.cs
using System.Windows;
using Pack_Track.ViewModels;
using Pack_Track.Models;
using Pack_Track.Services;

namespace Pack_Track.Views
{
    public partial class ShowSetupWindow : Window
    {
        public Show Show { get; private set; }
        public bool ShowChanged { get; private set; }

        public ShowSetupWindow(Show show)
        {
            InitializeComponent();
            Show = show;

            var dataService = new JsonDataService();
            var viewModel = new ShowSetupViewModel(show, dataService);

            // Subscribe to save event
            viewModel.ShowSaved += () => ShowChanged = true;

            DataContext = viewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Mark as changed so main window can refresh
            ShowChanged = true;
            Close();
        }
    }
}





