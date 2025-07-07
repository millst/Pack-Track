// Models/Product.cs - Make sure Product is not abstract
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Pack_Track.Models
{
    public class Product : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _description = string.Empty;
        private decimal _replacementCost;
        private string _photoPath = string.Empty;

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

        public decimal ReplacementCost
        {
            get => _replacementCost;
            set => SetProperty(ref _replacementCost, value);
        }

        public string PhotoPath
        {
            get => _photoPath;
            set => SetProperty(ref _photoPath, value);
        }

        public ObservableCollection<Product> Accessories { get; set; } = new ObservableCollection<Product>();

        // Helper method for safely copying accessories
        public void CopyAccessoriesFrom(ObservableCollection<Product> sourceAccessories)
        {
            Accessories.Clear();
            foreach (var accessory in sourceAccessories)
            {
                Accessories.Add(accessory);
            }
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
}