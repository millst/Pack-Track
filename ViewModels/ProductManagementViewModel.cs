using Pack_Track.Models;
using Pack_Track.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Win32;
using System.Windows;
using Pack_Track.Views;

namespace Pack_Track.ViewModels
{
    public class ProductManagementViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;
        private Product? _selectedProduct;
        private string _productName = string.Empty;
        private string _productDescription = string.Empty;
        private decimal _replacementCost;
        private string _photoPath = string.Empty;
        private bool _isTrackedProduct = true;
        private string _assetNumbers = string.Empty;
        private int _quantityTotal;
        private bool _isEditMode = false;

        public ProductManagementViewModel(IDataService dataService)
        {
            _dataService = dataService;
            Products = new ObservableCollection<Product>();

            // Initialize commands
            AddProductCommand = new RelayCommand(AddProduct);
            SaveProductCommand = new RelayCommand(SaveProduct, CanSaveProduct);
            DeleteProductCommand = new RelayCommand(DeleteProduct, () => SelectedProduct != null);
            CancelEditCommand = new RelayCommand(CancelEdit);
            SelectPhotoCommand = new RelayCommand(SelectPhoto);
            AddAssetNumbersCommand = new RelayCommand(AddAssetNumbers, () => IsTrackedProduct);
            ManageAccessoriesCommand = new RelayCommand(ManageAccessories, () => SelectedProduct != null);
            ImportProductsCommand = new RelayCommand(ImportProducts);
            ExportProductsCommand = new RelayCommand(ExportProducts);

            LoadProducts();
        }

        public ObservableCollection<Product> Products { get; }

        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (SetProperty(ref _selectedProduct, value))
                {
                    LoadProductForEdit();
                }
            }
        }

        public string ProductName
        {
            get => _productName;
            set => SetProperty(ref _productName, value);
        }

        public string ProductDescription
        {
            get => _productDescription;
            set => SetProperty(ref _productDescription, value);
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

        public bool IsTrackedProduct
        {
            get => _isTrackedProduct;
            set => SetProperty(ref _isTrackedProduct, value);
        }

        public string AssetNumbers
        {
            get => _assetNumbers;
            set => SetProperty(ref _assetNumbers, value);
        }

        public int QuantityTotal
        {
            get => _quantityTotal;
            set => SetProperty(ref _quantityTotal, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public string SaveButtonText => IsEditMode ? "Update Product" : "Add Product";

        // Commands
        public ICommand AddProductCommand { get; }
        public ICommand SaveProductCommand { get; }
        public ICommand DeleteProductCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand SelectPhotoCommand { get; }
        public ICommand AddAssetNumbersCommand { get; }
        public ICommand ManageAccessoriesCommand { get; }
        public ICommand ImportProductsCommand { get; }
        public ICommand ExportProductsCommand { get; }

        private async void LoadProducts()
        {
            try
            {
                var products = await _dataService.LoadProductsAsync();
                Products.Clear();
                foreach (var product in products)
                {
                    Products.Add(product);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading products: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProductForEdit()
        {
            if (SelectedProduct == null)
            {
                ClearForm();
                return;
            }

            IsEditMode = true;
            ProductName = SelectedProduct.Name;
            ProductDescription = SelectedProduct.Description;
            ReplacementCost = SelectedProduct.ReplacementCost;
            PhotoPath = SelectedProduct.PhotoPath;

            if (SelectedProduct is TrackedProduct trackedProduct)
            {
                IsTrackedProduct = true;
                AssetNumbers = trackedProduct.AssetNumber;
            }
            else if (SelectedProduct is InventoryProduct inventoryProduct)
            {
                IsTrackedProduct = false;
                QuantityTotal = inventoryProduct.QuantityTotal;
            }

            OnPropertyChanged(nameof(SaveButtonText));
        }

        private void AddProduct()
        {
            ClearForm();
            IsEditMode = false;
            ProductName = "New Product";
            OnPropertyChanged(nameof(SaveButtonText));
        }

        private async void SaveProduct()
        {
            try
            {
                Product product;

                if (IsEditMode && SelectedProduct != null)
                {
                    // Update existing product
                    product = SelectedProduct;
                }
                else
                {
                    // Create new product
                    if (IsTrackedProduct)
                    {
                        product = new TrackedProduct();
                    }
                    else
                    {
                        product = new InventoryProduct();
                    }
                    Products.Add(product);
                }

                // Update common properties
                product.Name = ProductName.Trim();
                product.Description = ProductDescription.Trim();
                product.ReplacementCost = ReplacementCost;
                product.PhotoPath = PhotoPath.Trim();

                // Initialize accessories if null
                if (product.Accessories == null)
                    product.Accessories = new List<Product>();

                // Update type-specific properties
                if (product is TrackedProduct trackedProduct && IsTrackedProduct)
                {
                    trackedProduct.AssetNumber = AssetNumbers.Trim();
                }
                else if (product is InventoryProduct inventoryProduct && !IsTrackedProduct)
                {
                    inventoryProduct.QuantityTotal = QuantityTotal;
                    inventoryProduct.QuantityAvailable = QuantityTotal; // Initially all available
                }

                // Save to file
                await _dataService.SaveProductsAsync(Products.ToList());

                if (!IsEditMode)
                {
                    SelectedProduct = product;
                }

                MessageBox.Show($"Product '{product.Name}' saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Refresh the display
                OnPropertyChanged(nameof(Products));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving product: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanSaveProduct()
        {
            return !string.IsNullOrWhiteSpace(ProductName) && ReplacementCost >= 0;
        }

        private async void DeleteProduct()
        {
            if (SelectedProduct == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete '{SelectedProduct.Name}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    Products.Remove(SelectedProduct);
                    await _dataService.SaveProductsAsync(Products.ToList());
                    ClearForm();
                    MessageBox.Show("Product deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting product: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CancelEdit()
        {
            if (IsEditMode)
            {
                LoadProductForEdit(); // Reload original values
            }
            else
            {
                ClearForm();
            }
        }

        private void SelectPhoto()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp;*.gif)|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All files (*.*)|*.*",
                Title = "Select Product Photo"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Test if the image can be loaded
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage(new Uri(dialog.FileName));
                    PhotoPath = dialog.FileName;
                    OnPropertyChanged(nameof(PhotoPath)); // Force UI update
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unable to load image: {ex.Message}", "Invalid Image", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void AddAssetNumbers()
        {
            var dialog = new AssetNumberDialog();
            if (dialog.ShowDialog() == true)
            {
                AssetNumbers = dialog.AssetNumbers;
            }
        }

        private void ManageAccessories()
        {
            if (SelectedProduct == null) return;

            var dialog = new Views.AccessoryManagementDialog(SelectedProduct, Products.ToList());
            if (dialog.ShowDialog() == true)
            {
                // Refresh the current product display
                OnPropertyChanged(nameof(SelectedProduct));
            }
        }

        private void ImportProducts()
        {
            MessageBox.Show("Import functionality will be implemented in a future version.", "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportProducts()
        {
            MessageBox.Show("Export functionality will be implemented in a future version.", "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearForm()
        {
            ProductName = string.Empty;
            ProductDescription = string.Empty;
            ReplacementCost = 0;
            PhotoPath = string.Empty;
            IsTrackedProduct = true;
            AssetNumbers = string.Empty;
            QuantityTotal = 0;
            SelectedProduct = null;
            IsEditMode = false;
            OnPropertyChanged(nameof(SaveButtonText));
        }
    }
}