using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Application.Services.Interfaces
{
    public interface IDriverService
    {
        Task<List<Driver>> GetActiveAsync();
        Task<List<Driver>> GetFeaturedAsync(int count);
        Task<Driver?> GetByIdAsync(int id);
        Task<Driver?> GetByEmailAsync(string email);
        Task AddAsync(Driver driver);
        Task UpdateAsync(Driver driver);
        Task DeleteAsync(int id);
    }
}
