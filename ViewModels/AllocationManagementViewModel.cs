// ViewModels/AllocationManagementViewModel.cs - Fixed version
using Pack_Track.Models;
using Pack_Track.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;

namespace Pack_Track.ViewModels
{
    public class AllocationManagementViewModel : BaseViewModel
    {
        private readonly Show _show;
        private readonly IDataService _dataService;
        private Scene? _selectedScene;
        private Actor? _selectedActor;
        private Product? _selectedProduct;
        private string? _selectedAssetNumber;

        public AllocationManagementViewModel(Show show, List<Product> products, IDataService dataService)
        {
            _show = show;
            _dataService = dataService;

            Cast = new ObservableCollection<Actor>(show.Cast);
            Scenes = new ObservableCollection<Scene>(show.Scenes.OrderBy(s => s.SceneNumber));
            Products = new ObservableCollection<Product>(products);
            AvailableAssets = new ObservableCollection<string>();
            Allocations = new ObservableCollection<AllocationDisplayItem>();
            SceneSelections = new ObservableCollection<SceneSelectionItem>();

            AddAllocationCommand = new RelayCommand(AddAllocation, CanAddAllocation);
            RemoveAllocationCommand = new RelayCommand(RemoveAllocation, () => SelectedAllocation != null);
            SelectAllScenesCommand = new RelayCommand(SelectAllScenes);
            DeselectAllScenesCommand = new RelayCommand(DeselectAllScenes);
            SelectSceneCommand = new RelayCommand<Scene>(SelectScene);

            InitializeSceneSelections();

            // Select first scene by default
            if (Scenes.Any())
            {
                SelectedScene = Scenes.First();
            }
        }

        public ObservableCollection<Actor> Cast { get; }
        public ObservableCollection<Scene> Scenes { get; }
        public ObservableCollection<Product> Products { get; }
        public ObservableCollection<string> AvailableAssets { get; }
        public ObservableCollection<AllocationDisplayItem> Allocations { get; }
        public ObservableCollection<SceneSelectionItem> SceneSelections { get; }

        public Scene? SelectedScene
        {
            get => _selectedScene;
            set
            {
                if (SetProperty(ref _selectedScene, value))
                {
                    LoadAllocations();
                    UpdateSceneAvailability();
                }
            }
        }

        public Actor? SelectedActor
        {
            get => _selectedActor;
            set => SetProperty(ref _selectedActor, value);
        }

        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (SetProperty(ref _selectedProduct, value))
                {
                    LoadAvailableAssets();
                    UpdateSceneAvailability();
                }
            }
        }

        public string? SelectedAssetNumber
        {
            get => _selectedAssetNumber;
            set
            {
                if (SetProperty(ref _selectedAssetNumber, value))
                {
                    UpdateSceneAvailability();
                }
            }
        }

        private AllocationDisplayItem? _selectedAllocation;
        public AllocationDisplayItem? SelectedAllocation
        {
            get => _selectedAllocation;
            set => SetProperty(ref _selectedAllocation, value);
        }

        public ICommand AddAllocationCommand { get; }
        public ICommand RemoveAllocationCommand { get; }
        public ICommand SelectAllScenesCommand { get; }
        public ICommand DeselectAllScenesCommand { get; }
        public ICommand SelectSceneCommand { get; }

        private void InitializeSceneSelections()
        {
            SceneSelections.Clear();
            foreach (var scene in Scenes.OrderBy(s => s.SceneNumber))
            {
                SceneSelections.Add(new SceneSelectionItem
                {
                    Scene = scene,
                    IsSelected = false,
                    IsEnabled = true
                });
            }
        }

        private void UpdateSceneAvailability()
        {
            if (SelectedProduct == null || string.IsNullOrEmpty(SelectedAssetNumber))
            {
                // Enable all scenes if no product/asset selected
                foreach (var sceneSelection in SceneSelections)
                {
                    sceneSelection.IsEnabled = true;
                    sceneSelection.ConflictInfo = string.Empty;
                }
                return;
            }

            // Check which scenes already have this asset allocated
            foreach (var sceneSelection in SceneSelections)
            {
                var scene = sceneSelection.Scene;
                var existingAllocation = scene.Allocations.FirstOrDefault(a =>
                    a.ProductId == SelectedProduct.Id &&
                    a.AssetInfo == SelectedAssetNumber);

                if (existingAllocation != null)
                {
                    var actor = Cast.FirstOrDefault(c => c.Id == existingAllocation.ActorId);
                    sceneSelection.IsEnabled = false;
                    sceneSelection.IsSelected = false;
                    sceneSelection.ConflictInfo = $"Already assigned to {actor?.DisplayName ?? "Unknown"}";
                }
                else
                {
                    sceneSelection.IsEnabled = true;
                    sceneSelection.ConflictInfo = string.Empty;
                }
            }
        }

        private void LoadAvailableAssets()
        {
            AvailableAssets.Clear();
            SelectedAssetNumber = null;

            if (SelectedProduct is TrackedProduct trackedProduct)
            {
                // Parse asset numbers (comma or newline separated)
                var allAssetNumbers = trackedProduct.AssetNumber
                    .Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();

                foreach (var asset in allAssetNumbers)
                {
                    AvailableAssets.Add(asset);
                }

                if (AvailableAssets.Any())
                {
                    SelectedAssetNumber = AvailableAssets.First();
                }
            }
        }

        private void SelectAllScenes()
        {
            foreach (var sceneSelection in SceneSelections.Where(s => s.IsEnabled))
            {
                sceneSelection.IsSelected = true;
            }
        }

        private void DeselectAllScenes()
        {
            foreach (var sceneSelection in SceneSelections)
            {
                sceneSelection.IsSelected = false;
            }
        }

        private void AddAllocation()
        {
            if (SelectedActor == null || SelectedProduct == null) return;

            var selectedScenes = SceneSelections.Where(s => s.IsSelected && s.IsEnabled).ToList();
            if (!selectedScenes.Any()) return;

            string assetInfo = "";
            if (SelectedProduct is TrackedProduct && !string.IsNullOrEmpty(SelectedAssetNumber))
            {
                assetInfo = SelectedAssetNumber;
            }
            else if (SelectedProduct is InventoryProduct)
            {
                assetInfo = "1"; // Quantity of 1
            }

            // Create allocation for each selected scene
            foreach (var sceneSelection in selectedScenes)
            {
                var allocation = new Allocation
                {
                    ActorId = SelectedActor.Id,
                    ProductId = SelectedProduct.Id,
                    FromScene = sceneSelection.Scene.SceneNumber,
                    ToScene = sceneSelection.Scene.SceneNumber,
                    Actor = SelectedActor,
                    Product = SelectedProduct,
                    AssetInfo = assetInfo
                };

                sceneSelection.Scene.Allocations.Add(allocation);
            }

            LoadAllocations();
            UpdateSceneAvailability();

            // Clear selections
            DeselectAllScenes();
        }

        private bool CanAddAllocation()
        {
            if (SelectedActor == null || SelectedProduct == null) return false;

            // For tracked products, require asset selection
            if (SelectedProduct is TrackedProduct && string.IsNullOrEmpty(SelectedAssetNumber))
                return false;

            // Require at least one scene selected
            return SceneSelections.Any(s => s.IsSelected && s.IsEnabled);
        }

        private void LoadAllocations()
        {
            Allocations.Clear();

            if (SelectedScene == null) return;

            // FIXED: Only show allocations for the selected scene
            foreach (var allocation in SelectedScene.Allocations)
            {
                var actor = Cast.FirstOrDefault(a => a.Id == allocation.ActorId);
                var product = Products.FirstOrDefault(p => p.Id == allocation.ProductId);

                if (actor != null && product != null)
                {
                    Allocations.Add(new AllocationDisplayItem
                    {
                        AllocationId = allocation.Id,
                        ActorName = actor.DisplayName,
                        ProductName = product.Name,
                        AssetInfo = allocation.AssetInfo,
                        FromScene = allocation.FromScene,
                        ToScene = allocation.ToScene,
                        SceneRange = $"Scene {allocation.FromScene}"
                    });
                }
            }
        }

        private void RemoveAllocation()
        {
            if (SelectedAllocation == null || SelectedScene == null) return;

            var result = MessageBox.Show(
                $"Remove {SelectedAllocation.ProductName} from {SelectedAllocation.ActorName}?",
                "Confirm Remove",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Remove only from the current scene
                SelectedScene.Allocations.RemoveAll(a => a.Id == SelectedAllocation.AllocationId);

                LoadAllocations();
                UpdateSceneAvailability();
            }
        }

        private void SelectScene(Scene? scene)
        {
            if (scene != null)
            {
                SelectedScene = scene;
            }
        }
    }

    public class SceneSelectionItem : BaseViewModel
    {
        private bool _isSelected;
        private bool _isEnabled = true;
        private string _conflictInfo = string.Empty;

        public Scene Scene { get; set; } = null!;

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        public string ConflictInfo
        {
            get => _conflictInfo;
            set => SetProperty(ref _conflictInfo, value);
        }

        public string DisplayName => $"Scene {Scene.SceneNumber}: {Scene.Name}";
        public string StatusText => !IsEnabled ? $"Unavailable - {ConflictInfo}" : "";
    }

    public class AllocationDisplayItem
    {
        public Guid AllocationId { get; set; }
        public string ActorName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string AssetInfo { get; set; } = string.Empty;
        public int FromScene { get; set; }
        public int ToScene { get; set; }
        public string SceneRange { get; set; } = string.Empty;
        public string DisplayText => string.IsNullOrEmpty(AssetInfo)
            ? ProductName
            : $"{ProductName} ({AssetInfo})";
    }
}