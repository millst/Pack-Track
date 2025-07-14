// Models/Show.cs - Complete version with smart transitions
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Pack_Track.Models
{
    public partial class Show : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _description = string.Empty;
        private int _currentSceneIndex = 0;

        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public int CurrentSceneIndex
        {
            get => _currentSceneIndex;
            set => SetProperty(ref _currentSceneIndex, value);
        }

        public List<Scene> Scenes { get; set; } = new List<Scene>();
        public List<Run> Runs { get; set; } = new List<Run>();
        public List<Actor> Cast { get; set; } = new List<Actor>();

        // Enhanced tracking properties
        public List<AssetStatus> AssetStatuses { get; set; } = new List<AssetStatus>();
        public List<EquipmentTransaction> Transactions { get; set; } = new List<EquipmentTransaction>();
        public List<SceneTransition> SceneTransitions { get; set; } = new List<SceneTransition>();

        private Scene? _currentScene;

        public Scene? CurrentScene
        {
            get => _currentScene;
            set => SetProperty(ref _currentScene, value);
        }

        // Enhanced tracking methods
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
                                ToActor = toActor,
                                Show = this
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
                            ToActor = null,
                            Show = this
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
                            ToActor = toActor,
                            Show = this
                        });
                    }
                }
            }

            return transition;
        }

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

        public bool CheckInEquipment(Guid assetStatusId, Guid sceneId)
        {
            var assetStatus = AssetStatuses.FirstOrDefault(a => a.Id == assetStatusId);
            if (assetStatus == null || !assetStatus.IsCheckedOut) return false;

            var fromActorId = assetStatus.CurrentlyAssignedToActorId ?? Guid.Empty;

            // Update asset status
            assetStatus.Status = EquipmentStatus.CheckedIn;
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

    public class Scene : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private int _sceneNumber;
        public string DisplayName => $"Scene {SceneNumber}: {Name}";

        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public int SceneNumber
        {
            get => _sceneNumber;
            set => SetProperty(ref _sceneNumber, value);
        }

        public List<Allocation> Allocations { get; set; } = new List<Allocation>();

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

    public class Run : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private DateTime _dateTime = DateTime.Now;
        private RunType _runType = RunType.Rehearsal;

        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public DateTime DateTime
        {
            get => _dateTime;
            set => SetProperty(ref _dateTime, value);
        }

        public RunType RunType
        {
            get => _runType;
            set => SetProperty(ref _runType, value);
        }

        public List<CheckInOutRecord> CheckInOutRecords { get; set; } = new List<CheckInOutRecord>();

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

    public class Actor : INotifyPropertyChanged
    {
        private string _roleName = string.Empty;
        private string _realName = string.Empty;
        private string _phoneNumber = string.Empty;

        public Guid Id { get; set; } = Guid.NewGuid();

        public string RoleName
        {
            get => _roleName;
            set => SetProperty(ref _roleName, value);
        }

        public string RealName
        {
            get => _realName;
            set => SetProperty(ref _realName, value);
        }

        public string PhoneNumber
        {
            get => _phoneNumber;
            set => SetProperty(ref _phoneNumber, value);
        }

        public string DisplayName => !string.IsNullOrEmpty(RoleName) && !string.IsNullOrEmpty(RealName)
            ? $"{RoleName} ({RealName})"
            : RoleName + RealName; // One of them will be empty

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

    public class Allocation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ActorId { get; set; }
        public Guid ProductId { get; set; }
        public int FromScene { get; set; } = 1;
        public int ToScene { get; set; } = 999; // Default to "all scenes"
        public string AssetInfo { get; set; } = string.Empty; // Asset number or quantity

        // Navigation properties (will be resolved at runtime)
        public Actor? Actor { get; set; }
        public Product? Product { get; set; }
    }

    public class CheckInOutRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AllocationId { get; set; }
        public DateTime CheckOutTime { get; set; }
        public DateTime? CheckInTime { get; set; }
        public CheckInOutStatus Status { get; set; } = CheckInOutStatus.CheckedOut;
        public string? Notes { get; set; }
        public decimal? LossCharge { get; set; }

        // Navigation properties
        public Allocation? Allocation { get; set; }
    }

    // Enums
    public enum RunType
    {
        Rehearsal,
        TechnicalRehearsal,
        DressRehearsal,
        Performance
    }

    public enum CheckInOutStatus
    {
        CheckedOut,
        CheckedIn,
        Lost,
        Damaged
    }

    public enum EquipmentStatus
    {
        Available,
        CheckedOut,
        CheckedIn,
        Missing,
        Damaged
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

    public enum TransitionType
    {
        Transfer,  // Equipment moves from one actor to another
        CheckIn,   // Equipment gets returned/checked in
        CheckOut   // Equipment gets checked out to new actor
    }

    // Enhanced tracking classes
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

    // Scene Transition Classes
    public class SceneTransition : INotifyPropertyChanged
    {
        private bool _isCompleted = false;

        public Guid Id { get; set; } = Guid.NewGuid();
        public int FromSceneNumber { get; set; }
        public int ToSceneNumber { get; set; }
        public List<TransitionAction> Actions { get; set; } = new List<TransitionAction>();

        public bool IsCompleted
        {
            get => _isCompleted;
            set => SetProperty(ref _isCompleted, value);
        }

        public string Title => $"Transition: Scene {FromSceneNumber} → Scene {ToSceneNumber}";

        public int CompletedActionsCount => Actions.Count(a => a.IsSmartCompleted);
        public int TotalActionsCount => Actions.Count;
        public bool AllActionsCompleted => Actions.Any() && Actions.All(a => a.IsSmartCompleted);

        public string ProgressText => $"{CompletedActionsCount}/{TotalActionsCount} actions completed";

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

        public void RefreshProgress()
        {
            OnPropertyChanged(nameof(CompletedActionsCount));
            OnPropertyChanged(nameof(AllActionsCompleted));
            OnPropertyChanged(nameof(ProgressText));
        }
    }

    public class TransitionAction : INotifyPropertyChanged
    {
        private bool _isCompleted = false;

        public Guid Id { get; set; } = Guid.NewGuid();
        public TransitionType Type { get; set; }
        public Guid ProductId { get; set; }
        public string AssetNumber { get; set; } = string.Empty;
        public Guid FromActorId { get; set; }
        public Guid ToActorId { get; set; }

        // Navigation properties
        public Product? Product { get; set; }
        public Actor? FromActor { get; set; }
        public Actor? ToActor { get; set; }
        public Show? Show { get; set; } // Add reference to show for status checking

        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                if (SetProperty(ref _isCompleted, value))
                {
                    OnPropertyChanged(nameof(StatusIcon));
                    OnPropertyChanged(nameof(StatusColor));
                }
            }
        }

        // Smart completion status based on actual equipment state
        public bool IsSmartCompleted
        {
            get
            {
                if (Show == null) return false;

                var assetStatus = Show.AssetStatuses.FirstOrDefault(a =>
                    a.ProductId == ProductId && a.AssetNumber == AssetNumber);

                if (assetStatus == null) return false;

                return Type switch
                {
                    TransitionType.CheckIn =>
                        // Completed when equipment is checked in (not with the from actor)
                        assetStatus.Status == EquipmentStatus.CheckedIn ||
                        assetStatus.CurrentlyAssignedToActorId != FromActorId,

                    TransitionType.CheckOut =>
                        // Completed when equipment is with the to actor
                        assetStatus.Status == EquipmentStatus.CheckedOut &&
                        assetStatus.CurrentlyAssignedToActorId == ToActorId,

                    TransitionType.Transfer =>
                        // Completed when equipment is with the to actor (regardless of how it got there)
                        assetStatus.Status == EquipmentStatus.CheckedOut &&
                        assetStatus.CurrentlyAssignedToActorId == ToActorId,

                    _ => false
                };
            }
        }

        // Smart visibility - hide when transition is complete
        public bool IsVisible
        {
            get
            {
                if (Show == null) return true;

                var assetStatus = Show.AssetStatuses.FirstOrDefault(a =>
                    a.ProductId == ProductId && a.AssetNumber == AssetNumber);

                if (assetStatus == null) return true;

                return Type switch
                {
                    TransitionType.CheckIn =>
                        // Show until equipment is checked in
                        assetStatus.Status != EquipmentStatus.CheckedIn &&
                        assetStatus.CurrentlyAssignedToActorId == FromActorId,

                    TransitionType.CheckOut =>
                        // Show until equipment is with the target actor
                        !(assetStatus.Status == EquipmentStatus.CheckedOut &&
                          assetStatus.CurrentlyAssignedToActorId == ToActorId),

                    TransitionType.Transfer =>
                        // Show based on current state and what's needed
                        GetTransferVisibility(assetStatus),

                    _ => true
                };
            }
        }

        private bool GetTransferVisibility(AssetStatus assetStatus)
        {
            // For transfers: Pack 1 goes from Actor 1 to Actor 2

            // If equipment is still with FromActor, show "Pack 1 goes from Actor 1 to Actor 2"
            if (assetStatus.CurrentlyAssignedToActorId == FromActorId)
                return true;

            // If equipment is checked in (intermediate state), show "Pack 1 to Actor 2"
            if (assetStatus.Status == EquipmentStatus.CheckedIn)
                return true;

            // If equipment is with ToActor, hide completely
            if (assetStatus.CurrentlyAssignedToActorId == ToActorId)
                return false;

            // Default: show
            return true;
        }

        public string Description
        {
            get
            {
                if (Show == null) return GetBasicDescription();

                var assetStatus = Show.AssetStatuses.FirstOrDefault(a =>
                    a.ProductId == ProductId && a.AssetNumber == AssetNumber);

                if (assetStatus == null) return GetBasicDescription();

                var productName = Product?.Name ?? "Unknown Product";
                var assetInfo = !string.IsNullOrEmpty(AssetNumber) ? $" {AssetNumber}" : "";
                var fromActorName = FromActor?.DisplayName ?? "Unknown Actor";
                var toActorName = ToActor?.DisplayName ?? "Unknown Actor";

                return Type switch
                {
                    TransitionType.Transfer => GetTransferDescription(assetStatus, productName, assetInfo, fromActorName, toActorName),
                    TransitionType.CheckIn => $"{productName}{assetInfo} returned by {fromActorName}",
                    TransitionType.CheckOut => $"{productName}{assetInfo} goes to {toActorName}",
                    _ => GetBasicDescription()
                };
            }
        }

        private string GetTransferDescription(AssetStatus assetStatus, string productName, string assetInfo, string fromActorName, string toActorName)
        {
            // Smart description based on current state
            if (assetStatus.CurrentlyAssignedToActorId == FromActorId)
            {
                // Still with original actor: "Pack 1 goes from Actor 1 to Actor 2"
                return $"{productName}{assetInfo} goes from {fromActorName} to {toActorName}";
            }
            else if (assetStatus.Status == EquipmentStatus.CheckedIn)
            {
                // Checked in (intermediate): "Pack 1 to Actor 2"
                return $"{productName}{assetInfo} to {toActorName}";
            }
            else
            {
                // Default/fallback
                return $"{productName}{assetInfo} goes from {fromActorName} to {toActorName}";
            }
        }

        private string GetBasicDescription()
        {
            var productName = Product?.Name ?? "Unknown Product";
            var assetInfo = !string.IsNullOrEmpty(AssetNumber) ? $" {AssetNumber}" : "";
            var fromActorName = FromActor?.DisplayName ?? "Unknown Actor";
            var toActorName = ToActor?.DisplayName ?? "Unknown Actor";

            return Type switch
            {
                TransitionType.Transfer => $"{productName}{assetInfo} goes from {fromActorName} to {toActorName}",
                TransitionType.CheckIn => $"{productName}{assetInfo} returned by {fromActorName}",
                TransitionType.CheckOut => $"{productName}{assetInfo} goes to {toActorName}",
                _ => $"{productName}{assetInfo} - {Type}"
            };
        }

        public string StatusIcon => IsSmartCompleted ? "✅" : "⭕";
        public string StatusColor => IsSmartCompleted ? "#4CAF50" : "#FF9800";

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

        // Method to refresh computed properties when asset status changes
        public void RefreshStatus()
        {
            OnPropertyChanged(nameof(IsSmartCompleted));
            OnPropertyChanged(nameof(IsVisible));
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(StatusIcon));
            OnPropertyChanged(nameof(StatusColor));
        }
    }
}