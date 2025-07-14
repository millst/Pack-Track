// Models/SceneTransition.cs - New models for managing equipment transitions between scenes
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Pack_Track.Models
{
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

        public int CompletedActionsCount => Actions.Count(a => a.IsCompleted);
        public int TotalActionsCount => Actions.Count;
        public bool AllActionsCompleted => Actions.Any() && Actions.All(a => a.IsCompleted);

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

        public string Description
        {
            get
            {
                var productName = Product?.Name ?? "Unknown Product";
                var assetInfo = !string.IsNullOrEmpty(AssetNumber) ? $" ({AssetNumber})" : "";
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
        }

        public string StatusIcon => IsCompleted ? "✅" : "⭕";
        public string StatusColor => IsCompleted ? "#4CAF50" : "#FF9800";

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

    public enum TransitionType
    {
        Transfer,  // Equipment moves from one actor to another
        CheckIn,   // Equipment gets returned/checked in
        CheckOut   // Equipment gets checked out to new actor
    }
}