// ViewModels/ProductManagementViewModel.cs - Complete Fixed Version with Debug
using Pack_Track.Models;
using Pack_Track.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Pack_Track.ViewModels
{
    public class ProductManagementViewModel : BaseViewModel
    {
        private readonly IDataService _dataService;
        private Product? _selectedProduct;
        private string _selectedProductType = "Tracked Product (with asset numbers)";
        private string _assetNumbers = string.Empty;
        private int _inventoryQuantity = 1;

        public ProductManagementViewModel(IDataService dataService)
        {
            _dataService = dataService;

            Products = new ObservableCollection<Product>();

            // Commands
            AddNewProductCommand = new RelayCommand(AddNewProduct);
            SaveProductCommand = new RelayCommand(SaveProduct, () => SelectedProduct != null);
            DeleteProductCommand = new RelayCommand(DeleteProduct, () => SelectedProduct != null);
            BrowsePhotoCommand = new RelayCommand(BrowsePhoto, () => SelectedProduct != null);
            AddAccessoryCommand = new RelayCommand(AddAccessory, () => SelectedProduct != null);
            RemoveAccessoryCommand = new RelayCommand<Product>(RemoveAccessory);

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
                    System.Diagnostics.Debug.WriteLine($"SelectedProduct changed to: {value?.Name ?? "null"}");
                    OnSelectedProductChanged();

                    // Force refresh of all related properties
                    OnPropertyChanged(nameof(SelectedProduct));
                    OnPropertyChanged(nameof(IsTrackedProduct));
                    OnPropertyChanged(nameof(IsInventoryProduct));
                    OnPropertyChanged(nameof(AssetNumbers));
                    OnPropertyChanged(nameof(InventoryQuantity));

                    // Force command reevaluation
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string SelectedProductType
        {
            get => _selectedProductType;
            set
            {
                if (SetProperty(ref _selectedProductType, value))
                {
                    System.Diagnostics.Debug.WriteLine($"SelectedProductType changed to: {value}");
                    OnPropertyChanged(nameof(IsTrackedProduct));
                    OnPropertyChanged(nameof(IsInventoryProduct));
                }
            }
        }

        public bool IsTrackedProduct
        {
            get
            {
                var isTracked = SelectedProductType.Contains("Tracked");
                System.Diagnostics.Debug.WriteLine($"IsTrackedProduct: {isTracked}");
                return isTracked;
            }
        }

        public bool IsInventoryProduct
        {
            get
            {
                var isInventory = SelectedProductType.Contains("Inventory");
                System.Diagnostics.Debug.WriteLine($"IsInventoryProduct: {isInventory}");
                return isInventory;
            }
        }

        public string AssetNumbers
        {
            get => _assetNumbers;
            set
            {
                if (SetProperty(ref _assetNumbers, value))
                {
                    System.Diagnostics.Debug.WriteLine($"AssetNumbers changed to: {value}");
                }
            }
        }

        public int InventoryQuantity
        {
            get => _inventoryQuantity;
            set
            {
                if (SetProperty(ref _inventoryQuantity, value))
                {
                    System.Diagnostics.Debug.WriteLine($"InventoryQuantity changed to: {value}");
                }
            }
        }

        // Commands
        public ICommand AddNewProductCommand { get; }
        public ICommand SaveProductCommand { get; }
        public ICommand DeleteProductCommand { get; }
        public ICommand BrowsePhotoCommand { get; }
        public ICommand AddAccessoryCommand { get; }
        public ICommand RemoveAccessoryCommand { get; }

        private async void LoadProducts()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Loading products...");
                var products = await _dataService.LoadProductsAsync();
                Products.Clear();
                foreach (var product in products)
                {
                    Products.Add(product);
                    System.Diagnostics.Debug.WriteLine($"Loaded product: {product.Name}");
                }
                System.Diagnostics.Debug.WriteLine($"Total products loaded: {Products.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading products: {ex.Message}");
                MessageBox.Show($"Error loading products: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnSelectedProductChanged()
        {
            System.Diagnostics.Debug.WriteLine($"OnSelectedProductChanged called. Product: {SelectedProduct?.Name ?? "null"}");

            if (SelectedProduct == null)
            {
                System.Diagnostics.Debug.WriteLine("SelectedProduct is null, clearing form");
                AssetNumbers = string.Empty;
                InventoryQuantity = 1;
                SelectedProductType = "Tracked Product (with asset numbers)";
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Product type: {SelectedProduct.GetType().Name}");

            // Update form based on selected product
            if (SelectedProduct is TrackedProduct trackedProduct)
            {
                System.Diagnostics.Debug.WriteLine($"Setting up TrackedProduct. AssetNumber: {trackedProduct.AssetNumber}");
                SelectedProductType = "Tracked Product (with asset numbers)";
                AssetNumbers = trackedProduct.AssetNumber ?? string.Empty;
                System.Diagnostics.Debug.WriteLine($"AssetNumbers set to: '{AssetNumbers}'");
            }
            else if (SelectedProduct is InventoryProduct inventoryProduct)
            {
                System.Diagnostics.Debug.WriteLine($"Setting up InventoryProduct. Quantity: {inventoryProduct.QuantityTotal}");
                SelectedProductType = "Inventory Product (quantity only)";
                InventoryQuantity = inventoryProduct.QuantityTotal;
                System.Diagnostics.Debug.WriteLine($"InventoryQuantity set to: {InventoryQuantity}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Unknown product type, defaulting to TrackedProduct");
                SelectedProductType = "Tracked Product (with asset numbers)";
                AssetNumbers = string.Empty;
            }

            System.Diagnostics.Debug.WriteLine($"Form updated. SelectedProductType: {SelectedProductType}");

            // Force update of UI elements that depend on SelectedProduct
            OnPropertyChanged(nameof(SelectedProduct));
        }

        private void AddNewProduct()
        {
            System.Diagnostics.Debug.WriteLine("AddNewProduct called");

            var newProduct = new TrackedProduct
            {
                Name = "New Product",
                Description = "Enter description here",
                ReplacementCost = 0.00m,
                AssetNumber = "Asset1"
            };

            System.Diagnostics.Debug.WriteLine($"Created new product: {newProduct.Name}");
            Products.Add(newProduct);
            SelectedProduct = newProduct;

            System.Diagnostics.Debug.WriteLine($"Product added. Total products: {Products.Count}");
            System.Diagnostics.Debug.WriteLine($"SelectedProduct set to: {SelectedProduct?.Name}");
        }

        private async void SaveProduct()
        {
            if (SelectedProduct == null) return;

            try
            {
                System.Diagnostics.Debug.WriteLine($"Saving product: {SelectedProduct.Name}");

                // Update product based on type selection
                if (IsTrackedProduct && !(SelectedProduct is TrackedProduct))
                {
                    System.Diagnostics.Debug.WriteLine("Converting to TrackedProduct");
                    // Convert to TrackedProduct
                    var trackedProduct = new TrackedProduct
                    {
                        Id = SelectedProduct.Id,
                        Name = SelectedProduct.Name,
                        Description = SelectedProduct.Description,
                        ReplacementCost = SelectedProduct.ReplacementCost,
                        PhotoPath = SelectedProduct.PhotoPath,
                        AssetNumber = AssetNumbers
                    };

                    // Copy accessories
                    trackedProduct.CopyAccessoriesFrom(SelectedProduct.Accessories);

                    var index = Products.IndexOf(SelectedProduct);
                    Products[index] = trackedProduct;
                    SelectedProduct = trackedProduct;
                }
                else if (IsInventoryProduct && !(SelectedProduct is InventoryProduct))
                {
                    System.Diagnostics.Debug.WriteLine("Converting to InventoryProduct");
                    // Convert to InventoryProduct
                    var inventoryProduct = new InventoryProduct
                    {
                        Id = SelectedProduct.Id,
                        Name = SelectedProduct.Name,
                        Description = SelectedProduct.Description,
                        ReplacementCost = SelectedProduct.ReplacementCost,
                        PhotoPath = SelectedProduct.PhotoPath,
                        QuantityTotal = InventoryQuantity,
                        QuantityAvailable = InventoryQuantity
                    };

                    // Copy accessories
                    inventoryProduct.CopyAccessoriesFrom(SelectedProduct.Accessories);

                    var index = Products.IndexOf(SelectedProduct);
                    Products[index] = inventoryProduct;
                    SelectedProduct = inventoryProduct;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Updating existing product");
                    // Update existing product
                    if (SelectedProduct is TrackedProduct tracked)
                    {
                        tracked.AssetNumber = AssetNumbers;
                    }
                    else if (SelectedProduct is InventoryProduct inventory)
                    {
                        inventory.QuantityTotal = InventoryQuantity;
                        inventory.QuantityAvailable = InventoryQuantity;
                    }
                }

                // Save all products
                await _dataService.SaveProductsAsync(Products.ToList());

                MessageBox.Show("Product saved successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving product: {ex.Message}");
                MessageBox.Show($"Error saving product: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                    SelectedProduct = null;

                    MessageBox.Show("Product deleted successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting product: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BrowsePhoto()
        {
            if (SelectedProduct == null) return;

            var dialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg, *.jpeg, *.png, *.bmp)|*.jpg;*.jpeg;*.png;*.bmp|All files (*.*)|*.*",
                Title = "Select Product Photo"
            };

            if (dialog.ShowDialog() == true)
            {
                SelectedProduct.PhotoPath = dialog.FileName;
            }
        }

        private void AddAccessory()
        {
            if (SelectedProduct == null) return;

            try
            {
                var dialog = new Pack_Track.Views.AccessoryManagementDialog(SelectedProduct, Products.ToList(), _dataService);
                dialog.Owner = Application.Current.MainWindow;
                if (dialog.ShowDialog() == true)
                {
                    // Reload products to ensure we have the latest data
                    LoadProducts();
                    // Force refresh of the product view
                    OnPropertyChanged(nameof(SelectedProduct));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening accessory management: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveAccessory(Product? accessory)
        {
            if (SelectedProduct == null || accessory == null) return;

            SelectedProduct.Accessories.Remove(accessory);
        }
    }
}