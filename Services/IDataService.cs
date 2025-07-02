// Services/IDataService.cs
using Pack_Track.Models;

namespace Pack_Track.Services
{
    public interface IDataService
    {
        Task<Show?> LoadShowAsync(string filePath);
        Task SaveShowAsync(Show show, string filePath);
        Task<List<Product>> LoadProductsAsync();
        Task SaveProductsAsync(List<Product> products);
        Task<string?> GetLastShowPathAsync();
    }

    public class AppSettings
    {
        public string? LastShowPath { get; set; }
    }
}