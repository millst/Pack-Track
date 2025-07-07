// ViewModels/MainViewModel.cs - Enhanced Tracking Only
using Pack_Track.Models;
using Pack_Track.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;
using Microsoft.Win32;
using System.IO;

namespace Pack_Track.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;
        private Show? _currentShow;
        private string _currentShowPath = string.Empty;
        private string _statusMessage = "Ready";
        private SceneBySceneLiveOperationsViewModel? _enhancedLiveOperationsViewModel;
        private Scene? _selectedSceneFromButtons;

        public MainViewModel(IDataService dataService)
        {
            _dataService = dataService;

            // Initialize commands
            NewShowCommand = new RelayCommand(NewShow);
            LoadShowCommand = new RelayCommand(LoadShow);
            SaveShowCommand = new RelayCommand(SaveShow, () => CurrentShow != null);
            SaveShowAsCommand = new RelayCommand(SaveShowAs, () => CurrentShow != null);
            ManageProductsCommand = new RelayCommand(ManageProducts);
            SetupShowCommand = new RelayCommand(SetupShow);
            JumpToSceneCommand = new RelayCommand<Scene>(JumpToScene);

            SceneButtonCommands = new ObservableCollection<SceneButtonViewModel>();

            // Try to load the last show, or create a new one
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                var lastShowPath = await _dataService.GetLastShowPathAsync();

                if (!string.IsNullOrEmpty(lastShowPath) && File.Exists(lastShowPath))
                {
                    var show = await _dataService.LoadShowAsync(lastShowPath);
                    if (show != null)
                    {
                        CurrentShow = show;
                        _currentShowPath = lastShowPath;
                        StatusMessage = $"Loaded last show: {show.Name}";
                        OnPropertyChanged(nameof(WindowTitle));
                        return;
                    }
                }

                // Fallback to new show if no last show or loading failed
                NewShow();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading last show: {ex.Message}";
                NewShow();
            }
        }

        public Show? CurrentShow
        {
            get => _currentShow;
            set
            {
                if (SetProperty(ref _currentShow, value))
                {
                    UpdateLiveOperations();
                    UpdateSceneButtons();
                }
            }
        }

        public SceneBySceneLiveOperationsViewModel? EnhancedLiveOperationsViewModel
        {
            get => _enhancedLiveOperationsViewModel;
            private set => SetProperty(ref _enhancedLiveOperationsViewModel, value);
        }

        public ObservableCollection<SceneButtonViewModel> SceneButtonCommands { get; }

        public Scene? SelectedSceneFromButtons
        {
            get => _selectedSceneFromButtons;
            set => SetProperty(ref _selectedSceneFromButtons, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string WindowTitle => string.IsNullOrEmpty(_currentShowPath)
            ? "Pack Track - New Show"
            : $"Pack Track - {Path.GetFileNameWithoutExtension(_currentShowPath)}";

        // Commands
        public ICommand NewShowCommand { get; }
        public ICommand LoadShowCommand { get; }
        public ICommand SaveShowCommand { get; }
        public ICommand SaveShowAsCommand { get; }
        public ICommand ManageProductsCommand { get; }
        public ICommand SetupShowCommand { get; }
        public ICommand JumpToSceneCommand { get; }

        private void NewShow()
        {
            CurrentShow = new Show { Name = "New Show" };
            _currentShowPath = string.Empty;
            StatusMessage = "New show created";
            OnPropertyChanged(nameof(WindowTitle));
        }

        private async void UpdateLiveOperations()
        {
            if (CurrentShow != null && CurrentShow.Cast.Any() && CurrentShow.Scenes.Any())
            {
                try
                {
                    var products = await _dataService.LoadProductsAsync();
                    EnhancedLiveOperationsViewModel = new SceneBySceneLiveOperationsViewModel(CurrentShow, products, _dataService);
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error loading live operations: {ex.Message}";
                    EnhancedLiveOperationsViewModel = null;
                }
            }
            else
            {
                EnhancedLiveOperationsViewModel = null;
            }
        }

        private void UpdateSceneButtons()
        {
            SceneButtonCommands.Clear();

            if (CurrentShow?.Scenes != null)
            {
                foreach (var scene in CurrentShow.Scenes.OrderBy(s => s.SceneNumber))
                {
                    SceneButtonCommands.Add(new SceneButtonViewModel(scene));
                }
            }
        }

        private void JumpToScene(Scene? scene)
        {
            if (scene != null)
            {
                SelectedSceneFromButtons = scene;
                StatusMessage = $"Jumped to {scene.Name}";

                // Trigger a property change to notify the UI to scroll to this scene
                OnPropertyChanged(nameof(SelectedSceneFromButtons));
            }
        }

        private async void LoadShow()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Pack Track Show files (*.pts)|*.pts|All files (*.*)|*.*",
                DefaultExt = "pts"
            };

            if (dialog.ShowDialog() == true)
            {
                var show = await _dataService.LoadShowAsync(dialog.FileName);
                if (show != null)
                {
                    CurrentShow = show;
                    _currentShowPath = dialog.FileName;
                    StatusMessage = $"Loaded show: {show.Name}";
                    OnPropertyChanged(nameof(WindowTitle));
                }
                else
                {
                    StatusMessage = "Failed to load show file";
                }
            }
        }

        private async void SaveShow()
        {
            if (CurrentShow == null) return;

            if (string.IsNullOrEmpty(_currentShowPath))
            {
                SaveShowAs();
                return;
            }

            await _dataService.SaveShowAsync(CurrentShow, _currentShowPath);
            StatusMessage = $"Saved show: {CurrentShow.Name}";
        }

        private async void SaveShowAs()
        {
            if (CurrentShow == null) return;

            var dialog = new SaveFileDialog
            {
                Filter = "Pack Track Show files (*.pts)|*.pts|All files (*.*)|*.*",
                DefaultExt = "pts",
                FileName = CurrentShow.Name
            };

            if (dialog.ShowDialog() == true)
            {
                await _dataService.SaveShowAsync(CurrentShow, dialog.FileName);
                _currentShowPath = dialog.FileName;
                StatusMessage = $"Saved show: {CurrentShow.Name}";
                OnPropertyChanged(nameof(WindowTitle));
            }
        }

        private void ManageProducts()
        {
            var productWindow = new Pack_Track.Views.ProductManagementWindow();
            productWindow.Owner = Application.Current.MainWindow;
            productWindow.ShowDialog();
            StatusMessage = "Product management opened";
        }

        private void SetupShow()
        {
            if (CurrentShow == null)
            {
                CurrentShow = new Show { Name = "New Show" };
                _currentShowPath = string.Empty;
                OnPropertyChanged(nameof(WindowTitle));
            }

            var setupWindow = new Pack_Track.Views.ShowSetupWindow(CurrentShow);
            setupWindow.Owner = Application.Current.MainWindow;
            setupWindow.ShowDialog();

            // Refresh UI if show was changed
            if (setupWindow.ShowChanged)
            {
                OnPropertyChanged(nameof(CurrentShow));
                UpdateLiveOperations();
                UpdateSceneButtons();
                StatusMessage = "Show setup completed - data refreshed";
            }
        }
    }

    public class SceneButtonViewModel : BaseViewModel
    {
        public SceneButtonViewModel(Scene scene)
        {
            Scene = scene;
        }

        public Scene Scene { get; }
        public string ButtonText => $"Scene {Scene.SceneNumber}";
        public string ToolTip => Scene.Name;
    }
}