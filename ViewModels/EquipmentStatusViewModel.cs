// Enhanced Models for Asset-Level Equipment Tracking
using System.ComponentModel;
using System.Runtime.CompilerServices;

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