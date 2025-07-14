// ViewModels/SceneBySceneLiveOperationsViewModel.cs - Fixed version with proper access methods
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

            SceneOperations = new ObservableCollection<ISceneOperation>();

            CheckOutAllCommand = new RelayCommand(CheckOutAll);
            CheckInAllCommand = new RelayCommand(CheckInAll);
            ViewMissingCommand = new RelayCommand(ViewMissing);
            ViewTransactionsCommand = new RelayCommand(ViewTransactions);

            LoadSceneOperations();
        }

        public ObservableCollection<ISceneOperation> SceneOperations { get; }

        public int TotalEquipmentCount => SceneOperations.OfType<SceneOperationViewModel>().Sum(s => s.ActorEquipmentGroups.Sum(a => a.AssetItems.Count));
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

            if (_show?.Scenes == null) return;

            // Generate scene transitions first
            _show.GenerateSceneTransitions(_allProducts);

            var orderedScenes = _show.Scenes.OrderBy(s => s.SceneNumber).ToList();

            for (int i = 0; i < orderedScenes.Count; i++)
            {
                var scene = orderedScenes[i];
                var sceneOperation = new SceneOperationViewModel(scene, _show, _allProducts, this);
                SceneOperations.Add(sceneOperation);

                // Add transition after each scene (except the last one)
                if (i < orderedScenes.Count - 1)
                {
                    var transition = _show.SceneTransitions.FirstOrDefault(t =>
                        t.FromSceneNumber == scene.SceneNumber);

                    if (transition != null && transition.Actions.Any())
                    {
                        var transitionViewModel = new SceneTransitionViewModel(transition, _show);
                        SceneOperations.Add(new TransitionOperationViewModel(transitionViewModel));
                    }
                }
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

            // Refresh scene counts by calling their public refresh methods
            foreach (var sceneOp in SceneOperations.OfType<SceneOperationViewModel>())
            {
                sceneOp.RefreshSceneCounts();
            }
        }

        // Public method to refresh main counts only
        public void RefreshMainCounts()
        {
            OnPropertyChanged(nameof(TotalEquipmentCount));
            OnPropertyChanged(nameof(TotalCheckedOutCount));
            OnPropertyChanged(nameof(TotalCheckedInCount));
            OnPropertyChanged(nameof(TotalAvailableCount));
            OnPropertyChanged(nameof(TotalMissingCount));
        }

        // Public method to refresh all asset buttons
        public void RefreshAllAssetButtons()
        {
            foreach (var sceneOp in SceneOperations.OfType<SceneOperationViewModel>())
            {
                foreach (var actorGroup in sceneOp.ActorEquipmentGroups)
                {
                    foreach (var assetItem in actorGroup.AssetItems)
                    {
                        assetItem.RefreshButtonProperties();
                    }
                }
            }
        }

        private void CheckOutAll()
        {
            foreach (var sceneOp in SceneOperations.OfType<SceneOperationViewModel>())
            {
                sceneOp.CheckOutAll();
            }
        }

        private void CheckInAll()
        {
            foreach (var sceneOp in SceneOperations.OfType<SceneOperationViewModel>())
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

    public class SceneOperationViewModel : BaseViewModel, ISceneOperation
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
        public int SceneCheckedOutCount => ActorEquipmentGroups.Sum(a => a.AssetItems.Count(i => i.Status == EquipmentStatus.CheckedOut && i.IsAssignedToThisActor));
        public int SceneCheckedInCount => ActorEquipmentGroups.Sum(a => a.AssetItems.Count(i => i.Status == EquipmentStatus.CheckedIn));

        public ICommand CheckOutAllSceneCommand { get; }
        public ICommand CheckInAllSceneCommand { get; }

        private void LoadActorEquipmentGroups()
        {
            ActorEquipmentGroups.Clear();

            if (_scene.Allocations == null || !_scene.Allocations.Any()) return;

            // Group allocations by actor
            var actorGroups = _scene.Allocations.GroupBy(a => a.ActorId);

            foreach (var actorGroup in actorGroups)
            {
                var actorId = actorGroup.Key;
                var actor = _show.Cast.FirstOrDefault(c => c.Id == actorId);

                if (actor == null) continue;

                // Create asset items for each allocation
                var assetItems = new List<AssetItemViewModel>();

                foreach (var allocation in actorGroup)
                {
                    var product = _allProducts.FirstOrDefault(p => p.Id == allocation.ProductId);
                    if (product == null) continue;

                    // Get or create shared asset status for this physical asset
                    var assetStatus = GetOrCreateSharedAssetStatus(allocation.ProductId, allocation.AssetInfo ?? "");

                    // Create the allocation with assets object
                    var allocationWithAssets = new AllocationWithAssets
                    {
                        AllocationId = allocation.Id,
                        ActorId = allocation.ActorId,
                        ProductId = allocation.ProductId,
                        SceneId = _scene.Id,
                        AssetNumber = allocation.AssetInfo ?? "",
                        Actor = actor,
                        Product = product,
                        Scene = _scene,
                        AssetStatus = assetStatus
                    };

                    var assetItem = new AssetItemViewModel(allocationWithAssets, _show, null, _allProducts);
                    assetItems.Add(assetItem);

                    // Add accessories for this product
                    if (product.Accessories != null && product.Accessories.Any())
                    {
                        foreach (var accessory in product.Accessories)
                        {
                            // Create a unique asset number for this accessory instance
                            var accessoryAssetNumber = $"ACC_{accessory.Id:N}_{allocation.AssetInfo}";

                            var accessoryStatus = GetOrCreateSharedAssetStatus(accessory.Id, accessoryAssetNumber);
                            var accessoryAllocation = new AllocationWithAssets
                            {
                                AllocationId = allocation.Id,
                                ActorId = allocation.ActorId,
                                ProductId = accessory.Id,
                                SceneId = _scene.Id,
                                AssetNumber = accessoryAssetNumber,
                                Actor = actor,
                                Product = accessory,
                                Scene = _scene,
                                AssetStatus = accessoryStatus
                            };

                            var accessoryItem = new AssetItemViewModel(accessoryAllocation, _show, null, _allProducts);
                            assetItems.Add(accessoryItem);
                        }
                    }
                }

                if (assetItems.Any())
                {
                    var actorEquipmentGroup = new ActorEquipmentGroupViewModel(actor, assetItems, _show, _parentViewModel);
                    ActorEquipmentGroups.Add(actorEquipmentGroup);
                }
            }
        }

        private AssetStatus GetOrCreateSharedAssetStatus(Guid productId, string assetNumber)
        {
            // Look for existing asset status with the same product ID and asset number
            var existing = _show.AssetStatuses.FirstOrDefault(a =>
                a.ProductId == productId &&
                a.AssetNumber == assetNumber);

            if (existing != null)
            {
                return existing;
            }

            // Create new shared asset status
            var newStatus = new AssetStatus
            {
                ProductId = productId,
                AssetNumber = assetNumber,
                Status = EquipmentStatus.Available
            };

            _show.AssetStatuses.Add(newStatus);
            return newStatus;
        }

        public void RefreshCounts()
        {
            OnPropertyChanged(nameof(SceneEquipmentCount));
            OnPropertyChanged(nameof(SceneCheckedOutCount));
            OnPropertyChanged(nameof(SceneCheckedInCount));
        }

        // Public method to refresh just the scene counts without circular calls
        public void RefreshSceneCounts()
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
            _parentViewModel.RefreshCounts();
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
            _parentViewModel.RefreshCounts();
        }
    }

    public class ActorEquipmentGroupViewModel : BaseViewModel
    {
        private readonly Show _show;
        internal readonly object _parentViewModel;

        public ActorEquipmentGroupViewModel(Actor actor, List<AssetItemViewModel> assetItems, Show show, object parentViewModel)
        {
            Actor = actor;
            _show = show;
            _parentViewModel = parentViewModel;

            AssetItems = new ObservableCollection<AssetItemViewModel>(assetItems);

            // Subscribe to changes in asset items
            foreach (var item in AssetItems)
            {
                item.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(AssetItemViewModel.Status))
                    {
                        // Only refresh our own counts, don't trigger parent refresh to avoid circular calls
                        // The parent will be notified through RefreshCounts when needed
                    }
                };
            }
        }

        public Actor Actor { get; }
        public ObservableCollection<AssetItemViewModel> AssetItems { get; }

        public void RefreshCounts()
        {
            // Only refresh the parent counts, don't create circular calls
            if (_parentViewModel is SceneBySceneLiveOperationsViewModel sceneByScene)
            {
                // Call the public refresh method instead of accessing protected members
                sceneByScene.RefreshMainCounts();
            }
        }

        // Public method to refresh just this group's properties
        public void RefreshGroupCounts()
        {
            // Can call our own protected method
            OnPropertyChanged(nameof(AssetItems));
        }
    }

    public class AssetItemViewModel : BaseViewModel
    {
        internal readonly AllocationWithAssets _allocation;
        private readonly Show _show;
        private readonly List<Product>? _allProducts;
        private readonly ActorEquipmentGroupViewModel? _parentGroup;

        public AssetItemViewModel(AllocationWithAssets allocation, Show show, ActorEquipmentGroupViewModel? parentGroup, List<Product>? allProducts = null)
        {
            _allocation = allocation;
            _show = show;
            _parentGroup = parentGroup;
            _allProducts = allProducts;

            ToggleStatusCommand = new RelayCommand(ToggleStatus);

            // Subscribe to asset status changes
            if (_allocation.AssetStatus != null)
            {
                _allocation.AssetStatus.PropertyChanged += OnAssetStatusChanged;
            }
        }

        private void OnAssetStatusChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AssetStatus.Status) ||
                e.PropertyName == nameof(AssetStatus.CurrentlyAssignedToActorId))
            {
                // Force refresh of all properties
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(ButtonText));
                OnPropertyChanged(nameof(ButtonColor));
                OnPropertyChanged(nameof(CanInteract));
                OnPropertyChanged(nameof(CanCheckOut));
                OnPropertyChanged(nameof(CanCheckIn));
                OnPropertyChanged(nameof(IsAssignedToThisActor));

                // Only refresh parent counts, don't trigger full refresh to avoid circular calls
                _parentGroup?.RefreshCounts();
            }
        }

        public string ProductName => _allocation.Product?.Name ?? "Unknown";
        public EquipmentStatus Status => _allocation.AssetStatus?.Status ?? EquipmentStatus.Available;

        // Check if this asset is currently assigned to this actor
        public bool IsAssignedToThisActor =>
            _allocation.AssetStatus?.CurrentlyAssignedToActorId == _allocation.ActorId;

        // Check if this asset is available for checkout by this actor
        public bool CanCheckOut =>
            _allocation.AssetStatus?.IsAvailable == true;

        // Check if this asset can be checked in by this actor
        public bool CanCheckIn =>
            _allocation.AssetStatus?.IsCheckedOut == true && IsAssignedToThisActor;

        // Check if user can interact with this button
        public bool CanInteract => CanCheckOut || CanCheckIn ||
            Status == EquipmentStatus.Missing || Status == EquipmentStatus.Damaged;

        // Clear, descriptive button text that shows CURRENT STATE → ACTION
        public string ButtonText
        {
            get
            {
                switch (Status)
                {
                    case EquipmentStatus.Available:
                        return "📦 Ready → Check Out";

                    case EquipmentStatus.CheckedOut when IsAssignedToThisActor:
                        return "✅ With Actor → Check In";

                    case EquipmentStatus.CheckedOut when !IsAssignedToThisActor:
                        var currentActor = _show.Cast.FirstOrDefault(a => a.Id == _allocation.AssetStatus?.CurrentlyAssignedToActorId);
                        var actorName = currentActor?.DisplayName ?? "Another Actor";
                        return $"🔒 With {actorName}";

                    case EquipmentStatus.CheckedIn:
                        return "🏠 Returned → Check Out";

                    case EquipmentStatus.Missing:
                        return "❌ Missing → Mark Found";

                    case EquipmentStatus.Damaged:
                        return "⚠️ Damaged → Mark Repaired";

                    default:
                        return "❓ Unknown State";
                }
            }
        }

        // Color coding that matches the button text meaning
        public string ButtonColor
        {
            get
            {
                switch (Status)
                {
                    case EquipmentStatus.Available:
                        return "#2196F3"; // Blue - ready to go

                    case EquipmentStatus.CheckedOut when IsAssignedToThisActor:
                        return "#4CAF50"; // Green - success, you have it

                    case EquipmentStatus.CheckedOut when !IsAssignedToThisActor:
                        return "#FF9800"; // Orange - locked/unavailable

                    case EquipmentStatus.CheckedIn:
                        return "#2196F3"; // Blue - ready to check out again

                    case EquipmentStatus.Missing:
                        return "#F44336"; // Red - problem

                    case EquipmentStatus.Damaged:
                        return "#E91E63"; // Pink - problem

                    default:
                        return "#9E9E9E"; // Gray - unknown
                }
            }
        }

        public ICommand ToggleStatusCommand { get; }

        private void ToggleStatus()
        {
            if (!CanInteract) return;

            switch (Status)
            {
                case EquipmentStatus.Available:
                    CheckOut();
                    break;
                case EquipmentStatus.CheckedOut when IsAssignedToThisActor:
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
            if (_allocation.AssetStatus != null && CanCheckOut)
            {
                _show.CheckOutEquipment(_allocation.AssetStatus.Id, _allocation.ActorId, _allocation.SceneId);
                RefreshAllSceneOperations();
            }
        }

        public void CheckIn()
        {
            if (_allocation.AssetStatus != null && CanCheckIn)
            {
                _show.CheckInEquipment(_allocation.AssetStatus.Id, _allocation.SceneId);
                RefreshAllSceneOperations();
            }
        }

        private void RefreshAllSceneOperations()
        {
            // Refresh parent group counts
            _parentGroup?.RefreshCounts();

            // Force property change notifications on this item only
            RefreshButtonProperties();

            // Refresh the main view model counts and all buttons
            if (_parentGroup?._parentViewModel is SceneBySceneLiveOperationsViewModel mainViewModel)
            {
                mainViewModel.RefreshMainCounts();
                mainViewModel.RefreshAllAssetButtons();

                // Refresh scene counts
                foreach (var sceneOp in mainViewModel.SceneOperations.OfType<SceneOperationViewModel>())
                {
                    sceneOp.RefreshSceneCounts();
                }
            }
        }

        // Public method to refresh button properties
        public void RefreshButtonProperties()
        {
            OnPropertyChanged(nameof(ButtonText));
            OnPropertyChanged(nameof(ButtonColor));
            OnPropertyChanged(nameof(CanInteract));
        }

        private void MarkFound()
        {
            if (_allocation.AssetStatus != null)
            {
                _allocation.AssetStatus.Status = EquipmentStatus.Available;
                RefreshAllSceneOperations();
            }
        }

        private void MarkRepaired()
        {
            if (_allocation.AssetStatus != null)
            {
                _allocation.AssetStatus.Status = EquipmentStatus.Available;
                RefreshAllSceneOperations();
            }
        }

        public void MarkAsDamaged()
        {
            if (_allocation.AssetStatus != null)
            {
                _allocation.AssetStatus.Status = EquipmentStatus.Damaged;
                RefreshAllSceneOperations();
            }
        }

        public void MarkAsLost()
        {
            if (_allocation.AssetStatus != null)
            {
                _allocation.AssetStatus.Status = EquipmentStatus.Missing;
                RefreshAllSceneOperations();
            }
        }
    }

    // Interface for scene operations (scenes and transitions)
    public interface ISceneOperation
    {
    }

    // Wrapper for transition operations
    public class TransitionOperationViewModel : BaseViewModel, ISceneOperation
    {
        public TransitionOperationViewModel(SceneTransitionViewModel transitionViewModel)
        {
            TransitionViewModel = transitionViewModel;
        }

        public SceneTransitionViewModel TransitionViewModel { get; }
    }
}