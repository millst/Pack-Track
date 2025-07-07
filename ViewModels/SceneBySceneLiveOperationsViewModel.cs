// ViewModels/SceneBySceneLiveOperationsViewModel.cs
using Pack_Track.Models;
using Pack_Track.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;

namespace Pack_Track.ViewModels
{
    public class SceneBySceneLiveOperationsViewModel : BaseViewModel
    {
        private readonly Show _show;
        private readonly IDataService _dataService;
        private readonly List<Product> _allProducts;

        public SceneBySceneLiveOperationsViewModel(Show show, List<Product> allProducts, IDataService dataService)
        {
            _show = show;
            _allProducts = allProducts;
            _dataService = dataService;

            SceneOperations = new ObservableCollection<SceneOperationViewModel>();

            CheckOutAllCommand = new RelayCommand(CheckOutAll);
            CheckInAllCommand = new RelayCommand(CheckInAll);
            ViewMissingCommand = new RelayCommand(ViewMissing);
            ViewTransactionsCommand = new RelayCommand(ViewTransactions);

            LoadSceneOperations();
        }

        public ObservableCollection<SceneOperationViewModel> SceneOperations { get; }

        public int TotalEquipmentCount => SceneOperations.Sum(s => s.ActorEquipmentGroups.Sum(a => a.AssetItems.Count));
        public int TotalCheckedOutCount => _show.AssetStatuses.Count(a => a.Status == EquipmentStatus.CheckedOut);
        public int TotalCheckedInCount => _show.AssetStatuses.Count(a => a.Status == EquipmentStatus.CheckedIn);
        public int TotalAvailableCount => _show.AssetStatuses.Count(a => a.Status == EquipmentStatus.Available);
        public int TotalMissingCount => _show.AssetStatuses.Count(a => a.Status == EquipmentStatus.Missing || a.Status == EquipmentStatus.Damaged);

        public ICommand CheckOutAllCommand { get; }
        public ICommand CheckInAllCommand { get; }
        public ICommand ViewMissingCommand { get; }
        public ICommand ViewTransactionsCommand { get; }

        private void LoadSceneOperations()
        {
            SceneOperations.Clear();

            foreach (var scene in _show.Scenes.OrderBy(s => s.SceneNumber))
            {
                var sceneOperation = new SceneOperationViewModel(scene, _show, _allProducts, this);
                SceneOperations.Add(sceneOperation);
            }

            RefreshCounts();
        }

        public void RefreshCounts()
        {
            OnPropertyChanged(nameof(TotalEquipmentCount));
            OnPropertyChanged(nameof(TotalCheckedOutCount));
            OnPropertyChanged(nameof(TotalCheckedInCount));
            OnPropertyChanged(nameof(TotalAvailableCount));
            OnPropertyChanged(nameof(TotalMissingCount));

            // Refresh all scene operations
            foreach (var sceneOp in SceneOperations)
            {
                sceneOp.RefreshCounts();
            }
        }

        private void CheckOutAll()
        {
            foreach (var sceneOp in SceneOperations)
            {
                sceneOp.CheckOutAll();
            }
        }

        private void CheckInAll()
        {
            foreach (var sceneOp in SceneOperations)
            {
                sceneOp.CheckInAll();
            }
        }

        private void ViewMissing()
        {
            var missingAssets = _show.AssetStatuses
                .Where(a => a.Status == EquipmentStatus.Missing || a.Status == EquipmentStatus.Damaged)
                .ToList();

            if (!missingAssets.Any())
            {
                MessageBox.Show("No missing or damaged equipment!", "All Equipment Accounted For",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var message = string.Join("\n", missingAssets.Select(a =>
            {
                var product = _allProducts.FirstOrDefault(p => p.Id == a.ProductId);
                return $"{product?.Name ?? "Unknown"} ({a.AssetNumber}) - {a.Status}";
            }));
            MessageBox.Show($"Missing/Damaged Equipment:\n\n{message}", "Equipment Issues",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void ViewTransactions()
        {
            var recentTransactions = _show.Transactions
                .OrderByDescending(t => t.TransactionTime)
                .Take(20)
                .ToList();

            if (!recentTransactions.Any())
            {
                MessageBox.Show("No transactions recorded yet.", "Transaction History",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var message = string.Join("\n", recentTransactions.Select(t =>
                $"{t.TransactionTime:HH:mm} - {t.Type} - Asset {t.AssetStatus?.AssetNumber}"));
            MessageBox.Show($"Recent Transactions:\n\n{message}", "Transaction History",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public class SceneOperationViewModel : BaseViewModel
    {
        private readonly Scene _scene;
        private readonly Show _show;
        private readonly List<Product> _allProducts;
        private readonly SceneBySceneLiveOperationsViewModel _parentViewModel;

        public SceneOperationViewModel(Scene scene, Show show, List<Product> allProducts, SceneBySceneLiveOperationsViewModel parentViewModel)
        {
            _scene = scene;
            _show = show;
            _allProducts = allProducts;
            _parentViewModel = parentViewModel;

            ActorEquipmentGroups = new ObservableCollection<ActorEquipmentGroupViewModel>();

            CheckOutAllSceneCommand = new RelayCommand(CheckOutAll);
            CheckInAllSceneCommand = new RelayCommand(CheckInAll);

            LoadActorEquipmentGroups();
        }

        public Scene Scene => _scene;
        public ObservableCollection<ActorEquipmentGroupViewModel> ActorEquipmentGroups { get; }

        public string SceneTitle => $"Scene {_scene.SceneNumber}: {_scene.Name}";
        public int SceneEquipmentCount => ActorEquipmentGroups.Sum(a => a.AssetItems.Count);
        public int SceneCheckedOutCount => ActorEquipmentGroups.Sum(a => a.AssetItems.Count(i => i.Status == EquipmentStatus.CheckedOut));
        public int SceneCheckedInCount => ActorEquipmentGroups.Sum(a => a.AssetItems.Count(i => i.Status == EquipmentStatus.CheckedIn));

        public ICommand CheckOutAllSceneCommand { get; }
        public ICommand CheckInAllSceneCommand { get; }

        private void LoadActorEquipmentGroups()
        {
            ActorEquipmentGroups.Clear();

            // Get allocations with asset status for this scene
            var allocationsWithAssets = _show.GetAllocationsWithAssets(_scene.Id, _allProducts);

            // Group by actor
            var actorGroups = allocationsWithAssets.GroupBy(a => a.ActorId);

            foreach (var actorGroup in actorGroups)
            {
                var actor = actorGroup.First().Actor;
                if (actor == null) continue;

                var actorEquipmentGroup = new ActorEquipmentGroupViewModel(actor, actorGroup.ToList(), _show, _parentViewModel);
                ActorEquipmentGroups.Add(actorEquipmentGroup);
            }
        }

        public void RefreshCounts()
        {
            OnPropertyChanged(nameof(SceneEquipmentCount));
            OnPropertyChanged(nameof(SceneCheckedOutCount));
            OnPropertyChanged(nameof(SceneCheckedInCount));
        }

        public void CheckOutAll()
        {
            foreach (var actorGroup in ActorEquipmentGroups)
            {
                foreach (var assetItem in actorGroup.AssetItems.Where(a => a.CanCheckOut))
                {
                    assetItem.CheckOut();
                }
            }
        }

        public void CheckInAll()
        {
            foreach (var actorGroup in ActorEquipmentGroups)
            {
                foreach (var assetItem in actorGroup.AssetItems.Where(a => a.CanCheckIn))
                {
                    assetItem.CheckIn();
                }
            }
        }
    }

    // Update the existing ActorEquipmentGroupViewModel to work with the new parent
    public class ActorEquipmentGroupViewModel : BaseViewModel
    {
        private readonly Show _show;
        private readonly object _parentViewModel; // Can be either type
        private EquipmentStatus _groupStatus;

        public ActorEquipmentGroupViewModel(Actor actor, List<AllocationWithAssets> allocations, Show show, object parentViewModel)
        {
            Actor = actor;
            _show = show;
            _parentViewModel = parentViewModel;

            AssetItems = new ObservableCollection<AssetItemViewModel>();

            foreach (var allocation in allocations)
            {
                if (allocation.AssetStatus != null)
                {
                    var assetItem = new AssetItemViewModel(allocation, show, this);
                    AssetItems.Add(assetItem);
                }
            }

            UpdateGroupStatus();

            // Subscribe to changes in asset items
            foreach (var item in AssetItems)
            {
                item.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(AssetItemViewModel.Status))
                    {
                        UpdateGroupStatus();
                    }
                };
            }
        }

        public Actor Actor { get; }
        public ObservableCollection<AssetItemViewModel> AssetItems { get; }

        public EquipmentStatus GroupStatus
        {
            get => _groupStatus;
            private set => SetProperty(ref _groupStatus, value);
        }

        public string StatusColor => GroupStatus switch
        {
            EquipmentStatus.Available => "#9E9E9E",
            EquipmentStatus.CheckedOut => "#FF9800",
            EquipmentStatus.CheckedIn => "#4CAF50",
            EquipmentStatus.Missing => "#F44336",
            EquipmentStatus.Damaged => "#E91E63",
            _ => "#9E9E9E"
        };

        public string StatusText => GroupStatus switch
        {
            EquipmentStatus.Available => "Available",
            EquipmentStatus.CheckedOut => "Checked Out",
            EquipmentStatus.CheckedIn => "Checked In",
            EquipmentStatus.Missing => "Missing Items",
            EquipmentStatus.Damaged => "Damaged Items",
            _ => "Mixed Status"
        };

        public string EquipmentList => string.Join(", ", AssetItems.Select(a => a.ProductName));

        public void UpdateGroupStatus()
        {
            if (!AssetItems.Any())
            {
                GroupStatus = EquipmentStatus.Available;
                return;
            }

            var statuses = AssetItems.Select(a => a.Status).Distinct().ToList();

            if (statuses.Count == 1)
            {
                GroupStatus = statuses.First();
            }
            else if (statuses.Any(s => s == EquipmentStatus.Missing || s == EquipmentStatus.Damaged))
            {
                GroupStatus = EquipmentStatus.Missing;
            }
            else
            {
                GroupStatus = EquipmentStatus.CheckedOut;
            }

            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(StatusText));
        }

        public void RefreshCounts()
        {
            // Notify parent to refresh counts
            if (_parentViewModel is SceneBySceneLiveOperationsViewModel sceneByScene)
            {
                sceneByScene.RefreshCounts();
            }
        }
    }

    // Updated AssetItemViewModel to work with the new structure
    public class AssetItemViewModel : BaseViewModel
    {
        internal readonly AllocationWithAssets _allocation;
        private readonly Show _show;
        private readonly ActorEquipmentGroupViewModel _parentGroup;

        public AssetItemViewModel(AllocationWithAssets allocation, Show show, ActorEquipmentGroupViewModel parentGroup)
        {
            _allocation = allocation;
            _show = show;
            _parentGroup = parentGroup;

            ToggleStatusCommand = new RelayCommand(ToggleStatus);

            // Subscribe to asset status changes
            if (_allocation.AssetStatus != null)
            {
                _allocation.AssetStatus.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(AssetStatus.Status))
                    {
                        OnPropertyChanged(nameof(Status));
                        OnPropertyChanged(nameof(StatusColor));
                        OnPropertyChanged(nameof(ActionButtonText));
                        OnPropertyChanged(nameof(CanCheckOut));
                        OnPropertyChanged(nameof(CanCheckIn));
                        _parentGroup.RefreshCounts();
                    }
                };
            }
        }

        public string ProductName => _allocation.ProductDisplayName;
        public EquipmentStatus Status => _allocation.AssetStatus?.Status ?? EquipmentStatus.Available;
        public bool CanCheckOut => _allocation.AssetStatus?.IsAvailable == true;
        public bool CanCheckIn => _allocation.AssetStatus?.IsCheckedOut == true &&
                                  _allocation.AssetStatus?.CurrentlyAssignedToActorId == _allocation.ActorId;

        public string StatusColor => Status switch
        {
            EquipmentStatus.Available => "#9E9E9E",
            EquipmentStatus.CheckedOut => "#FF9800",
            EquipmentStatus.CheckedIn => "#4CAF50",
            EquipmentStatus.Missing => "#F44336",
            EquipmentStatus.Damaged => "#E91E63",
            _ => "#9E9E9E"
        };

        public string ActionButtonText => Status switch
        {
            EquipmentStatus.Available => "Check Out",
            EquipmentStatus.CheckedOut => "Check In",
            EquipmentStatus.CheckedIn => "Check Out",
            EquipmentStatus.Missing => "Found",
            EquipmentStatus.Damaged => "Repaired",
            _ => "Toggle"
        };

        public ICommand ToggleStatusCommand { get; }

        private void ToggleStatus()
        {
            switch (Status)
            {
                case EquipmentStatus.Available:
                    CheckOut();
                    break;
                case EquipmentStatus.CheckedOut:
                    CheckIn();
                    break;
                case EquipmentStatus.CheckedIn:
                    CheckOut();
                    break;
                case EquipmentStatus.Missing:
                    MarkFound();
                    break;
                case EquipmentStatus.Damaged:
                    MarkRepaired();
                    break;
            }
        }

        public void CheckOut()
        {
            if (_allocation.AssetStatus != null && _allocation.Scene != null)
            {
                _show.CheckOutEquipment(_allocation.AssetStatus.Id, _allocation.ActorId, _allocation.Scene.Id);
            }
        }

        public void CheckIn()
        {
            if (_allocation.AssetStatus != null && _allocation.Scene != null)
            {
                _show.CheckInEquipment(_allocation.AssetStatus.Id, _allocation.Scene.Id);
            }
        }

        private void MarkFound()
        {
            if (_allocation.AssetStatus != null)
            {
                _allocation.AssetStatus.Status = EquipmentStatus.Available;
            }
        }

        private void MarkRepaired()
        {
            if (_allocation.AssetStatus != null)
            {
                _allocation.AssetStatus.Status = EquipmentStatus.Available;
            }
        }
    }
}