// ViewModels/MainViewModel.cs - Enhanced with run isolation and fixes
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
        private Run? _selectedRun;
        private RunSummaryViewModel? _currentRunSummary;
        private AllRunsSummaryViewModel? _allRunsSummary;
        private bool _allRunsSummaryExpanded = false;

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
            ToggleAllRunsSummaryCommand = new RelayCommand(ToggleAllRunsSummary);

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
                    System.Diagnostics.Debug.WriteLine($"=== CurrentShow changed to: {value?.Name} ===");
                    UpdateLiveOperations();
                    UpdateSceneButtons();
                    UpdateRunSummaries();

                    // Select first run by default
                    if (value?.Runs?.Any() == true && SelectedRun == null)
                    {
                        SelectedRun = value.Runs.OrderBy(r => r.DateTime).First();
                    }
                }
            }
        }

        public Run? SelectedRun
        {
            get => _selectedRun;
            set
            {
                if (SetProperty(ref _selectedRun, value))
                {
                    System.Diagnostics.Debug.WriteLine($"=== SelectedRun changed to: {value?.Name} ===");

                    // Reset all equipment status when switching runs
                    ResetEquipmentForRun();

                    UpdateRunSummaries();
                    RefreshLiveOperations();
                }
            }
        }

        private void ResetEquipmentForRun()
        {
            if (CurrentShow == null) return;

            System.Diagnostics.Debug.WriteLine("=== Resetting equipment for new run ===");

            // Reset all asset statuses to available when switching runs
            foreach (var assetStatus in CurrentShow.AssetStatuses)
            {
                if (assetStatus.Status == EquipmentStatus.CheckedOut)
                {
                    assetStatus.Status = EquipmentStatus.Available;
                    assetStatus.CurrentlyAssignedToActorId = null;
                    assetStatus.CurrentSceneId = null;
                    assetStatus.LastStatusChange = DateTime.Now;
                    System.Diagnostics.Debug.WriteLine($"Reset asset: {assetStatus.AssetNumber}");
                }
            }
        }

        private void RefreshLiveOperations()
        {
            if (EnhancedLiveOperationsViewModel != null)
            {
                EnhancedLiveOperationsViewModel.RefreshAllAssetButtons();
                EnhancedLiveOperationsViewModel.RefreshCounts();
            }
        }

        public RunSummaryViewModel? CurrentRunSummary
        {
            get => _currentRunSummary;
            private set => SetProperty(ref _currentRunSummary, value);
        }

        public AllRunsSummaryViewModel? AllRunsSummary
        {
            get => _allRunsSummary;
            private set => SetProperty(ref _allRunsSummary, value);
        }

        public bool AllRunsSummaryExpanded
        {
            get => _allRunsSummaryExpanded;
            set => SetProperty(ref _allRunsSummaryExpanded, value);
        }

        public SceneBySceneLiveOperationsViewModel? EnhancedLiveOperationsViewModel
        {
            get => _enhancedLiveOperationsViewModel;
            private set
            {
                if (SetProperty(ref _enhancedLiveOperationsViewModel, value))
                {
                    // Subscribe to equipment changes to update run summaries
                    if (value != null && CurrentShow != null)
                    {
                        // Monitor asset status changes for run tracking
                        foreach (var assetStatus in CurrentShow.AssetStatuses)
                        {
                            assetStatus.PropertyChanged += OnAssetStatusChanged;
                        }
                    }
                }
            }
        }

        private void OnAssetStatusChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AssetStatus.Status))
            {
                // Refresh run summaries when any asset status changes
                System.Diagnostics.Debug.WriteLine("Asset status changed, refreshing run summaries");
                UpdateRunSummaries();
            }
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
        public ICommand ToggleAllRunsSummaryCommand { get; }

        private void NewShow()
        {
            CurrentShow = new Show { Name = "New Show" };
            _currentShowPath = string.Empty;
            StatusMessage = "New show created";
            OnPropertyChanged(nameof(WindowTitle));
        }

        private void ToggleAllRunsSummary()
        {
            AllRunsSummaryExpanded = !AllRunsSummaryExpanded;
        }

        private async void UpdateLiveOperations()
        {
            System.Diagnostics.Debug.WriteLine($"=== UpdateLiveOperations START ===");

            if (CurrentShow == null)
            {
                System.Diagnostics.Debug.WriteLine("CurrentShow is null");
                EnhancedLiveOperationsViewModel = null;
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Show: {CurrentShow.Name}");
            System.Diagnostics.Debug.WriteLine($"Cast count: {CurrentShow.Cast?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine($"Scenes count: {CurrentShow.Scenes?.Count ?? 0}");

            if (CurrentShow.Cast.Any() && CurrentShow.Scenes.Any())
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("Loading products...");
                    var products = await _dataService.LoadProductsAsync();
                    System.Diagnostics.Debug.WriteLine($"Loaded {products.Count} products");

                    // Check for allocations
                    var totalAllocations = CurrentShow.Scenes.Sum(s => s.Allocations?.Count ?? 0);
                    System.Diagnostics.Debug.WriteLine($"Total allocations across all scenes: {totalAllocations}");

                    if (totalAllocations == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("No allocations found - user needs to set up equipment allocations");
                        StatusMessage = "No equipment allocations found. Use Setup Show > Manage Allocations to assign equipment to actors.";
                    }

                    EnhancedLiveOperationsViewModel = new SceneBySceneLiveOperationsViewModel(CurrentShow, products, _dataService);
                    System.Diagnostics.Debug.WriteLine("Enhanced live operations view model created successfully");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading live operations: {ex.Message}");
                    StatusMessage = $"Error loading live operations: {ex.Message}";
                    EnhancedLiveOperationsViewModel = null;
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Show doesn't have both cast and scenes");
                StatusMessage = "Add cast members and scenes to enable live operations";
                EnhancedLiveOperationsViewModel = null;
            }

            System.Diagnostics.Debug.WriteLine($"=== UpdateLiveOperations END ===");
        }

        private async void UpdateRunSummaries()
        {
            System.Diagnostics.Debug.WriteLine($"=== UpdateRunSummaries START ===");

            if (CurrentShow == null)
            {
                System.Diagnostics.Debug.WriteLine("CurrentShow is null");
                CurrentRunSummary = null;
                AllRunsSummary = null;
                return;
            }

            try
            {
                var products = await _dataService.LoadProductsAsync();
                System.Diagnostics.Debug.WriteLine($"Loaded {products.Count} products for summaries");

                // Update current run summary
                if (SelectedRun != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Creating summary for run: {SelectedRun.Name}");
                    CurrentRunSummary = new RunSummaryViewModel(SelectedRun, CurrentShow, products);
                    System.Diagnostics.Debug.WriteLine($"Current run summary has {CurrentRunSummary.OutstandingCount} outstanding issues");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No run selected");
                    CurrentRunSummary = null;
                }

                // Update all runs summary
                System.Diagnostics.Debug.WriteLine("Creating all runs summary");
                AllRunsSummary = new AllRunsSummaryViewModel(CurrentShow, products);
                System.Diagnostics.Debug.WriteLine($"All runs summary has {AllRunsSummary.TotalIssuesCount} total issues");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating run summaries: {ex.Message}");
                CurrentRunSummary = null;
                AllRunsSummary = null;
            }

            System.Diagnostics.Debug.WriteLine($"=== UpdateRunSummaries END ===");
        }

        public void RefreshRunSummaries()
        {
            UpdateRunSummaries();
        }

        public void RefreshRunsDropdown()
        {
            // Force refresh of the runs collection
            OnPropertyChanged(nameof(CurrentShow));

            // If no run is selected and there are runs available, select the first one
            if (SelectedRun == null && CurrentShow?.Runs?.Any() == true)
            {
                SelectedRun = CurrentShow.Runs.OrderBy(r => r.DateTime).First();
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
                System.Diagnostics.Debug.WriteLine($"=== JumpToScene: {scene.Name} ===");
                SelectedSceneFromButtons = scene;
                StatusMessage = $"Jumped to {scene.Name}";

                // Force property change notification for the UI to respond
                OnPropertyChanged(nameof(SelectedSceneFromButtons));

                // Small delay to ensure UI has updated before scrolling
                System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
                    new Action(() => OnPropertyChanged(nameof(SelectedSceneFromButtons))),
                    System.Windows.Threading.DispatcherPriority.Loaded);
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

            // Clear navigation properties before saving to avoid cycles
            ClearNavigationProperties();

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
                // Clear navigation properties before saving to avoid cycles
                ClearNavigationProperties();

                await _dataService.SaveShowAsync(CurrentShow, dialog.FileName);
                _currentShowPath = dialog.FileName;
                StatusMessage = $"Saved show: {CurrentShow.Name}";
                OnPropertyChanged(nameof(WindowTitle));
            }
        }

        private void ClearNavigationProperties()
        {
            if (CurrentShow == null) return;

            // Clear navigation properties that cause JSON cycles
            foreach (var transition in CurrentShow.SceneTransitions)
            {
                foreach (var action in transition.Actions)
                {
                    action.Product = null;
                    action.FromActor = null;
                    action.ToActor = null;
                    action.Show = null;
                }
            }
        }

        private void ManageProducts()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== ManageProducts START ===");
                System.Diagnostics.Debug.WriteLine("Creating ProductManagementWindow...");

                var productWindow = new Pack_Track.Views.ProductManagementWindow();
                System.Diagnostics.Debug.WriteLine("ProductManagementWindow created successfully");

                productWindow.Owner = Application.Current.MainWindow;
                System.Diagnostics.Debug.WriteLine("Owner set to MainWindow");

                System.Diagnostics.Debug.WriteLine("Calling ShowDialog...");
                productWindow.ShowDialog();

                StatusMessage = "Product management closed";
                System.Diagnostics.Debug.WriteLine("=== ManageProducts END ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ManageProducts: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                StatusMessage = $"Error opening product management: {ex.Message}";
                MessageBox.Show($"Error opening product management: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

            // Always refresh UI after show setup, especially if allocations changed
            if (setupWindow.ShowChanged)
            {
                System.Diagnostics.Debug.WriteLine("Show was changed, refreshing everything...");
                OnPropertyChanged(nameof(CurrentShow));
                UpdateLiveOperations();
                UpdateSceneButtons();
                UpdateRunSummaries();

                // Refresh the runs dropdown
                RefreshRunsDropdown();

                StatusMessage = "Show setup completed - data refreshed";
            }
            else
            {
                // Even if ShowChanged is false, we should refresh live operations 
                // in case allocations were modified
                System.Diagnostics.Debug.WriteLine("Refreshing live operations after setup...");
                UpdateLiveOperations();
                UpdateRunSummaries();

                // Still refresh runs dropdown in case runs were added
                RefreshRunsDropdown();

                StatusMessage = "Show setup completed";
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