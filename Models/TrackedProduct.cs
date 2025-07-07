// Models/TrackedProduct.cs
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Pack_Track.Models
{
    public class TrackedProduct : Product
    {
        private string _assetNumber = string.Empty;

        public string AssetNumber
        {
            get => _assetNumber;
            set => SetProperty(ref _assetNumber, value);
        }

        // Constructor
        public TrackedProduct()
        {
            // Default constructor
        }

        // Copy constructor for type conversions
        public TrackedProduct(Product baseProduct)
        {
            Id = baseProduct.Id;
            Name = baseProduct.Name;
            Description = baseProduct.Description;
            ReplacementCost = baseProduct.ReplacementCost;
            PhotoPath = baseProduct.PhotoPath;
            Accessories = baseProduct.Accessories;
            AssetNumber = string.Empty;
        }

        // Override ToString for better display
        public override string ToString()
        {
            return string.IsNullOrEmpty(AssetNumber)
                ? Name
                : $"{Name} ({AssetNumber})";
        }

        // Helper method to get all asset numbers as a list
        public List<string> GetAssetNumbersList()
        {
            if (string.IsNullOrEmpty(AssetNumber))
                return new List<string>();

            return AssetNumber
                .Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        // Helper method to set asset numbers from a list
        public void SetAssetNumbersFromList(List<string> assetNumbers)
        {
            AssetNumber = string.Join(", ", assetNumbers.Where(s => !string.IsNullOrWhiteSpace(s)));
        }
    }
}