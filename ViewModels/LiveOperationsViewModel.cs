// ViewModels/EnhancedLiveOperationsViewModel.cs
using Pack_Track.Models;
using Pack_Track.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;

namespace Pack_Track.ViewModels
{
    public class EnhancedLiveOperationsViewModel : BaseViewModel
    {
        private readonly Show _show;
        private readonly IDataService _dataService;
        private readonly List<Product> _allProducts;
        private Scene? _currentScene;
        private Run? _selectedRun;

        public EnhancedLiveOperationsViewModel(Show show, List<Product> allProducts, IDataService dataService)
        {
            _show = show;
            _allProducts = allProducts;
            _dataService = dataService;

            ActorEquipmentGroups = new ObservableCollection<ActorEquipmentGroupViewModel>();
            AvailableAssets = new ObservableCollection<AssetStatusViewModel>();
            Runs = new ObservableCollection<Run>(show.Runs);

            CheckOutAllCommand = new RelayCommand(CheckOutAll);
            CheckInAllCommand = new RelayCommand(CheckInAll);
            ViewMissingCommand = new RelayCommand(ViewMissing);
            ViewTransactionsCommand = new RelayCommand(ViewTransactions);

            CurrentScene = show.CurrentScene;
            LoadEquipmentData();
        }

        public ObservableCollection<ActorEquipmentGroupViewModel> ActorEquipmentGroups { get; }
        public ObservableCollection<AssetStatusViewModel> AvailableAssets { get; }
        public ObservableCollection<Run> Runs { get; }

        public Scene? CurrentScene
        {
            get => _currentScene;
            set
            {
                if (SetProperty(ref _currentScene, value))
                {
                    LoadEquipmentData();
                }
            }
        }

        public Run? SelectedRun
        {
            get => _selectedRun;
            set => SetProperty(ref _selectedRun, value);
        }

        public string SceneName => CurrentScene?.Name ?? "No Scene";
        public int EquipmentCount => ActorEquipmentGroups.Sum(g => g.AssetItems.Count);
        public int CheckedOutCount => _show.AssetStatuses.Count(a => a.Status == EquipmentStatus.CheckedOut);
        public int CheckedInCount => _show.AssetStatuses.Count(a => a.Status == EquipmentStatus.CheckedIn);
        public int AvailableCount => _show.AssetStatuses.Count(a => a.Status == EquipmentStatus.Available);
        public int MissingCount => _show.AssetStatuses.Count(a => a.Status == EquipmentStatus.Missing || a.Status == EquipmentStatus.Damaged);

        public ICommand CheckOutAllCommand { get; }
        public ICommand CheckInAllCommand { get; }
        public ICommand ViewMissingCommand { get; }
        public ICommand ViewTransactionsCommand { get; }

        private void LoadEquipmentData()
        {
            ActorEquipmentGroups.Clear();
            AvailableAssets.Clear();

            if (CurrentScene == null) return;

            // Get allocations with asset status for current scene
            var allocationsWithAssets = _show.GetAllocationsWithAssets(CurrentScene.Id, _allProducts);

            // Group by actor
            var actorGroups = allocationsWithAssets.GroupBy(a => a.ActorId);

            foreach (var actorGroup in actorGroups)
            {
                var actor = actorGroup.First().Actor;
                if (actor == null) continue;

                var actorEquipmentGroup = new ActorEquipmentGroupViewModel(actor, actorGroup.ToList(), _show, this);
                ActorEquipmentGroups.Add(actorEquipmentGroup);
            }

            // Load available assets (not currently assigned)
            foreach (var assetStatus in _show.AssetStatuses.Where(a => a.CurrentlyAssignedToActorId == null))
            {
                var product = _allProducts.FirstOrDefault(p => p.Id == assetStatus.ProductId);
                if (product != null)
                {
                    AvailableAssets.Add(new AssetStatusViewModel(assetStatus, product, _show, this));
                }
            }

            RefreshCounts();
        }

        public void RefreshCounts()
        {
            OnPropertyChanged(nameof(EquipmentCount));
            OnPropertyChanged(nameof(CheckedOutCount));
            OnPropertyChanged(nameof(CheckedInCount));
            OnPropertyChanged(nameof(AvailableCount));
            OnPropertyChanged(nameof(MissingCount));
        }

        private void CheckOutAll()
        {
            if (CurrentScene == null) return;

            foreach (var actorGroup in ActorEquipmentGroups)
            {
                foreach (var assetItem in actorGroup.AssetItems.Where(a => a.CanCheckOut))
                {
                    assetItem.CheckOut();
                }
            }
        }

        private void CheckInAll()
        {
            foreach (var actorGroup in ActorEquipmentGroups)
            {
                foreach (var assetItem in actorGroup.AssetItems.Where(a => a.CanCheckIn))
                {
                    assetItem.CheckIn();
                }
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

            // For now, show a simple message. Later we can create an enhanced dialog
            var message = string.Join("\n", missingAssets.Select(a => $"Asset {a.AssetNumber} - {a.Status}"));
            MessageBox.Show($"Missing/Damaged Equipment:\n\n{message}", "Equipment Issues",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void ViewTransactions()
        {
            // For now, show a simple message. Later we can create a transaction history dialog
            var recentTransactions = _show.Transactions
                .OrderByDescending(t => t.TransactionTime)
                .Take(10)
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

    // ViewModel for each actor's equipment group
    public class ActorEquipmentGroupViewModel : BaseViewModel
    {
        private readonly Show _show;
        private readonly EnhancedLiveOperationsViewModel _parentViewModel;

        public ActorEquipmentGroupViewModel(Actor actor, List<AllocationWithAssets> allocations, Show show, EnhancedLiveOperationsViewModel parentViewModel)
        {
            Actor = actor;
            _show = show;
            _parentViewModel = parentViewModel;

            AssetItems = new ObservableCollection<AssetItemViewModel>();

            foreach (var allocation in allocations)
            {
                if (allocation.AssetStatus != null)
                {
                    var assetItem = new AssetItemViewModel(allocation, show, parentViewModel);
                    AssetItems.Add(assetItem);
                }
            }

            // Calculate group status
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

        private EquipmentStatus _groupStatus;
        public EquipmentStatus GroupStatus
        {
            get => _groupStatus;
            private set => SetProperty(ref _groupStatus, value);
        }

        public string StatusColor => Status switch
        {
            EquipmentStatus.Available => "#9E9E9E",
            EquipmentStatus.CheckedOut => "#FF9800",
            EquipmentStatus.CheckedIn => "#4CAF50",
            EquipmentStatus.Missing => "#F44336",
            EquipmentStatus.Damaged => "#E91E63",
            _ => "#9E9E9E"
        };

        public string StatusText => Status switch
        {
            EquipmentStatus.Available => "Available",
            EquipmentStatus.CheckedOut => "Checked Out",
            EquipmentStatus.CheckedIn => "Checked In",
            EquipmentStatus.Missing => "Missing",
            EquipmentStatus.Damaged => "Damaged",
            _ => "Unknown"
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

        public ICommand CheckOutCommand { get; }
        public ICommand CheckInCommand { get; }
        public ICommand MarkMissingCommand { get; }
        public ICommand MarkDamagedCommand { get; }

        public void CheckOut()
        {
            if (_allocation.AssetStatus != null && _parentViewModel.CurrentScene != null)
            {
                _show.CheckOutEquipment(_allocation.AssetStatus.Id, _allocation.ActorId, _parentViewModel.CurrentScene.Id);
            }
        }

        public void CheckIn()
        {
            if (_allocation.AssetStatus != null && _parentViewModel.CurrentScene != null)
            {
                _show.CheckInEquipment(_allocation.AssetStatus.Id, _parentViewModel.CurrentScene.Id);
            }
        }

        private void MarkMissing()
        {
            var result = MessageBox.Show(
                $"Mark {ProductName} as missing?",
                "Confirm Missing Equipment",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes && _allocation.AssetStatus != null)
            {
                _allocation.AssetStatus.Status = EquipmentStatus.Missing;
            }
        }

        private void MarkDamaged()
        {
            var result = MessageBox.Show(
                $"Mark {ProductName} as damaged?",
                "Confirm Damaged Equipment",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes && _allocation.AssetStatus != null)
            {
                _allocation.AssetStatus.Status = EquipmentStatus.Damaged;
            }
        }
    }

    // ViewModel for available assets (in equipment pool)
    public class AssetStatusViewModel : BaseViewModel
    {
        private readonly AssetStatus _assetStatus;
        private readonly Product _product;
        private readonly Show _show;
        private readonly EnhancedLiveOperationsViewModel _parentViewModel;

        public AssetStatusViewModel(AssetStatus assetStatus, Product product, Show show, EnhancedLiveOperationsViewModel parentViewModel)
        {
            _assetStatus = assetStatus;
            _product = product;
            _show = show;
            _parentViewModel = parentViewModel;

            AssignToActorCommand = new RelayCommand<Actor>(AssignToActor);

            // Subscribe to status changes
            _assetStatus.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(AssetStatus.Status))
                {
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(IsAvailable));
                    OnPropertyChanged(nameof(StatusColor));
                    _parentViewModel.RefreshCounts();
                }
            };
        }

        public string DisplayName => $"{_product.Name} ({_assetStatus.AssetNumber})";
        public EquipmentStatus Status => _assetStatus.Status;
        public bool IsAvailable => _assetStatus.IsAvailable;

        public string StatusColor => Status switch
        {
            EquipmentStatus.Available => "#9E9E9E",
            EquipmentStatus.CheckedOut => "#FF9800",
            EquipmentStatus.CheckedIn => "#4CAF50",
            EquipmentStatus.Missing => "#F44336",
            EquipmentStatus.Damaged => "#E91E63",
            _ => "#9E9E9E"
        };

        public ICommand AssignToActorCommand { get; }

        private void AssignToActor(Actor? actor)
        {
            if (actor != null && _parentViewModel.CurrentScene != null && _assetStatus.IsAvailable)
            {
                _show.CheckOutEquipment(_assetStatus.Id, actor.Id, _parentViewModel.CurrentScene.Id);
                _parentViewModel.RefreshCounts();
            }
        }
    }
}
Color => GroupStatus switch
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

public string EquipmentList => string.Join("\n", AssetItems.Select(a => a.ProductName));

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
        GroupStatus = EquipmentStatus.Missing; // Show as problematic if any items are missing/damaged
    }
    else
    {
        GroupStatus = EquipmentStatus.CheckedOut; // Mixed status defaults to checked out
    }

    OnPropertyChanged(nameof(StatusColor));
    OnPropertyChanged(nameof(StatusText));
}
    }

    // ViewModel for individual asset items
    public class AssetItemViewModel : BaseViewModel
{
    private readonly AllocationWithAssets _allocation;
    private readonly Show _show;
    private readonly EnhancedLiveOperationsViewModel _parentViewModel;

    public AssetItemViewModel(AllocationWithAssets allocation, Show show, EnhancedLiveOperationsViewModel parentViewModel)
    {
        _allocation = allocation;
        _show = show;
        _parentViewModel = parentViewModel;

        CheckOutCommand = new RelayCommand(CheckOut, () => CanCheckOut);
        CheckInCommand = new RelayCommand(CheckIn, () => CanCheckIn);
        MarkMissingCommand = new RelayCommand(MarkMissing);
        MarkDamagedCommand = new RelayCommand(MarkDamaged);

        // Subscribe to asset status changes
        if (_allocation.AssetStatus != null)
        {
            _allocation.AssetStatus.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(AssetStatus.Status))
                {
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(StatusColor));
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(CanCheckOut));
                    OnPropertyChanged(nameof(CanCheckIn));
                    _parentViewModel.RefreshCounts();
                }
            };
        }
    }

    public string ProductName => _allocation.ProductDisplayName;
    public EquipmentStatus Status => _allocation.AssetStatus?.Status ?? EquipmentStatus.Available;
    public bool CanCheckOut => _allocation.AssetStatus?.IsAvailable == true;
    public bool CanCheckIn => _allocation.AssetStatus?.IsCheckedOut == true &&
                              _allocation.AssetStatus?.CurrentlyAssignedToActorId == _allocation.ActorId;

    public string Status