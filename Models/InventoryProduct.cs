// Models/InventoryProduct.cs - Complete with all properties
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Pack_Track.Models
{
    public class InventoryProduct : Product
    {
        private int _quantityTotal = 1;
        private int _quantityAvailable = 1;

        public int QuantityTotal
        {
            get => _quantityTotal;
            set => SetProperty(ref _quantityTotal, value);
        }

        public int QuantityAvailable
        {
            get => _quantityAvailable;
            set => SetProperty(ref _quantityAvailable, value);
        }

        // Backward compatibility property
        public int Quantity
        {
            get => _quantityTotal;
            set
            {
                QuantityTotal = value;
                QuantityAvailable = value; // Default available to total
            }
        }

        public int QuantityInUse => QuantityTotal - QuantityAvailable;

        // Constructor
        public InventoryProduct()
        {
            // Default constructor
        }

        // Copy constructor for type conversions
        public InventoryProduct(Product baseProduct)
        {
            Id = baseProduct.Id;
            Name = baseProduct.Name;
            Description = baseProduct.Description;
            ReplacementCost = baseProduct.ReplacementCost;
            PhotoPath = baseProduct.PhotoPath;

            // Create new ObservableCollection to avoid reference issues
            Accessories = new System.Collections.ObjectModel.ObservableCollection<Product>();
            foreach (var accessory in baseProduct.Accessories)
            {
                Accessories.Add(accessory);
            }

            QuantityTotal = 1;
            QuantityAvailable = 1;
        }

        // Override ToString for better display
        public override string ToString()
        {
            return $"{Name} (Qty: {QuantityAvailable}/{QuantityTotal})";
        }

        // Helper methods for inventory management
        public bool CanCheckOut(int quantity = 1)
        {
            return QuantityAvailable >= quantity;
        }

        public bool CheckOut(int quantity = 1)
        {
            if (!CanCheckOut(quantity)) return false;

            QuantityAvailable -= quantity;
            return true;
        }

        public void CheckIn(int quantity = 1)
        {
            QuantityAvailable = Math.Min(QuantityAvailable + quantity, QuantityTotal);
        }
    }
}