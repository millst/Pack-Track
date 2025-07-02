using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Pack_Track.Models
{
    public abstract class Product : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _description = string.Empty;
        private string _photoPath = string.Empty;
        private decimal _replacementCost;

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

        public string PhotoPath
        {
            get => _photoPath;
            set => SetProperty(ref _photoPath, value);
        }

        public decimal ReplacementCost
        {
            get => _replacementCost;
            set => SetProperty(ref _replacementCost, value);
        }

        public List<Product> Accessories { get; set; } = new List<Product>();

        // Helper property to check if this product has accessories
        public bool HasAccessories => Accessories.Any();

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

    public class TrackedProduct : Product
    {
        private string _assetNumber = string.Empty;

        public string AssetNumber
        {
            get => _assetNumber;
            set => SetProperty(ref _assetNumber, value);
        }
    }

    public class InventoryProduct : Product
    {
        private int _quantityAvailable;
        private int _quantityTotal;

        public int QuantityAvailable
        {
            get => _quantityAvailable;
            set => SetProperty(ref _quantityAvailable, value);
        }

        public int QuantityTotal
        {
            get => _quantityTotal;
            set => SetProperty(ref _quantityTotal, value);
        }
    }
}
