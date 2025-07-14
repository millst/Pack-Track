// ViewModels/SceneBySceneLiveOperationsViewModel.cs - Complete clean version
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

            System.Diagnostics.Debug.WriteLine($"=== LoadSceneOperations START ===");
            System.Diagnostics.Debug.WriteLine($"Total scenes: {_show.Scenes.Count}");

            // Generate scene transitions first
            _show.GenerateSceneTransitions(_allProducts);
            System.Diagnostics.Debug.WriteLine($"Generated {_show.SceneTransitions.Count} transitions");

            var orderedScenes = _show.Scenes.OrderBy(s => s.SceneNumber).ToList();

            for (int i = 0; i < orderedScenes.Count; i++)
            {
                var scene = orderedScenes[i];
                System.Diagnostics.Debug.WriteLine($"Processing Scene {scene.SceneNumber}: {scene.Name}");

                var sceneOperation = new SceneOperationViewModel(scene, _show, _allProducts, this);
                SceneOperations.Add(sceneOperation);

                // Add transition after each scene (except the last one)
                if (i < orderedScenes.Count - 1)
                {
                    var nextScene = orderedScenes[i + 1];
                    System.Diagnostics.Debug.WriteLine($"Looking for transition from Scene {scene.SceneNumber} to Scene {nextScene.SceneNumber}");

                    var transition = _show.SceneTransitions.FirstOrDefault(t =>
                        t.FromSceneNumber == scene.SceneNumber && t.ToSceneNumber == nextScene.SceneNumber);

                    if (transition != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Found transition with {transition.Actions.Count} actions");
                        foreach (var action in transition.Actions)
                        {
                            System.Diagnostics.Debug.WriteLine($"  Action: {action.Type} - {action.Description}");
                        }

                        if (transition.Actions.Any())
                        {
                            var transitionViewModel = new SceneTransitionViewModel(transition, _show);
                            SceneOperations.Add(new TransitionOperationViewModel(transitionViewModel));
                            System.Diagnostics.Debug.WriteLine($"Added transition to SceneOperations");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Transition has no actions, not adding to display");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"No transition found, creating manual transition");

                        // Let's debug what allocations exist
                        System.Diagnostics.Debug.WriteLine($"Scene {scene.SceneNumber} allocations: {scene.Allocations.Count}");
                        foreach (var alloc in scene.Allocations)
                        {
                            var product = _allProducts.FirstOrDefault(p => p.Id == alloc.ProductId);
                            var actor = _show.Cast.FirstOrDefault(a => a.Id == alloc.ActorId);
                            System.Diagnostics.Debug.WriteLine($"  {product?.Name} ({alloc.AssetInfo}) -> {actor?.DisplayName}");
                        }

                        System.Diagnostics.Debug.WriteLine($"Scene {nextScene.SceneNumber} allocations: {nextScene.Allocations.Count}");
                        foreach (var alloc in nextScene.Allocations)
                        {
                            var product = _allProducts.FirstOrDefault(p => p.Id == alloc.ProductId);
                            var actor = _show.Cast.FirstOrDefault(a => a.Id == alloc.ActorId);
                            System.Diagnostics.Debug.WriteLine($"  {product?.Name} ({alloc.AssetInfo}) -> {actor?.DisplayName}");
                        }

                        // Create a manual transition
                        var manualTransition = CreateManualTransition(scene, nextScene);
                        if (manualTransition != null && manualTransition.Actions.Any())
                        {
                            _show.SceneTransitions.Add(manualTransition);
                            var transitionViewModel = new SceneTransitionViewModel(manualTransition, _show);
                            SceneOperations.Add(new TransitionOperationViewModel(transitionViewModel));
                            System.Diagnostics.Debug.WriteLine($"Created and added manual transition with {manualTransition.Actions.Count} actions");
                        }
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"Final SceneOperations count: {SceneOperations.Count}");
            RefreshCounts();
        }

        private SceneTransition? CreateManualTransition(Scene fromScene, Scene toScene)
        {
            var transition = new SceneTransition
            {
                FromSceneNumber = fromScene.SceneNumber,
                ToSceneNumber = toScene.SceneNumber
            };

            var fromAllocations = fromScene.Allocations.ToList();
            var toAllocations = toScene.Allocations.ToList();

            // Find equipment that needs to be checked in (in fromScene but not in toScene)
            foreach (var fromAllocation in fromAllocations)
            {
                var product = _allProducts.FirstOrDefault(p => p.Id == fromAllocation.ProductId);
                if (product == null) continue;

                var stillNeeded = toAllocations.Any(a =>
                    a.ProductId == fromAllocation.ProductId &&
                    a.AssetInfo == fromAllocation.AssetInfo);

                if (!stillNeeded)
                {
                    var fromActor = _show.Cast.FirstOrDefault(a => a.Id == fromAllocation.ActorId);
                    if (fromActor != null)
                    {
                        transition.Actions.Add(new TransitionAction
                        {
                            Type = TransitionType.CheckIn,
                            ProductId = fromAllocation.ProductId,
                            AssetNumber = fromAllocation.AssetInfo ?? "",
                            FromActorId = fromAllocation.ActorId,
                            ToActorId = Guid.Empty,
                            Product = product,
                            FromActor = fromActor,
                            ToActor = null
                        });
                    }
                }
            }

            // Find equipment that needs to be checked out or transferred
            foreach (var toAllocation in toAllocations)
            {
                var product = _allProducts.FirstOrDefault(p => p.Id == toAllocation.ProductId);
                if (product == null) continue;

                var wasWithSameActor = fromAllocations.Any(a =>
                    a.ProductId == toAllocation.ProductId &&
                    a.AssetInfo == toAllocation.AssetInfo &&
                    a.ActorId == toAllocation.ActorId);

                if (wasWithSameActor) continue; // No action needed

                var wasWithDifferentActor = fromAllocations.FirstOrDefault(a =>
                    a.ProductId == toAllocation.ProductId &&
                    a.AssetInfo == toAllocation.AssetInfo &&
                    a.ActorId != toAllocation.ActorId);

                if (wasWithDifferentActor != null)
                {
                    // Transfer between actors
                    var fromActor = _show.Cast.FirstOrDefault(a => a.Id == wasWithDifferentActor.ActorId);
                    var toActor = _show.Cast.FirstOrDefault(a => a.Id == toAllocation.ActorId);

                    if (fromActor != null && toActor != null)
                    {
                        transition.Actions.Add(new TransitionAction
                        {
                            Type = TransitionType.Transfer,
                            ProductId = toAllocation.ProductId,
                            AssetNumber = toAllocation.AssetInfo ?? "",
                            FromActorId = wasWithDifferentActor.ActorId,
                            ToActorId = toAllocation.ActorId,
                            Product = product,
                            FromActor = fromActor,
                            ToActor = toActor
                        });
                    }
                }
                else
                {
                    // New checkout
                    var toActor = _show.Cast.FirstOrDefault(a => a.Id == toAllocation.ActorId);
                    if (toActor != null)
                    {
                        transition.Actions.Add(new TransitionAction
                        {
                            Type = TransitionType.CheckOut,
                            ProductId = toAllocation.ProductId,
                            AssetNumber = toAllocation.AssetInfo ?? "",
                            FromActorId = Guid.Empty,
                            ToActorId = toAllocation.ActorId,
                            Product = product,
                            FromActor = null,
                            ToActor = toActor
                        });
                    }
                }
            }

            return transition.Actions.Any() ? transition : null;
        }

        public void RefreshCounts()
        {
            OnPropertyChanged(nameof(TotalEquipmentCount));
            OnPropertyChanged(nameof(TotalCheckedOutCount));
            OnPropertyChanged(nameof(TotalCheckedInCount));
            OnPropertyChanged(nameof(TotalAvailableCount));
            OnPropertyChanged(nameof(TotalMissingCount));

            foreach (var sceneOp in SceneOperations.OfType<SceneOperationViewModel>())
            {
                sceneOp.RefreshSceneCounts();
            }
        }

        public void RefreshMainCounts()
        {
            OnPropertyChanged(nameof(TotalEquipmentCount));
            OnPropertyChanged(nameof(TotalCheckedOutCount));
            OnPropertyChanged(nameof(TotalCheckedInCount));
            OnPropertyChanged(nameof(TotalAvailableCount));
            OnPropertyChanged(nameof(TotalMissingCount));
        }

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

            var actorGroups = _scene.Allocations.GroupBy(a => a.ActorId);

            foreach (var actorGroup in actorGroups)
            {
                var actorId = actorGroup.Key;
                var actor = _show.Cast.FirstOrDefault(c => c.Id == actorId);

                if (actor == null) continue;

                var assetItems = new List<AssetItemViewModel>();

                foreach (var allocation in actorGroup)
                {
                    var product = _allProducts.FirstOrDefault(p => p.Id == allocation.ProductId);
                    if (product == null) continue;

                    var assetStatus = GetOrCreateSharedAssetStatus(allocation.ProductId, allocation.AssetInfo ?? "");

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

                    // Add accessories
                    if (product.Accessories != null && product.Accessories.Any())
                    {
                        foreach (var accessory in product.Accessories)
                        {
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
            var existing = _show.AssetStatuses.FirstOrDefault(a =>
                a.ProductId == productId && a.AssetNumber == assetNumber);

            if (existing != null) return existing;

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
        }

        public Actor Actor { get; }
        public ObservableCollection<AssetItemViewModel> AssetItems { get; }

        public void RefreshCounts()
        {
            if (_parentViewModel is SceneBySceneLiveOperationsViewModel sceneByScene)
            {
                sceneByScene.RefreshMainCounts();
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
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(ButtonText));
                OnPropertyChanged(nameof(ButtonColor));
                OnPropertyChanged(nameof(CanInteract));
                OnPropertyChanged(nameof(CanCheckOut));
                OnPropertyChanged(nameof(CanCheckIn));
                OnPropertyChanged(nameof(IsAssignedToThisActor));
                _parentGroup?.RefreshCounts();
            }
        }

        public string ProductName => _allocation.Product?.Name ?? "Unknown";
        public EquipmentStatus Status => _allocation.AssetStatus?.Status ?? EquipmentStatus.Available;
        public bool IsAssignedToThisActor => _allocation.AssetStatus?.CurrentlyAssignedToActorId == _allocation.ActorId;
        public bool CanCheckOut => _allocation.AssetStatus?.IsAvailable == true;
        public bool CanCheckIn => _allocation.AssetStatus?.IsCheckedOut == true && IsAssignedToThisActor;
        public bool CanInteract => CanCheckOut || CanCheckIn || Status == EquipmentStatus.Missing || Status == EquipmentStatus.Damaged;

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

        public string ButtonColor
        {
            get
            {
                switch (Status)
                {
                    case EquipmentStatus.Available:
                        return "#2196F3";
                    case EquipmentStatus.CheckedOut when IsAssignedToThisActor:
                        return "#4CAF50";
                    case EquipmentStatus.CheckedOut when !IsAssignedToThisActor:
                        return "#FF9800";
                    case EquipmentStatus.CheckedIn:
                        return "#2196F3";
                    case EquipmentStatus.Missing:
                        return "#F44336";
                    case EquipmentStatus.Damaged:
                        return "#E91E63";
                    default:
                        return "#9E9E9E";
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
            _parentGroup?.RefreshCounts();
            RefreshButtonProperties();

            if (_parentGroup?._parentViewModel is SceneBySceneLiveOperationsViewModel mainViewModel)
            {
                mainViewModel.RefreshMainCounts();
                mainViewModel.RefreshAllAssetButtons();

                foreach (var sceneOp in mainViewModel.SceneOperations.OfType<SceneOperationViewModel>())
                {
                    sceneOp.RefreshSceneCounts();
                }
            }
        }

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

    public interface ISceneOperation
    {
    }

    public class TransitionOperationViewModel : BaseViewModel, ISceneOperation
    {
        public TransitionOperationViewModel(SceneTransitionViewModel transitionViewModel)
        {
            TransitionViewModel = transitionViewModel;
        }

        public SceneTransitionViewModel TransitionViewModel { get; }
    }
}