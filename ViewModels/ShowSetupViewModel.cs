// ViewModels/ShowSetupViewModel.cs
using Pack_Track.Models;
using Pack_Track.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;

namespace Pack_Track.ViewModels
{
    public class ShowSetupViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;
        private Show _currentShow;
        private Actor? _selectedActor;
        private Scene? _selectedScene;
        private Run? _selectedRun;
        private string _newActorRole = string.Empty;
        private string _newActorName = string.Empty;
        private string _newActorPhone = string.Empty;
        private string _newSceneName = string.Empty;
        private int _newSceneNumber = 1;
        private string _newRunName = string.Empty;
        private DateTime _newRunDateTime = DateTime.Now;
        private RunType _newRunType = RunType.Rehearsal;

        public ShowSetupViewModel(Show show, IDataService dataService)
        {
            _currentShow = show;
            _dataService = dataService;

            // Initialize collections
            Cast = new ObservableCollection<Actor>(_currentShow.Cast);
            Scenes = new ObservableCollection<Scene>(_currentShow.Scenes);
            Runs = new ObservableCollection<Run>(_currentShow.Runs);
            Products = new ObservableCollection<Product>();

            // Initialize commands
            AddActorCommand = new RelayCommand(AddActor, CanAddActor);
            EditActorCommand = new RelayCommand(EditActor, () => SelectedActor != null);
            DeleteActorCommand = new RelayCommand(DeleteActor, () => SelectedActor != null);

            AddSceneCommand = new RelayCommand(AddScene, CanAddScene);
            EditSceneCommand = new RelayCommand(EditScene, () => SelectedScene != null);
            DeleteSceneCommand = new RelayCommand(DeleteScene, () => SelectedScene != null);

            AddRunCommand = new RelayCommand(AddRun, CanAddRun);
            EditRunCommand = new RelayCommand(EditRun, () => SelectedRun != null);
            DeleteRunCommand = new RelayCommand(DeleteRun, () => SelectedRun != null);

            ManageAllocationsCommand = new RelayCommand(ManageAllocations, CanManageAllocations);

            SaveShowCommand = new RelayCommand(SaveShow);

            LoadProducts();
        }

        // Properties
        public Show CurrentShow
        {
            get => _currentShow;
            set => SetProperty(ref _currentShow, value);
        }

        public ObservableCollection<Actor> Cast { get; }
        public ObservableCollection<Scene> Scenes { get; }
        public ObservableCollection<Run> Runs { get; }
        public ObservableCollection<Product> Products { get; }

        public Actor? SelectedActor
        {
            get => _selectedActor;
            set => SetProperty(ref _selectedActor, value);
        }

        public Scene? SelectedScene
        {
            get => _selectedScene;
            set => SetProperty(ref _selectedScene, value);
        }

        public Run? SelectedRun
        {
            get => _selectedRun;
            set => SetProperty(ref _selectedRun, value);
        }

        // New Actor Properties
        public string NewActorRole
        {
            get => _newActorRole;
            set => SetProperty(ref _newActorRole, value);
        }

        public string NewActorName
        {
            get => _newActorName;
            set => SetProperty(ref _newActorName, value);
        }

        public string NewActorPhone
        {
            get => _newActorPhone;
            set => SetProperty(ref _newActorPhone, value);
        }

        // New Scene Properties
        public string NewSceneName
        {
            get => _newSceneName;
            set => SetProperty(ref _newSceneName, value);
        }

        public int NewSceneNumber
        {
            get => _newSceneNumber;
            set => SetProperty(ref _newSceneNumber, value);
        }

        // New Run Properties
        public string NewRunName
        {
            get => _newRunName;
            set => SetProperty(ref _newRunName, value);
        }

        public DateTime NewRunDateTime
        {
            get => _newRunDateTime;
            set => SetProperty(ref _newRunDateTime, value);
        }

        public RunType NewRunType
        {
            get => _newRunType;
            set => SetProperty(ref _newRunType, value);
        }

        public Array RunTypes => Enum.GetValues(typeof(RunType));

        // Commands
        public ICommand AddActorCommand { get; }
        public ICommand EditActorCommand { get; }
        public ICommand DeleteActorCommand { get; }
        public ICommand AddSceneCommand { get; }
        public ICommand EditSceneCommand { get; }
        public ICommand DeleteSceneCommand { get; }
        public ICommand AddRunCommand { get; }
        public ICommand EditRunCommand { get; }
        public ICommand DeleteRunCommand { get; }
        public ICommand ManageAllocationsCommand { get; }
        public ICommand SaveShowCommand { get; }

        // Actor Management
        private void AddActor()
        {
            var actor = new Actor
            {
                RoleName = NewActorRole.Trim(),
                RealName = NewActorName.Trim(),
                PhoneNumber = NewActorPhone.Trim()
            };

            Cast.Add(actor);
            CurrentShow.Cast.Add(actor);

            // Clear form
            NewActorRole = string.Empty;
            NewActorName = string.Empty;
            NewActorPhone = string.Empty;
        }

        private bool CanAddActor()
        {
            return !string.IsNullOrWhiteSpace(NewActorRole) || !string.IsNullOrWhiteSpace(NewActorName);
        }

        private void EditActor()
        {
            if (SelectedActor == null) return;

            var dialog = new Views.ActorEditDialog(SelectedActor);
            if (dialog.ShowDialog() == true)
            {
                // Actor is updated by reference
                OnPropertyChanged(nameof(Cast));
            }
        }

        private void DeleteActor()
        {
            if (SelectedActor == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete '{SelectedActor.DisplayName}'?\nThis will also remove all their equipment allocations.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // Remove allocations for this actor
                foreach (var scene in Scenes)
                {
                    scene.Allocations.RemoveAll(a => a.ActorId == SelectedActor.Id);
                }

                Cast.Remove(SelectedActor);
                CurrentShow.Cast.Remove(SelectedActor);
                SelectedActor = null;
            }
        }

        // Scene Management
        private void AddScene()
        {
            var scene = new Scene
            {
                Name = NewSceneName.Trim(),
                SceneNumber = NewSceneNumber
            };

            Scenes.Add(scene);
            CurrentShow.Scenes.Add(scene);

            // Clear form
            NewSceneName = string.Empty;
            NewSceneNumber = Scenes.Count + 1;
        }

        private bool CanAddScene()
        {
            return !string.IsNullOrWhiteSpace(NewSceneName);
        }

        private void EditScene()
        {
            if (SelectedScene == null) return;

            var dialog = new Views.SceneEditDialog(SelectedScene);
            if (dialog.ShowDialog() == true)
            {
                // Scene is updated by reference
                OnPropertyChanged(nameof(Scenes));
            }
        }

        private void DeleteScene()
        {
            if (SelectedScene == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete '{SelectedScene.Name}'?\nThis will also remove all equipment allocations for this scene.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                Scenes.Remove(SelectedScene);
                CurrentShow.Scenes.Remove(SelectedScene);
                SelectedScene = null;
            }
        }

        // Run Management
        private void AddRun()
        {
            var run = new Run
            {
                Name = NewRunName.Trim(),
                DateTime = NewRunDateTime,
                RunType = NewRunType
            };

            Runs.Add(run);
            CurrentShow.Runs.Add(run);

            // Clear form
            NewRunName = string.Empty;
            NewRunDateTime = DateTime.Now;
            NewRunType = RunType.Rehearsal;
        }

        private bool CanAddRun()
        {
            return !string.IsNullOrWhiteSpace(NewRunName);
        }

        private void EditRun()
        {
            if (SelectedRun == null) return;

            var dialog = new Views.RunEditDialog(SelectedRun);
            if (dialog.ShowDialog() == true)
            {
                // Run is updated by reference
                OnPropertyChanged(nameof(Runs));
            }
        }

        private void DeleteRun()
        {
            if (SelectedRun == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete '{SelectedRun.Name}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                Runs.Remove(SelectedRun);
                CurrentShow.Runs.Remove(SelectedRun);
                SelectedRun = null;
            }
        }

        private void ManageAllocations()
        {
            var dialog = new Views.AllocationManagementDialog(CurrentShow, Products.ToList());
            dialog.ShowDialog();
        }

        private bool CanManageAllocations()
        {
            return Cast.Any() && Scenes.Any();
        }

        private async void SaveShow()
        {
            try
            {
                // Update the main show object with our changes
                _currentShow.Cast.Clear();
                _currentShow.Scenes.Clear();
                _currentShow.Runs.Clear();

                foreach (var actor in Cast)
                    _currentShow.Cast.Add(actor);
                foreach (var scene in Scenes)
                    _currentShow.Scenes.Add(scene);
                foreach (var run in Runs)
                    _currentShow.Runs.Add(run);

                // Trigger save in main window (we'll add an event for this)
                ShowSaved?.Invoke();

                MessageBox.Show("Show configuration saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving show: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event Action? ShowSaved;

        private async void LoadProducts()
        {
            try
            {
                var products = await _dataService.LoadProductsAsync();
                Products.Clear();
                foreach (var product in products)
                {
                    Products.Add(product);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading products: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}