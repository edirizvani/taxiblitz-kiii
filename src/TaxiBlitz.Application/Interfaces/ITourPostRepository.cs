using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Application.Interfaces
{
    public interface ITourPostRepository : IRepository<TourPost>
    {
        Task<List<TourPost>> GetPublishedPagedAsync(int skip, int take);
        Task<int> CountPublishedAsync();
        Task<TourPost?> GetByIdWithDetailsAsync(int id);
        Task<TourPost?> GetByIdWithImagesAsync(int id);
        Task<List<TourPost>> GetRelatedAsync(int excludeId, string? tags, string? destination, int count);
        Task<List<TourPost>> GetAllForManagementAsync();
        Task IncrementViewCountAsync(int id);
        Task<TourComment?> GetCommentByIdAsync(int commentId);
        Task AddCommentAsync(TourComment comment);
        Task DeleteCommentAsync(TourComment comment);
    }
}
