using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Application.Interfaces
{
    public interface ITourRepository : IRepository<Tour>
    {
        Task<List<Tour>> SearchAsync(string? q, string? sortBy, int skip, int take);
        Task<int> CountAsync(string? q);
        Task<List<Tour>> GetActiveAsync();
        Task<(int count, double? average)> GetReviewStatsAsync();
        Task<Tour?> GetBySlugAsync(string slug);
        Task<bool> SlugExistsAsync(string slug, int? excludeId = null);
    }
}
