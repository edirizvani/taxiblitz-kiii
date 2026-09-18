using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Application.Services.Interfaces
{
    public interface ITourStoryService
    {
        Task<(List<TourPost> Posts, int TotalPages)> GetPublishedPagedAsync(int page, int pageSize);
        Task<TourPost?> GetByIdWithDetailsAsync(int id);
        Task<TourPost?> GetByIdForEditAsync(int id);
        Task<List<TourPost>> GetRelatedAsync(TourPost post, int count);
        Task<List<TourPost>> GetAllForManagementAsync();
        Task<List<TourPost>> GetRecentFeaturedAsync(int count);
        Task AddAsync(TourPost post, string authorId, string[] imageUrls, string[] imageCaptions);
        Task UpdateAsync(TourPost post, string[] imageUrls, string[] imageCaptions);
        Task DeleteAsync(int id);
        Task AddCommentAsync(int postId, string content, string authorId);
        Task DeleteCommentAsync(int commentId, string currentUserId, bool isAdmin);
    }
}
