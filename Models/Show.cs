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

        private Scene? _currentScene;

        public Scene? CurrentScene
        {
            get => _currentScene;
            set => SetProperty(ref _currentScene, value);
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

    public enum RunType
    {
        Rehearsal,
        TechnicalRehearsal,
        DressRehearsal,
        Performance
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
    

}