// Services/JsonDataService.cs
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
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
                var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);

                return settings?.LastShowPath;
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
                var settings = new AppSettings { LastShowPath = filePath };
                var json = JsonSerializer.Serialize(settings, _jsonOptions);
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
                        product.Accessories = new List<Product>();
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

            // Determine product type by checking for specific properties
            if (jsonObject.TryGetProperty("assetNumber", out _))
            {
                var product = JsonSerializer.Deserialize<TrackedProduct>(jsonObject.GetRawText(), options)!;
                // Ensure accessories list is initialized
                if (product.Accessories == null)
                    product.Accessories = new List<Product>();
                return product;
            }
            else if (jsonObject.TryGetProperty("quantityTotal", out _))
            {
                var product = JsonSerializer.Deserialize<InventoryProduct>(jsonObject.GetRawText(), options)!;
                // Ensure accessories list is initialized
                if (product.Accessories == null)
                    product.Accessories = new List<Product>();
                return product;
            }

            // Default to TrackedProduct if unclear
            var defaultProduct = JsonSerializer.Deserialize<TrackedProduct>(jsonObject.GetRawText(), options)!;
            if (defaultProduct.Accessories == null)
                defaultProduct.Accessories = new List<Product>();
            return defaultProduct;
        }

        public override void Write(Utf8JsonWriter writer, Product value, JsonSerializerOptions options)
        {
            // Create a simplified version for serialization to avoid circular references
            var productData = new Dictionary<string, object>
            {
                ["id"] = value.Id,
                ["name"] = value.Name,
                ["description"] = value.Description,
                ["photoPath"] = value.PhotoPath,
                ["replacementCost"] = value.ReplacementCost,
                ["accessories"] = value.Accessories.Select(a => new { id = a.Id, name = a.Name }).ToList()
            };

            if (value is TrackedProduct trackedProduct)
            {
                productData["assetNumber"] = trackedProduct.AssetNumber;
            }
            else if (value is InventoryProduct inventoryProduct)
            {
                productData["quantityTotal"] = inventoryProduct.QuantityTotal;
                productData["quantityAvailable"] = inventoryProduct.QuantityAvailable;
            }

            JsonSerializer.Serialize(writer, productData, options);
        }
    }
}