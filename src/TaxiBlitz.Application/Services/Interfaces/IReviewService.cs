using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Application.Services.Interfaces
{
    public interface IReviewService
    {
        Task<List<Review>> GetAllAsync();
        Task<Review?> GetByIdAsync(int id);
        Task<(int Count, double? Average)> GetStatsAsync();
        Task AddAsync(Review review);
        Task<bool> TryAddAsync(Review review);
        Task UpdateAsync(Review review);
        Task DeleteAsync(int id);
    }
}
