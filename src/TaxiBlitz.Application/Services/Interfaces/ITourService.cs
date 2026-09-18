using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Application.Services.Interfaces
{
    public interface ITourService
    {
        Task<(List<Tour> Tours, int TotalCount)> SearchAsync(string? q, string? sortBy, int page, int pageSize);
        Task<Tour?> GetByIdAsync(int id);
        Task<Tour?> GetBySlugAsync(string slug);
        Task<List<Tour>> GetActiveAsync();
        Task<(int ReviewCount, double? AverageRating)> GetReviewStatsAsync();
        Task AddAsync(Tour tour);
        Task UpdateAsync(Tour tour);
        Task DeleteAsync(int id);
    }
}
