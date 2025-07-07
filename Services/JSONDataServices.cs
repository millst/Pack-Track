// Services/JsonDataService.cs - Final Fixed Version (No AppSettings at all)
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.ObjectModel;
using Pack_Track.Models;

namespace Pack_Track.Services
{
    public class JsonDataService : IDataService
    {
        private readonly string _dataDirectory;
        private readonly string _settingsFile;
        private readonly JsonSerializerOptions _jsonOptions;

        public JsonDataService()
        {
            _dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PackTrack");
            _settingsFile = Path.Combine(_dataDirectory, "settings.json");

            if (!Directory.Exists(_dataDirectory))
                Directory.CreateDirectory(_dataDirectory);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter(), new ProductJsonConverter() }
            };
        }

        public async Task<Show?> LoadShowAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return null;

                var json = await File.ReadAllTextAsync(filePath);
                var show = JsonSerializer.Deserialize<Show>(json, _jsonOptions);

                if (show != null)
                {
                    // Save as last opened show
                    await SaveLastShowPathAsync(filePath);
                }

                return show;
            }
            catch
            {
                return null;
            }
        }

        public async Task SaveShowAsync(Show show, string filePath)
        {
            var json = JsonSerializer.Serialize(show, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);

            // Save as last opened show
            await SaveLastShowPathAsync(filePath);
        }

        public async Task<string?> GetLastShowPathAsync()
        {
            try
            {
                if (!File.Exists(_settingsFile)) return null;

                var json = await File.ReadAllTextAsync(_settingsFile);

                // Use a simple dictionary approach to avoid any class conflicts
                var settingsDict = JsonSerializer.Deserialize<Dictionary<string, string>>(json, _jsonOptions);

                if (settingsDict != null && settingsDict.ContainsKey("lastShowPath"))
                {
                    return settingsDict["lastShowPath"];
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private async Task SaveLastShowPathAsync(string filePath)
        {
            try
            {
                // Use a simple dictionary approach to avoid any class conflicts
                var settingsDict = new Dictionary<string, string>
                {
                    { "lastShowPath", filePath }
                };

                var json = JsonSerializer.Serialize(settingsDict, _jsonOptions);
                await File.WriteAllTextAsync(_settingsFile, json);
            }
            catch
            {
                // Ignore settings save errors
            }
        }

        public async Task<List<Product>> LoadProductsAsync()
        {
            var filePath = Path.Combine(_dataDirectory, "products.json");
            try
            {
                if (!File.Exists(filePath)) return new List<Product>();

                var json = await File.ReadAllTextAsync(filePath);
                if (string.IsNullOrWhiteSpace(json)) return new List<Product>();

                var products = JsonSerializer.Deserialize<List<Product>>(json, _jsonOptions) ?? new List<Product>();

                // Ensure all products have accessories lists initialized
                foreach (var product in products)
                {
                    if (product.Accessories == null)
                        product.Accessories = new ObservableCollection<Product>();
                }

                return products;
            }
            catch (Exception ex)
            {
                // Log the error and return empty list - this will allow the app to continue
                System.Diagnostics.Debug.WriteLine($"Error loading products: {ex.Message}");

                // Optionally backup the corrupted file
                if (File.Exists(filePath))
                {
                    var backupPath = filePath + ".backup." + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    try
                    {
                        File.Copy(filePath, backupPath);
                        File.Delete(filePath); // Clear corrupted file
                    }
                    catch { /* Ignore backup errors */ }
                }

                return new List<Product>();
            }
        }

        public async Task SaveProductsAsync(List<Product> products)
        {
            var filePath = Path.Combine(_dataDirectory, "products.json");
            var json = JsonSerializer.Serialize(products, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }
    }

    // Custom converter for Product inheritance
    public class ProductJsonConverter : JsonConverter<Product>
    {
        public override Product Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var jsonDocument = JsonDocument.ParseValue(ref reader);
            var jsonObject = jsonDocument.RootElement;

            Product product;

            // Determine product type by checking for specific properties
            if (jsonObject.TryGetProperty("assetNumber", out _))
            {
                product = JsonSerializer.Deserialize<TrackedProduct>(jsonObject.GetRawText(), options)!;
            }
            else if (jsonObject.TryGetProperty("quantityTotal", out _) || jsonObject.TryGetProperty("quantityAvailable", out _))
            {
                product = JsonSerializer.Deserialize<InventoryProduct>(jsonObject.GetRawText(), options)!;
            }
            else
            {
                // Default to TrackedProduct if unclear
                product = JsonSerializer.Deserialize<TrackedProduct>(jsonObject.GetRawText(), options)!;
            }

            // Initialize accessories as ObservableCollection
            if (product.Accessories == null)
            {
                product.Accessories = new ObservableCollection<Product>();
            }
            else if (!(product.Accessories is ObservableCollection<Product>))
            {
                // Convert List<Product> to ObservableCollection<Product>
                var accessoriesList = product.Accessories.ToList();
                product.Accessories = new ObservableCollection<Product>(accessoriesList);
            }

            // Load accessories recursively if they exist in JSON
            if (jsonObject.TryGetProperty("accessories", out var accessoriesElement) && accessoriesElement.ValueKind == JsonValueKind.Array)
            {
                var accessories = new ObservableCollection<Product>();
                foreach (var accessoryElement in accessoriesElement.EnumerateArray())
                {
                    var accessory = JsonSerializer.Deserialize<Product>(accessoryElement.GetRawText(), options);
                    if (accessory != null)
                    {
                        // Ensure accessories of accessories are also ObservableCollection
                        if (accessory.Accessories == null)
                            accessory.Accessories = new ObservableCollection<Product>();

                        accessories.Add(accessory);
                    }
                }
                product.Accessories = accessories;
            }

            return product;
        }

        public override void Write(Utf8JsonWriter writer, Product value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            // Write base Product properties
            writer.WriteString("id", value.Id.ToString());
            writer.WriteString("name", value.Name);
            writer.WriteString("description", value.Description);
            writer.WriteString("photoPath", value.PhotoPath);
            writer.WriteNumber("replacementCost", value.ReplacementCost);

            // Write type-specific properties
            if (value is TrackedProduct trackedProduct)
            {
                writer.WriteString("assetNumber", trackedProduct.AssetNumber);
            }
            else if (value is InventoryProduct inventoryProduct)
            {
                writer.WriteNumber("quantityTotal", inventoryProduct.QuantityTotal);
                writer.WriteNumber("quantityAvailable", inventoryProduct.QuantityAvailable);
            }

            // Write accessories array
            writer.WritePropertyName("accessories");
            writer.WriteStartArray();

            if (value.Accessories != null)
            {
                foreach (var accessory in value.Accessories)
                {
                    JsonSerializer.Serialize(writer, accessory, options);
                }
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }
}