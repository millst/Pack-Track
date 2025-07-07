// Views/ShowSetupWindow.xaml.cs - Add change notification
using System.Windows;
using Pack_Track.Models;
using Pack_Track.ViewModels;
using Pack_Track.Services;

namespace Pack_Track.Views
{
    public partial class ShowSetupWindow : Window
    {
        private readonly ShowSetupViewModel _viewModel;
        private readonly Show _originalShow;
        private readonly int _originalSceneCount;
        private readonly int _originalCastCount;
        private readonly int _originalRunCount;

        public bool ShowChanged { get; private set; }

        public ShowSetupWindow(Show show)
        {
            InitializeComponent();

            _originalShow = show;
            _originalSceneCount = show.Scenes.Count;
            _originalCastCount = show.Cast.Count;
            _originalRunCount = show.Runs.Count;

            var dataService = new JsonDataService();
            _viewModel = new ShowSetupViewModel(show, dataService);
            DataContext = _viewModel;

            // Subscribe to collection changes to detect modifications
            _viewModel.Scenes.CollectionChanged += (s, e) => ShowChanged = true;
            _viewModel.Cast.CollectionChanged += (s, e) => ShowChanged = true;
            _viewModel.Runs.CollectionChanged += (s, e) => ShowChanged = true;

            // Subscribe to property changes for show name/description
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.ShowName) ||
                    e.PropertyName == nameof(_viewModel.ShowDescription))
                {
                    ShowChanged = true;
                }
            };
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            // Check if there were any changes
            if (_originalSceneCount != _originalShow.Scenes.Count ||
                _originalCastCount != _originalShow.Cast.Count ||
                _originalRunCount != _originalShow.Runs.Count)
            {
                ShowChanged = true;
            }

            base.OnClosed(e);
        }
    }
}