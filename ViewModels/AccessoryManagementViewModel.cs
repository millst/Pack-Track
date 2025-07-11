// ViewModels/AccessoryManagementViewModel.cs - Fixed to save changes
using Pack_Track.Models;
using Pack_Track.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;

namespace Pack_Track.ViewModels
{
    public class AccessoryManagementViewModel : BaseViewModel
    {
        private readonly Product _product;
        private readonly IDataService _dataService;
        private readonly List<Product> _allProducts;
        private Product? _selectedAvailableProduct;
        private Product? _selectedAccessory;

        public AccessoryManagementViewModel(Product product, List<Product> allProducts, IDataService dataService)
        {
            _product = product;
            _dataService = dataService;
            _allProducts = allProducts;

            // Available products (excluding the product itself and its current accessories)
            AvailableProducts = new ObservableCollection<Product>(
                allProducts.Where(p => p.Id != product.Id && !product.Accessories.Any(a => a.Id == p.Id))
            );

            // Current accessories
            CurrentAccessories = new ObservableCollection<Product>(product.Accessories);

            AddAccessoryCommand = new RelayCommand(AddAccessory, () => SelectedAvailableProduct != null);
            RemoveAccessoryCommand = new RelayCommand(RemoveAccessory, () => SelectedAccessory != null);
            SaveCommand = new RelayCommand(SaveChanges);
        }

        public string ProductName => _product.Name;

        public ObservableCollection<Product> AvailableProducts { get; }
        public ObservableCollection<Product> CurrentAccessories { get; }

        public Product? SelectedAvailableProduct
        {
            get => _selectedAvailableProduct;
            set => SetProperty(ref _selectedAvailableProduct, value);
        }

        public Product? SelectedAccessory
        {
            get => _selectedAccessory;
            set => SetProperty(ref _selectedAccessory, value);
        }

        public ICommand AddAccessoryCommand { get; }
        public ICommand RemoveAccessoryCommand { get; }
        public ICommand SaveCommand { get; }

        private void AddAccessory()
        {
            if (SelectedAvailableProduct == null) return;

            try
            {
                // Add to current accessories
                CurrentAccessories.Add(SelectedAvailableProduct);
                _product.Accessories.Add(SelectedAvailableProduct);

                // Remove from available
                AvailableProducts.Remove(SelectedAvailableProduct);

                SelectedAvailableProduct = null;

                // Force command reevaluation
                CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding accessory: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveAccessory()
        {
            if (SelectedAccessory == null) return;

            try
            {
                // Store reference before clearing selection
                var accessoryToRemove = SelectedAccessory;

                // Remove from current accessories
                CurrentAccessories.Remove(accessoryToRemove);
                _product.Accessories.Remove(accessoryToRemove);

                // Add back to available (keep sorted)
                var insertIndex = 0;
                for (int i = 0; i < AvailableProducts.Count; i++)
                {
                    if (string.Compare(AvailableProducts[i].Name, accessoryToRemove.Name, StringComparison.OrdinalIgnoreCase) > 0)
                        break;
                    insertIndex = i + 1;
                }
                AvailableProducts.Insert(insertIndex, accessoryToRemove);

                // Clear selection after using it
                SelectedAccessory = null;

                // Force command reevaluation
                CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error removing accessory: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveChanges()
        {
            try
            {
                // Save all products to persist the accessory changes
                await _dataService.SaveProductsAsync(_allProducts);
                MessageBox.Show("Accessories saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving accessories: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}