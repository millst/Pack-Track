// ViewModels/ShowSetupViewModel.cs - Fixed Add Actor Command
using Pack_Track.Models;
using Pack_Track.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;

namespace Pack_Track.ViewModels
{
    public class ShowSetupViewModel : BaseViewModel
    {
        private readonly Show _show;
        private readonly IDataService _dataService;
        private List<Product> _allProducts = new List<Product>();

        // Show details
        private string _showName;
        private string _showDescription = string.Empty;

        // New scene
        private string _newSceneName = string.Empty;
        private int _newSceneNumber;

        // New cast member
        private string _newActorName = string.Empty;
        private string _newActorRole = string.Empty;
        private string _newActorPhone = string.Empty;

        // New run
        private string _newRunName = string.Empty;
        private DateTime _newRunDate = DateTime.Today;
        private TimeSpan _newRunTime = new TimeSpan(19, 30, 0); // 7:30 PM default
        private RunType _newRunType = RunType.Rehearsal;

        public ShowSetupViewModel(Show show, IDataService dataService)
        {
            _show = show;
            _dataService = dataService;

            // Initialize collections
            Scenes = new ObservableCollection<Scene>(show.Scenes.OrderBy(s => s.SceneNumber));
            Cast = new ObservableCollection<Actor>(show.Cast);
            Runs = new ObservableCollection<Run>(show.Runs.OrderBy(r => r.DateTime));

            // Initialize properties
            _showName = show.Name;
            ShowDescription = show.Description;

            // Set default scene number to next available
            UpdateNewSceneNumber();

            // Commands - Fixed to use proper CanExecute logic and correct names
            AddSceneCommand = new RelayCommand(AddScene, CanAddScene);
            RemoveSceneCommand = new RelayCommand(RemoveScene, () => SelectedScene != null);
            AddActorCommand = new RelayCommand(AddCastMember, CanAddCastMember); // Using AddActorCommand to match XAML
            AddCastMemberCommand = new RelayCommand(AddCastMember, CanAddCastMember); // Backup for any old references
            RemoveCastMemberCommand = new RelayCommand(RemoveCastMember, () => SelectedActor != null);
            AddRunCommand = new RelayCommand(AddRun, CanAddRun);
            RemoveRunCommand = new RelayCommand(RemoveRun, () => SelectedRun != null);
            ManageAllocationsCommand = new RelayCommand(ManageAllocations, () => Scenes.Any() && Cast.Any());

            LoadProducts();

            System.Diagnostics.Debug.WriteLine($"ShowSetupViewModel initialized. Cast count: {Cast.Count}");
        }

        public ObservableCollection<Scene> Scenes { get; }
        public ObservableCollection<Actor> Cast { get; }
        public ObservableCollection<Run> Runs { get; }

        public string ShowName
        {
            get => _showName;
            set
            {
                if (SetProperty(ref _showName, value))
                {
                    _show.Name = value;
                }
            }
        }

        public string ShowDescription
        {
            get => _showDescription;
            set
            {
                if (SetProperty(ref _showDescription, value))
                {
                    _show.Description = value;
                }
            }
        }

        // Scene properties
        public string NewSceneName
        {
            get => _newSceneName;
            set
            {
                if (SetProperty(ref _newSceneName, value))
                {
                    CommandManager.InvalidateRequerySuggested(); // Refresh CanExecute
                }
            }
        }

        public int NewSceneNumber
        {
            get => _newSceneNumber;
            set => SetProperty(ref _newSceneNumber, value);
        }

        private Scene? _selectedScene;
        public Scene? SelectedScene
        {
            get => _selectedScene;
            set => SetProperty(ref _selectedScene, value);
        }

        // Cast properties
        public string NewActorName
        {
            get => _newActorName;
            set
            {
                if (SetProperty(ref _newActorName, value))
                {
                    System.Diagnostics.Debug.WriteLine($"NewActorName changed to: '{value}'");
                    CommandManager.InvalidateRequerySuggested(); // Refresh CanExecute
                }
            }
        }

        public string NewActorRole
        {
            get => _newActorRole;
            set
            {
                if (SetProperty(ref _newActorRole, value))
                {
                    System.Diagnostics.Debug.WriteLine($"NewActorRole changed to: '{value}'");
                    CommandManager.InvalidateRequerySuggested(); // Refresh CanExecute
                }
            }
        }

        public string NewActorPhone
        {
            get => _newActorPhone;
            set => SetProperty(ref _newActorPhone, value);
        }

        private Actor? _selectedActor;
        public Actor? SelectedActor
        {
            get => _selectedActor;
            set => SetProperty(ref _selectedActor, value);
        }

        // Run properties
        public string NewRunName
        {
            get => _newRunName;
            set
            {
                if (SetProperty(ref _newRunName, value))
                {
                    CommandManager.InvalidateRequerySuggested(); // Refresh CanExecute
                }
            }
        }

        public DateTime NewRunDate
        {
            get => _newRunDate;
            set => SetProperty(ref _newRunDate, value);
        }

        public TimeSpan NewRunTime
        {
            get => _newRunTime;
            set => SetProperty(ref _newRunTime, value);
        }

        public RunType NewRunType
        {
            get => _newRunType;
            set => SetProperty(ref _newRunType, value);
        }

        // Available run types for the dropdown
        public Array RunTypes => Enum.GetValues(typeof(RunType));

        private Run? _selectedRun;
        public Run? SelectedRun
        {
            get => _selectedRun;
            set => SetProperty(ref _selectedRun, value);
        }

        // Commands
        public ICommand AddSceneCommand { get; }
        public ICommand RemoveSceneCommand { get; }
        public ICommand AddActorCommand { get; } // This matches the XAML binding
        public ICommand AddCastMemberCommand { get; } // Backup for compatibility
        public ICommand RemoveCastMemberCommand { get; }
        public ICommand AddRunCommand { get; }
        public ICommand RemoveRunCommand { get; }
        public ICommand ManageAllocationsCommand { get; }

        private async void LoadProducts()
        {
            try
            {
                _allProducts = await _dataService.LoadProductsAsync();
                System.Diagnostics.Debug.WriteLine($"Loaded {_allProducts.Count} products for show setup");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading products: {ex.Message}");
                MessageBox.Show($"Error loading products: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateNewSceneNumber()
        {
            // Find the highest scene number and add 1
            if (Scenes.Any())
            {
                NewSceneNumber = Scenes.Max(s => s.SceneNumber) + 1;
            }
            else
            {
                NewSceneNumber = 1;
            }
        }

        // CanExecute methods
        private bool CanAddScene()
        {
            return !string.IsNullOrWhiteSpace(NewSceneName);
        }

        private bool CanAddCastMember()
        {
            var canAdd = !string.IsNullOrWhiteSpace(NewActorName) || !string.IsNullOrWhiteSpace(NewActorRole);
            System.Diagnostics.Debug.WriteLine($"CanAddCastMember: {canAdd} (Name: '{NewActorName}', Role: '{NewActorRole}')");
            return canAdd;
        }

        private bool CanAddRun()
        {
            return !string.IsNullOrWhiteSpace(NewRunName);
        }

        // Command implementations
        private void AddScene()
        {
            System.Diagnostics.Debug.WriteLine($"AddScene called: {NewSceneName}");

            var scene = new Scene
            {
                Name = NewSceneName,
                SceneNumber = NewSceneNumber
            };

            _show.Scenes.Add(scene);
            Scenes.Add(scene);

            // Clear form and update scene number for next
            NewSceneName = string.Empty;
            UpdateNewSceneNumber();

            // Set current scene if this is the first one
            if (_show.CurrentScene == null)
            {
                _show.CurrentScene = scene;
            }

            System.Diagnostics.Debug.WriteLine($"Scene added successfully. Total scenes: {Scenes.Count}");
        }

        private void RemoveScene()
        {
            if (SelectedScene == null) return;

            var result = MessageBox.Show(
                $"Remove scene '{SelectedScene.Name}'? This will also remove all equipment allocations for this scene.",
                "Confirm Remove Scene",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _show.Scenes.Remove(SelectedScene);
                Scenes.Remove(SelectedScene);

                // Update current scene if we removed it
                if (_show.CurrentScene?.Id == SelectedScene.Id)
                {
                    _show.CurrentScene = Scenes.FirstOrDefault();
                }

                SelectedScene = null;
                UpdateNewSceneNumber();
            }
        }

        private void AddCastMember()
        {
            System.Diagnostics.Debug.WriteLine($"AddCastMember called - Name: '{NewActorName}', Role: '{NewActorRole}'");

            // Require at least one field to be filled
            if (string.IsNullOrWhiteSpace(NewActorName) && string.IsNullOrWhiteSpace(NewActorRole))
            {
                MessageBox.Show("Please enter either an actor name or role name.", "Missing Information",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var actor = new Actor
            {
                RealName = NewActorName?.Trim() ?? string.Empty,
                RoleName = NewActorRole?.Trim() ?? string.Empty,
                PhoneNumber = NewActorPhone?.Trim() ?? string.Empty
            };

            _show.Cast.Add(actor);
            Cast.Add(actor);

            // Clear form
            NewActorName = string.Empty;
            NewActorRole = string.Empty;
            NewActorPhone = string.Empty;

            System.Diagnostics.Debug.WriteLine($"Actor added successfully. Total cast: {Cast.Count}");
        }

        private void RemoveCastMember()
        {
            if (SelectedActor == null) return;

            var result = MessageBox.Show(
                $"Remove cast member '{SelectedActor.DisplayName}'? This will also remove all equipment allocations for this actor.",
                "Confirm Remove Cast Member",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // Remove all allocations for this actor
                foreach (var scene in _show.Scenes)
                {
                    scene.Allocations.RemoveAll(a => a.ActorId == SelectedActor.Id);
                }

                _show.Cast.Remove(SelectedActor);
                Cast.Remove(SelectedActor);
                SelectedActor = null;
            }
        }

        private void AddRun()
        {
            System.Diagnostics.Debug.WriteLine($"AddRun called: {NewRunName}");

            var run = new Run
            {
                Name = NewRunName,
                DateTime = NewRunDate.Add(NewRunTime),
                RunType = NewRunType
            };

            _show.Runs.Add(run);
            Runs.Add(run);

            // Sort runs by date
            var sortedRuns = Runs.OrderBy(r => r.DateTime).ToList();
            Runs.Clear();
            foreach (var sortedRun in sortedRuns)
            {
                Runs.Add(sortedRun);
            }

            // Clear form
            NewRunName = string.Empty;
            NewRunDate = DateTime.Today;
            NewRunTime = new TimeSpan(19, 30, 0);
            NewRunType = RunType.Rehearsal; // Reset to default

            System.Diagnostics.Debug.WriteLine($"Run added successfully. Total runs: {Runs.Count}");
        }

        private void RemoveRun()
        {
            if (SelectedRun == null) return;

            var result = MessageBox.Show(
                $"Remove run '{SelectedRun.Name}'?",
                "Confirm Remove Run",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _show.Runs.Remove(SelectedRun);
                Runs.Remove(SelectedRun);
                SelectedRun = null;
            }
        }

        private void ManageAllocations()
        {
            var allocationWindow = new Pack_Track.Views.AllocationManagementWindow(_show, _allProducts);
            allocationWindow.Owner = Application.Current.MainWindow;
            allocationWindow.ShowDialog();
        }
    }
}