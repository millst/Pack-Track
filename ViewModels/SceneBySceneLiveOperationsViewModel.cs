// ViewModels/SceneBySceneLiveOperationsViewModel.cs - Debug version with better asset handling
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

            System.Diagnostics.Debug.WriteLine($"=== SceneBySceneLiveOperationsViewModel Constructor ===");
            System.Diagnostics.Debug.WriteLine($"Show: {show?.Name}");
            System.Diagnostics.Debug.WriteLine($"Products count: {allProducts?.Count}");
            System.Diagnostics.Debug.WriteLine($"Scenes count: {show?.Scenes?.Count}");

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
            System.Diagnostics.Debug.WriteLine($"=== LoadSceneOperations START ===");
            SceneOperations.Clear();

            if (_show?.Scenes == null)
            {
                System.Diagnostics.Debug.WriteLine("No scenes found!");
                return;
            }

            foreach (var scene in _show.Scenes.OrderBy(s => s.SceneNumber))
            {
                System.Diagnostics.Debug.WriteLine($"Processing scene: {scene.Name} (ID: {scene.Id})");
                System.Diagnostics.Debug.WriteLine($"Scene allocations count: {scene.Allocations?.Count ?? 0}");

                var sceneOperation = new SceneOperationViewModel(scene, _show, _allProducts, this);
                SceneOperations.Add(sceneOperation);
            }

            System.Diagnostics.Debug.WriteLine($"Created {SceneOperations.Count} scene operations");
            RefreshCounts();
        }

        public void RefreshCounts()
        {
            OnPropertyChanged(nameof(TotalEquipmentCount));
            OnPropertyChanged(nameof(TotalCheckedOutCount));
            OnPropertyChanged(nameof(TotalCheckedInCount));
            OnPropertyChanged(nameof(TotalAvailableCount));
            OnPropertyChanged(nameof(TotalMissingCount));

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

            System.Diagnostics.Debug.WriteLine($"=== SceneOperationViewModel Constructor for {scene.Name} ===");
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
            System.Diagnostics.Debug.WriteLine($"=== LoadActorEquipmentGroups for {_scene.Name} ===");
            ActorEquipmentGroups.Clear();

            if (_scene.Allocations == null || !_scene.Allocations.Any())
            {
                System.Diagnostics.Debug.WriteLine("No allocations found for this scene");
                return;
            }

            // Group allocations by actor
            var actorGroups = _scene.Allocations.GroupBy(a => a.ActorId);
            System.Diagnostics.Debug.WriteLine($"Found {actorGroups.Count()} actor groups");

            foreach (var actorGroup in actorGroups)
            {
                var actorId = actorGroup.Key;
                var actor = _show.Cast.FirstOrDefault(c => c.Id == actorId);

                if (actor == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Actor not found for ID: {actorId}");
                    continue;
                }

                System.Diagnostics.Debug.WriteLine($"Processing actor: {actor.DisplayName}");

                // Create asset items for each allocation
                var assetItems = new List<AssetItemViewModel>();

                foreach (var allocation in actorGroup)
                {
                    var product = _allProducts.FirstOrDefault(p => p.Id == allocation.ProductId);
                    if (product == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Product not found for ID: {allocation.ProductId}");
                        continue;
                    }

                    System.Diagnostics.Debug.WriteLine($"  Processing product: {product.Name}, Asset Info: {allocation.AssetInfo}");

                    // Create asset status if it doesn't exist
                    var assetStatus = GetOrCreateAssetStatus(allocation.ProductId, allocation.AssetInfo ?? "");

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

                    var assetItem = new AssetItemViewModel(allocationWithAssets, _show, null);
                    assetItems.Add(assetItem);

                    // Add accessories for this product
                    if (product.Accessories != null && product.Accessories.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"    Adding {product.Accessories.Count} accessories");
                        foreach (var accessory in product.Accessories)
                        {
                            // Create simpler, readable asset number for accessories
                            // Format: ACC_[AccessoryName]_[ActorId]_[MainProductAssetInfo]
                            var simpleAccessoryAssetNumber = $"ACC_{accessory.Name}_{allocation.ActorId:N}_{allocation.AssetInfo}";

                            var accessoryStatus = GetOrCreateAssetStatus(accessory.Id, simpleAccessoryAssetNumber);
                            var accessoryAllocation = new AllocationWithAssets
                            {
                                AllocationId = allocation.Id,
                                ActorId = allocation.ActorId,
                                ProductId = accessory.Id,
                                SceneId = _scene.Id,
                                AssetNumber = simpleAccessoryAssetNumber,
                                Actor = actor,
                                Product = accessory,
                                Scene = _scene,
                                AssetStatus = accessoryStatus
                            };

                            var accessoryItem = new AssetItemViewModel(accessoryAllocation, _show, null);
                            assetItems.Add(accessoryItem);
                            System.Diagnostics.Debug.WriteLine($"      Added accessory: {accessory.Name} with asset number: {simpleAccessoryAssetNumber}");
                        }
                    }
                }

                if (assetItems.Any())
                {
                    var actorEquipmentGroup = new ActorEquipmentGroupViewModel(actor, assetItems, _show, _parentViewModel);
                    ActorEquipmentGroups.Add(actorEquipmentGroup);
                    System.Diagnostics.Debug.WriteLine($"  Created group with {assetItems.Count} asset items");
                }
            }

            System.Diagnostics.Debug.WriteLine($"Total actor equipment groups created: {ActorEquipmentGroups.Count}");
        }

        private AssetStatus GetOrCreateAssetStatus(Guid productId, string assetNumber)
        {
            // For accessories, we need to create unique asset statuses per allocation
            // This prevents shared status between different actors/scenes
            var existing = _show.AssetStatuses.FirstOrDefault(a =>
                a.ProductId == productId &&
                a.AssetNumber == assetNumber);

            if (existing != null)
            {
                System.Diagnostics.Debug.WriteLine($"Found existing asset status for {productId} - {assetNumber}");
                return existing;
            }

            var newStatus = new AssetStatus
            {
                ProductId = productId,
                AssetNumber = assetNumber,
                Status = EquipmentStatus.Available
            };

            _show.AssetStatuses.Add(newStatus);
            System.Diagnostics.Debug.WriteLine($"Created new asset status for {productId} - {assetNumber}");
            return newStatus;
        }

        public void RefreshCounts()
        {
            OnPropertyChanged(nameof(SceneEquipmentCount));
            OnPropertyChanged(nameof(SceneCheckedOutCount));
            OnPropertyChanged(nameof(SceneCheckedInCount));

            foreach (var group in ActorEquipmentGroups)
            {
                group.UpdateGroupStatus();
            }
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
        internal readonly object _parentViewModel; // Changed to internal so AssetItemViewModel can access it
        private EquipmentStatus _groupStatus;

        public ActorEquipmentGroupViewModel(Actor actor, List<AssetItemViewModel> assetItems, Show show, object parentViewModel)
        {
            Actor = actor;
            _show = show;
            _parentViewModel = parentViewModel;

            AssetItems = new ObservableCollection<AssetItemViewModel>(assetItems);

            System.Diagnostics.Debug.WriteLine($"=== ActorEquipmentGroupViewModel for {actor.DisplayName} ===");
            System.Diagnostics.Debug.WriteLine($"Asset items count: {assetItems.Count}");

            UpdateGroupStatus();

            // Subscribe to changes in asset items
            foreach (var item in AssetItems)
            {
                item.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(AssetItemViewModel.Status))
                    {
                        UpdateGroupStatus();
                        RefreshCounts();
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
            EquipmentStatus.Available => "#9E9E9E",      // Gray
            EquipmentStatus.CheckedOut => "#FF9800",     // Orange  
            EquipmentStatus.CheckedIn => "#4CAF50",      // Green
            EquipmentStatus.Missing => "#F44336",        // Red
            EquipmentStatus.Damaged => "#E91E63",        // Pink
            _ => "#9E9E9E"                               // Gray default
        };

        public string StatusText => GroupStatus switch
        {
            EquipmentStatus.Available => "Available",
            EquipmentStatus.CheckedOut => "Checked Out",
            EquipmentStatus.CheckedIn => "Checked In",      // This should show green
            EquipmentStatus.Missing => "Missing Items",
            EquipmentStatus.Damaged => "Damaged Items",
            _ => "Mixed Status"
        };

        public void UpdateGroupStatus()
        {
            if (!AssetItems.Any())
            {
                GroupStatus = EquipmentStatus.Available;
                return;
            }

            var statuses = AssetItems.Select(a => a.Status).ToList();
            var oldStatus = GroupStatus;

            System.Diagnostics.Debug.WriteLine($"=== UpdateGroupStatus for {Actor.DisplayName} ===");
            System.Diagnostics.Debug.WriteLine($"Individual statuses: {string.Join(", ", statuses)}");

            // If ANY item is missing or damaged, show that as priority
            if (statuses.Any(s => s == EquipmentStatus.Missing || s == EquipmentStatus.Damaged))
            {
                GroupStatus = statuses.Contains(EquipmentStatus.Missing) ? EquipmentStatus.Missing : EquipmentStatus.Damaged;
            }
            // If ANY item is checked out, show as checked out (Orange)
            else if (statuses.Any(s => s == EquipmentStatus.CheckedOut))
            {
                GroupStatus = EquipmentStatus.CheckedOut;
            }
            // If ALL items are checked in, show as checked in (Green)
            else if (statuses.All(s => s == EquipmentStatus.CheckedIn))
            {
                GroupStatus = EquipmentStatus.CheckedIn;
            }
            // Otherwise, show as available
            else
            {
                GroupStatus = EquipmentStatus.Available;
            }

            System.Diagnostics.Debug.WriteLine($"Group status changed from {oldStatus} to {GroupStatus}");

            OnPropertyChanged(nameof(GroupStatus));
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(StatusText));
        }

        public void RefreshCounts()
        {
            if (_parentViewModel is SceneBySceneLiveOperationsViewModel sceneByScene)
            {
                sceneByScene.RefreshCounts();
            }
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

            System.Diagnostics.Debug.WriteLine($"    Created AssetItemViewModel: {allocation.ProductDisplayName}");

            // Subscribe to asset status changes
            if (_allocation.AssetStatus != null)
            {
                _allocation.AssetStatus.PropertyChanged += OnAssetStatusChanged;
            }
        }

        private void OnAssetStatusChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AssetStatus.Status))
            {
                System.Diagnostics.Debug.WriteLine($"Asset status changed for {ProductName}: {Status}");

                // Force refresh of all properties
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(SimpleStatusColor));
                OnPropertyChanged(nameof(ActionButtonText));
                OnPropertyChanged(nameof(CanCheckOut));
                OnPropertyChanged(nameof(CanCheckIn));

                // Update parent group
                _parentGroup?.UpdateGroupStatus();
                _parentGroup?.RefreshCounts();
            }
        }

        public string ProductName => _allocation.ProductDisplayName;
        public EquipmentStatus Status => _allocation.AssetStatus?.Status ?? EquipmentStatus.Available;
        public bool CanCheckOut => _allocation.AssetStatus?.IsAvailable == true;
        public bool CanCheckIn => _allocation.AssetStatus?.IsCheckedOut == true &&
                                  _allocation.AssetStatus?.CurrentlyAssignedToActorId == _allocation.ActorId;

        // Check if this is an accessory (asset number starts with "ACC_")
        public bool IsAccessory => _allocation.AssetNumber?.StartsWith("ACC_") == true;

        public string StatusColor => Status switch
        {
            EquipmentStatus.Available => "#9E9E9E",
            EquipmentStatus.CheckedOut => "#FF9800",
            EquipmentStatus.CheckedIn => "#4CAF50",
            EquipmentStatus.Missing => "#F44336",
            EquipmentStatus.Damaged => "#E91E63",
            _ => "#9E9E9E"
        };

        // Simple color scheme: Green = checked in, Orange = checked out, Gray = available/other
        public string SimpleStatusColor => Status switch
        {
            EquipmentStatus.CheckedOut => "#FF9800", // Orange - when item is checked out
            EquipmentStatus.CheckedIn => "#4CAF50",  // Green - when item is checked in  
            EquipmentStatus.Missing => "#F44336",    // Red
            EquipmentStatus.Damaged => "#E91E63",    // Pink
            _ => "#9E9E9E"                           // Gray for available
        };

        public string ActionButtonText => Status switch
        {
            EquipmentStatus.Available => "Check Out",
            EquipmentStatus.CheckedOut => "Check In",  // If checked out, button says "Check In"
            EquipmentStatus.CheckedIn => "Check Out",  // If checked in, button says "Check Out"
            EquipmentStatus.Missing => "Found",
            EquipmentStatus.Damaged => "Repaired",
            _ => "Toggle"
        };

        public ICommand ToggleStatusCommand { get; }

        private void ToggleStatus()
        {
            System.Diagnostics.Debug.WriteLine($"ToggleStatus called for {ProductName}, current status: {Status}, IsAccessory: {IsAccessory}");
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
                System.Diagnostics.Debug.WriteLine($"Checking out {ProductName} (IsAccessory: {IsAccessory})");

                // Check out this item across all scenes where it's allocated
                CheckOutAcrossAllScenes();

                // If this is a main product (not accessory), also check out its accessories
                if (!IsAccessory)
                {
                    CheckOutAccessoriesForMainProduct();
                }

                // Force refresh of all UI elements
                RefreshAllSceneOperations();
            }
        }

        public void CheckIn()
        {
            if (_allocation.AssetStatus != null && _allocation.Scene != null)
            {
                System.Diagnostics.Debug.WriteLine($"Checking in {ProductName} (IsAccessory: {IsAccessory})");

                // Check in this item across all scenes where it's allocated
                CheckInAcrossAllScenes();

                // ASYMMETRIC LOGIC: When checking in main products, DO NOT automatically check in accessories
                // Users must manually check in each accessory
                // (This is different from check-out where accessories are automatically checked out)

                // Force refresh of all UI elements
                RefreshAllSceneOperations();
            }
        }

        private void RefreshAllSceneOperations()
        {
            // Trigger refresh of parent group and all scene operations
            _parentGroup?.RefreshCounts();

            // Also trigger refresh through the parent view model chain
            if (_parentGroup?._parentViewModel is SceneBySceneLiveOperationsViewModel mainViewModel)
            {
                System.Diagnostics.Debug.WriteLine("Refreshing all scene operations...");
                mainViewModel.RefreshCounts();

                // Force property change notifications on all asset items
                foreach (var sceneOp in mainViewModel.SceneOperations)
                {
                    foreach (var actorGroup in sceneOp.ActorEquipmentGroups)
                    {
                        foreach (var assetItem in actorGroup.AssetItems)
                        {
                            assetItem.OnPropertyChanged(nameof(Status));
                            assetItem.OnPropertyChanged(nameof(StatusColor));
                            assetItem.OnPropertyChanged(nameof(SimpleStatusColor));
                            assetItem.OnPropertyChanged(nameof(ActionButtonText));
                            assetItem.OnPropertyChanged(nameof(CanCheckOut));
                            assetItem.OnPropertyChanged(nameof(CanCheckIn));
                        }
                        actorGroup.UpdateGroupStatus();
                    }
                }
            }
        }

        private void CheckOutAcrossAllScenes()
        {
            // Find all scenes where this specific asset is allocated to this actor
            var relevantAllocations = FindRelevantAllocations();

            foreach (var sceneId in relevantAllocations)
            {
                _show.CheckOutEquipment(_allocation.AssetStatus.Id, _allocation.ActorId, sceneId);
            }
        }

        private void CheckInAcrossAllScenes()
        {
            // Find all scenes where this specific asset is allocated to this actor
            var relevantAllocations = FindRelevantAllocations();

            foreach (var sceneId in relevantAllocations)
            {
                _show.CheckInEquipment(_allocation.AssetStatus.Id, sceneId);
            }
        }

        private List<Guid> FindRelevantAllocations()
        {
            var relevantScenes = new List<Guid>();

            // For main products, find scenes where this product is allocated to this actor
            if (!IsAccessory)
            {
                foreach (var scene in _show.Scenes)
                {
                    var allocation = scene.Allocations.FirstOrDefault(a =>
                        a.ActorId == _allocation.ActorId &&
                        a.ProductId == _allocation.ProductId &&
                        a.AssetInfo == _allocation.AssetNumber);

                    if (allocation != null)
                    {
                        relevantScenes.Add(scene.Id);
                    }
                }
            }
            else
            {
                // For accessories, find scenes where the parent product is allocated
                // Extract main product ID from accessory asset number: ACC_[MainProductId]_[AccessoryId]_[ActorId]_[SceneId]
                var parts = _allocation.AssetNumber?.Split('_');
                if (parts?.Length >= 2 && Guid.TryParse(parts[1], out var mainProductId))
                {
                    foreach (var scene in _show.Scenes)
                    {
                        var allocation = scene.Allocations.FirstOrDefault(a =>
                            a.ActorId == _allocation.ActorId &&
                            a.ProductId == mainProductId);

                        if (allocation != null)
                        {
                            relevantScenes.Add(scene.Id);
                        }
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"Found {relevantScenes.Count} relevant scenes for {ProductName}");
            return relevantScenes;
        }

        private void CheckOutAccessoriesForMainProduct()
        {
            System.Diagnostics.Debug.WriteLine($"Checking out accessories for main product: {ProductName}");

            // Find all accessories for this main product across all relevant scenes
            var mainProductAssetInfo = _allocation.AssetNumber;
            var actorId = _allocation.ActorId;

            // Look for accessories with asset numbers that match: ACC_[AccessoryName]_[ActorId]_[MainProductAssetInfo]
            var accessoryStatuses = _show.AssetStatuses.Where(a =>
                a.AssetNumber.StartsWith("ACC_") &&
                a.AssetNumber.Contains($"_{actorId:N}_") &&
                a.AssetNumber.EndsWith($"_{mainProductAssetInfo}")).ToList();

            foreach (var accessoryStatus in accessoryStatuses)
            {
                System.Diagnostics.Debug.WriteLine($"  Checking out accessory: {accessoryStatus.AssetNumber}");

                // Find all scenes where this accessory should be checked out
                var relevantScenes = FindRelevantAllocations();
                foreach (var sceneId in relevantScenes)
                {
                    _show.CheckOutEquipment(accessoryStatus.Id, actorId, sceneId);
                }
            }
        }

        private void CheckInAccessoriesForMainProduct()
        {
            System.Diagnostics.Debug.WriteLine($"Checking in accessories for main product: {ProductName}");

            // Find all accessories for this main product across all relevant scenes
            var mainProductId = _allocation.ProductId;
            var actorId = _allocation.ActorId;

            // Look for accessories with asset numbers that contain this main product ID
            var accessoryStatuses = _show.AssetStatuses.Where(a =>
                a.AssetNumber.StartsWith($"ACC_{mainProductId:N}") &&
                a.AssetNumber.Contains($"{actorId:N}")).ToList();

            foreach (var accessoryStatus in accessoryStatuses)
            {
                System.Diagnostics.Debug.WriteLine($"  Checking in accessory: {accessoryStatus.AssetNumber}");

                // Extract scene ID from the accessory asset number to check in from the right scene
                var parts = accessoryStatus.AssetNumber.Split('_');
                if (parts.Length >= 5 && Guid.TryParse(parts[4], out var sceneId))
                {
                    _show.CheckInEquipment(accessoryStatus.Id, sceneId);
                }
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

        public void MarkAsDamaged()
        {
            if (_allocation.AssetStatus != null)
            {
                _allocation.AssetStatus.Status = EquipmentStatus.Damaged;
            }
        }

        public void MarkAsLost()
        {
            if (_allocation.AssetStatus != null)
            {
                _allocation.AssetStatus.Status = EquipmentStatus.Missing;
            }
        }
    }
}