// Enhanced Models for Asset-Level Equipment Tracking with Shared Status
using Pack_Track.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Transactions;

namespace Pack_Track.Models
{
    // Enhanced Equipment Status at Asset Level
    public class AssetStatus : INotifyPropertyChanged
    {
        private EquipmentStatus _status = EquipmentStatus.Available;
        private Guid? _currentlyAssignedToActorId;
        private Guid? _currentSceneId;
        private DateTime? _lastStatusChange;

        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProductId { get; set; }
        public string AssetNumber { get; set; } = string.Empty;

        public EquipmentStatus Status
        {
            get => _status;
            set
            {
                if (SetProperty(ref _status, value))
                {
                    LastStatusChange = DateTime.Now;
                    OnPropertyChanged(nameof(IsAvailable));
                    OnPropertyChanged(nameof(IsCheckedOut));
                }
            }
        }

        public Guid? CurrentlyAssignedToActorId
        {
            get => _currentlyAssignedToActorId;
            set => SetProperty(ref _currentlyAssignedToActorId, value);
        }

        public Guid? CurrentSceneId
        {
            get => _currentSceneId;
            set => SetProperty(ref _currentSceneId, value);
        }

        public DateTime? LastStatusChange
        {
            get => _lastStatusChange;
            set => SetProperty(ref _lastStatusChange, value);
        }

        // Helper properties
        public bool IsAvailable => Status == EquipmentStatus.Available || Status == EquipmentStatus.CheckedIn;
        public bool IsCheckedOut => Status == EquipmentStatus.CheckedOut;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    // Enhanced Allocation with Asset-Specific Status
    public class AllocationWithAssets
    {
        public Guid AllocationId { get; set; }
        public Guid ActorId { get; set; }
        public Guid ProductId { get; set; }
        public Guid SceneId { get; set; }
        public string AssetNumber { get; set; } = string.Empty;

        // Navigation properties
        public Actor? Actor { get; set; }
        public Product? Product { get; set; }
        public Scene? Scene { get; set; }
        public AssetStatus? AssetStatus { get; set; }

        // Computed properties
        public string DisplayName => Actor?.DisplayName ?? "Unknown Actor";
        public string ProductDisplayName => string.IsNullOrEmpty(AssetNumber)
            ? Product?.Name ?? "Unknown Product"
            : $"{Product?.Name} ({AssetNumber})";
    }

    // Equipment Transaction Record for Audit Trail
    public class EquipmentTransaction : INotifyPropertyChanged
    {
        private string _notes = string.Empty;

        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AssetStatusId { get; set; }
        public Guid FromActorId { get; set; }
        public Guid ToActorId { get; set; }
        public Guid SceneId { get; set; }
        public EquipmentStatus FromStatus { get; set; }
        public EquipmentStatus ToStatus { get; set; }
        public DateTime TransactionTime { get; set; } = DateTime.Now;
        public TransactionType Type { get; set; }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        // Navigation properties
        public AssetStatus? AssetStatus { get; set; }
        public Actor? FromActor { get; set; }
        public Actor? ToActor { get; set; }
        public Scene? Scene { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public enum TransactionType
    {
        CheckOut,
        CheckIn,
        Transfer,
        MarkMissing,
        MarkDamaged,
        MarkFound,
        MarkRepaired
    }

    // Enhanced Show model with asset tracking
    public partial class Show
    {
        public List<AssetStatus> AssetStatuses { get; set; } = new List<AssetStatus>();
        public List<EquipmentTransaction> Transactions { get; set; } = new List<EquipmentTransaction>();
        public List<SceneTransition> SceneTransitions { get; set; } = new List<SceneTransition>();

        // Helper method to generate transitions based on scene allocations
        public void GenerateSceneTransitions(List<Product> allProducts)
        {
            SceneTransitions.Clear();

            var orderedScenes = Scenes.OrderBy(s => s.SceneNumber).ToList();

            for (int i = 0; i < orderedScenes.Count - 1; i++)
            {
                var currentScene = orderedScenes[i];
                var nextScene = orderedScenes[i + 1];

                var transition = GenerateTransitionBetweenScenes(currentScene, nextScene, allProducts);
                if (transition.Actions.Any())
                {
                    SceneTransitions.Add(transition);
                }
            }
        }

        private SceneTransition GenerateTransitionBetweenScenes(Scene fromScene, Scene toScene, List<Product> allProducts)
        {
            var transition = new SceneTransition
            {
                FromSceneNumber = fromScene.SceneNumber,
                ToSceneNumber = toScene.SceneNumber
            };

            // Get allocations for both scenes
            var fromAllocations = fromScene.Allocations.ToList();
            var toAllocations = toScene.Allocations.ToList();

            // Find equipment that needs to be moved
            foreach (var fromAllocation in fromAllocations)
            {
                var product = allProducts.FirstOrDefault(p => p.Id == fromAllocation.ProductId);
                if (product == null) continue;

                // Check if this exact asset (product + asset number) appears in the next scene
                var matchingToAllocation = toAllocations.FirstOrDefault(a =>
                    a.ProductId == fromAllocation.ProductId &&
                    a.AssetInfo == fromAllocation.AssetInfo);

                if (matchingToAllocation != null)
                {
                    // Asset moves from one actor to another (or stays with same actor)
                    if (matchingToAllocation.ActorId != fromAllocation.ActorId)
                    {
                        var fromActor = Cast.FirstOrDefault(a => a.Id == fromAllocation.ActorId);
                        var toActor = Cast.FirstOrDefault(a => a.Id == matchingToAllocation.ActorId);

                        if (fromActor != null && toActor != null)
                        {
                            transition.Actions.Add(new TransitionAction
                            {
                                Type = TransitionType.Transfer,
                                ProductId = fromAllocation.ProductId,
                                AssetNumber = fromAllocation.AssetInfo ?? "",
                                FromActorId = fromAllocation.ActorId,
                                ToActorId = matchingToAllocation.ActorId,
                                Product = product,
                                FromActor = fromActor,
                                ToActor = toActor
                            });
                        }
                    }
                }
                else
                {
                    // Asset is no longer needed - should be checked in
                    var fromActor = Cast.FirstOrDefault(a => a.Id == fromAllocation.ActorId);
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

            // Find new equipment that needs to be checked out for the next scene
            foreach (var toAllocation in toAllocations)
            {
                var product = allProducts.FirstOrDefault(p => p.Id == toAllocation.ProductId);
                if (product == null) continue;

                // Check if this asset was not in the previous scene
                var wasInPreviousScene = fromAllocations.Any(a =>
                    a.ProductId == toAllocation.ProductId &&
                    a.AssetInfo == toAllocation.AssetInfo);

                if (!wasInPreviousScene)
                {
                    // New equipment needs to be checked out
                    var toActor = Cast.FirstOrDefault(a => a.Id == toAllocation.ActorId);
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

            return transition;
        }

        // Helper method to get current allocations for a scene with asset status
        public List<AllocationWithAssets> GetAllocationsWithAssets(Guid sceneId, List<Product> allProducts)
        {
            var scene = Scenes.FirstOrDefault(s => s.Id == sceneId);
            if (scene == null) return new List<AllocationWithAssets>();

            var result = new List<AllocationWithAssets>();

            foreach (var allocation in scene.Allocations)
            {
                var actor = Cast.FirstOrDefault(a => a.Id == allocation.ActorId);
                var product = allProducts.FirstOrDefault(p => p.Id == allocation.ProductId);

                if (actor != null && product != null)
                {
                    // For tracked products with specific assets
                    if (product is TrackedProduct && !string.IsNullOrEmpty(allocation.AssetInfo))
                    {
                        var assetStatus = AssetStatuses.FirstOrDefault(a =>
                            a.ProductId == allocation.ProductId &&
                            a.AssetNumber == allocation.AssetInfo);

                        if (assetStatus == null)
                        {
                            // Create new asset status if it doesn't exist
                            assetStatus = new AssetStatus
                            {
                                ProductId = allocation.ProductId,
                                AssetNumber = allocation.AssetInfo,
                                Status = EquipmentStatus.Available
                            };
                            AssetStatuses.Add(assetStatus);
                        }

                        result.Add(new AllocationWithAssets
                        {
                            AllocationId = allocation.Id,
                            ActorId = allocation.ActorId,
                            ProductId = allocation.ProductId,
                            SceneId = sceneId,
                            AssetNumber = allocation.AssetInfo,
                            Actor = actor,
                            Product = product,
                            Scene = scene,
                            AssetStatus = assetStatus
                        });
                    }
                    // For inventory products or products without specific assets
                    else
                    {
                        result.Add(new AllocationWithAssets
                        {
                            AllocationId = allocation.Id,
                            ActorId = allocation.ActorId,
                            ProductId = allocation.ProductId,
                            SceneId = sceneId,
                            AssetNumber = allocation.AssetInfo,
                            Actor = actor,
                            Product = product,
                            Scene = scene,
                            AssetStatus = null // No specific asset tracking for inventory items
                        });
                    }
                }
            }

            return result;
        }

        // Helper method to check out equipment to an actor
        public bool CheckOutEquipment(Guid assetStatusId, Guid toActorId, Guid sceneId)
        {
            var assetStatus = AssetStatuses.FirstOrDefault(a => a.Id == assetStatusId);
            if (assetStatus == null || !assetStatus.IsAvailable) return false;

            var fromActorId = assetStatus.CurrentlyAssignedToActorId ?? Guid.Empty;

            // Update asset status
            assetStatus.Status = EquipmentStatus.CheckedOut;
            assetStatus.CurrentlyAssignedToActorId = toActorId;
            assetStatus.CurrentSceneId = sceneId;

            // Record transaction
            Transactions.Add(new EquipmentTransaction
            {
                AssetStatusId = assetStatusId,
                FromActorId = fromActorId,
                ToActorId = toActorId,
                SceneId = sceneId,
                FromStatus = EquipmentStatus.Available,
                ToStatus = EquipmentStatus.CheckedOut,
                Type = TransactionType.CheckOut
            });

            return true;
        }

        // Helper method to check in equipment
        public bool CheckInEquipment(Guid assetStatusId, Guid sceneId)
        {
            var assetStatus = AssetStatuses.FirstOrDefault(a => a.Id == assetStatusId);
            if (assetStatus == null || !assetStatus.IsCheckedOut) return false;

            var fromActorId = assetStatus.CurrentlyAssignedToActorId ?? Guid.Empty;

            // Update asset status
            assetStatus.Status = EquipmentStatus.CheckedIn;
            var previousActor = assetStatus.CurrentlyAssignedToActorId;
            assetStatus.CurrentlyAssignedToActorId = null;
            assetStatus.CurrentSceneId = null;

            // Record transaction
            Transactions.Add(new EquipmentTransaction
            {
                AssetStatusId = assetStatusId,
                FromActorId = fromActorId,
                ToActorId = Guid.Empty,
                SceneId = sceneId,
                FromStatus = EquipmentStatus.CheckedOut,
                ToStatus = EquipmentStatus.CheckedIn,
                Type = TransactionType.CheckIn
            });

            return true;
        }

        // Helper method to transfer equipment between actors
        public bool TransferEquipment(Guid assetStatusId, Guid fromActorId, Guid toActorId, Guid sceneId)
        {
            var assetStatus = AssetStatuses.FirstOrDefault(a => a.Id == assetStatusId);
            if (assetStatus == null || assetStatus.CurrentlyAssignedToActorId != fromActorId) return false;

            // Update asset status
            assetStatus.CurrentlyAssignedToActorId = toActorId;
            assetStatus.CurrentSceneId = sceneId;

            // Record transaction
            Transactions.Add(new EquipmentTransaction
            {
                AssetStatusId = assetStatusId,
                FromActorId = fromActorId,
                ToActorId = toActorId,
                SceneId = sceneId,
                FromStatus = EquipmentStatus.CheckedOut,
                ToStatus = EquipmentStatus.CheckedOut,
                Type = TransactionType.Transfer
            });

            return true;
        }
    }
}
d, Guid toActorId, Guid sceneId)
        {
            var assetStatus = AssetStatuses.FirstOrDefault(a => a.Id == assetStatusId);
if (assetStatus == null || assetStatus.CurrentlyAssignedToActorId != fromActorId) return false;

// Update asset status
assetStatus.CurrentlyAssignedToActorId = toActorId;
assetStatus.CurrentSceneId = sceneId;

// Record transaction
Transactions.Add(new EquipmentTransaction
{
    AssetStatusId = assetStatusId,
    FromActorId = fromActorId,
    ToActorId = toActorId,
    SceneId = sceneId,
    FromStatus = EquipmentStatus.CheckedOut,
    ToStatus = EquipmentStatus.CheckedOut,
    Type = TransactionType.Transfer
});

return true;
        }d, Guid toActorId, Guid sceneId)
        {
            var assetStatus = AssetStatuses.FirstOrDefault(a => a.Id == assetStatusId);
if (assetStatus == null || assetStatus.CurrentlyAssignedToActorId != fromActorId) return false;

// Update asset status
assetStatus.CurrentlyAssignedToActorId = toActorId;
assetStatus.CurrentSceneId = sceneId;

// Record transaction
Transactions.Add(new EquipmentTransaction
{
    AssetStatusId = assetStatusId,
    FromActorId = fromActorId,
    ToActorId = toActorId,
    SceneId = sceneId,
    FromStatus = EquipmentStatus.CheckedOut,
    ToStatus = EquipmentStatus.CheckedOut,
    Type = TransactionType.Transfer
});

return true;
        }
    }
}