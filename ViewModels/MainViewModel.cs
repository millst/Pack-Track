// ViewModels/MainViewModel.cs
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
        private LiveOperationsViewModel? _liveOperationsViewModel;

        public MainViewModel(IDataService dataService)
        {
            _dataService = dataService;

            // Initialize commands
            NewShowCommand = new RelayCommand(NewShow);
            LoadShowCommand = new RelayCommand(LoadShow);
            SaveShowCommand = new RelayCommand(SaveShow, () => CurrentShow != null);
            SaveShowAsCommand = new RelayCommand(SaveShowAs, () => CurrentShow != null);
            NextSceneCommand = new RelayCommand(NextScene, CanNextScene);
            PreviousSceneCommand = new RelayCommand(PreviousScene, CanPreviousScene);
            ManageProductsCommand = new RelayCommand(ManageProducts);
            SetupShowCommand = new RelayCommand(SetupShow);

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
                }
            }
        }

        public LiveOperationsViewModel? LiveOperationsViewModel
        {
            get => _liveOperationsViewModel;
            private set => SetProperty(ref _liveOperationsViewModel, value);
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
        public ICommand NextSceneCommand { get; }
        public ICommand PreviousSceneCommand { get; }
        public ICommand ManageProductsCommand { get; }
        public ICommand SetupShowCommand { get; }

        private void NewShow()
        {
            CurrentShow = new Show { Name = "New Show" };
            _currentShowPath = string.Empty;
            StatusMessage = "New show created";
            OnPropertyChanged(nameof(WindowTitle));
            UpdateLiveOperations();
        }

        private void UpdateLiveOperations()
        {
            if (CurrentShow != null && CurrentShow.Cast.Any() && CurrentShow.Scenes.Any())
            {
                LiveOperationsViewModel = new LiveOperationsViewModel(CurrentShow, _dataService);
            }
            else
            {
                LiveOperationsViewModel = null;
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

        private void NextScene()
        {
            if (CurrentShow != null && CurrentShow.CurrentSceneIndex < CurrentShow.Scenes.Count - 1)
            {
                CurrentShow.CurrentSceneIndex++;
                OnPropertyChanged(nameof(CurrentShow));
                UpdateLiveOperationsScene();
                StatusMessage = $"Advanced to scene {CurrentShow.CurrentSceneIndex + 1}: {CurrentShow.CurrentScene?.Name}";
            }
        }

        private bool CanNextScene()
        {
            return CurrentShow != null && CurrentShow.CurrentSceneIndex < CurrentShow.Scenes.Count - 1;
        }

        private void PreviousScene()
        {
            if (CurrentShow != null && CurrentShow.CurrentSceneIndex > 0)
            {
                CurrentShow.CurrentSceneIndex--;
                OnPropertyChanged(nameof(CurrentShow));
                UpdateLiveOperationsScene();
                StatusMessage = $"Moved to scene {CurrentShow.CurrentSceneIndex + 1}: {CurrentShow.CurrentScene?.Name}";
            }
        }

        private bool CanPreviousScene()
        {
            return CurrentShow != null && CurrentShow.CurrentSceneIndex > 0;
        }

        private void UpdateLiveOperationsScene()
        {
            if (LiveOperationsViewModel != null && CurrentShow != null)
            {
                LiveOperationsViewModel.CurrentScene = CurrentShow.CurrentScene;
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
                StatusMessage = "Show setup completed - data refreshed";
            }
        }
    }
}