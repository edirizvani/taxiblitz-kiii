using Microsoft.EntityFrameworkCore;
using TaxiBlitz.Application.Interfaces;
using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Persistence.Repositories
{
    public class TourPostRepository : ITourPostRepository
    {
        private readonly AppDbContext _db;
        public TourPostRepository(AppDbContext db) => _db = db;

        public async Task<TourPost?> GetByIdAsync(int id) =>
            await _db.TourPosts.FindAsync(id);

        public async Task<IReadOnlyList<TourPost>> GetAllAsync() =>
            await _db.TourPosts.ToListAsync();

        public async Task AddAsync(TourPost entity) =>
            await _db.TourPosts.AddAsync(entity);

        public Task UpdateAsync(TourPost entity)
        {
            _db.Entry(entity).State = EntityState.Modified;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(TourPost entity)
        {
            _db.TourPosts.Remove(entity);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync() =>
            await _db.SaveChangesAsync();

        public async Task<List<TourPost>> GetPublishedPagedAsync(int skip, int take) =>
            await _db.TourPosts
                .Where(p => p.IsPublished)
                .Include(p => p.Author)
                .Include(p => p.Images)
                .OrderByDescending(p => p.CreatedDate)
                .Skip(skip).Take(take)
                .ToListAsync();

        public async Task<int> CountPublishedAsync() =>
            await _db.TourPosts.CountAsync(p => p.IsPublished);

        public async Task<TourPost?> GetByIdWithDetailsAsync(int id) =>
            await _db.TourPosts
                .Include(p => p.Author)
                .Include(p => p.Images)
                .Include(p => p.Comments).ThenInclude(c => c.CommentAuthor)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsPublished);

        public async Task<TourPost?> GetByIdWithImagesAsync(int id) =>
            await _db.TourPosts.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);

        public async Task<List<TourPost>> GetRelatedAsync(int excludeId, string? tags, string? destination, int count) =>
            await _db.TourPosts
                .Where(p => p.IsPublished && p.Id != excludeId &&
                    (p.Tags == tags || p.TourDestination == destination))
                .OrderByDescending(p => p.CreatedDate)
                .Take(count)
                .ToListAsync();

        public async Task<List<TourPost>> GetAllForManagementAsync() =>
            await _db.TourPosts
                .Include(p => p.Author)
                .Include(p => p.Images)
                .Include(p => p.Comments)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();

        public async Task IncrementViewCountAsync(int id)
        {
            var post = await _db.TourPosts.FindAsync(id);
            if (post != null)
            {
                post.ViewCount++;
                await _db.SaveChangesAsync();
            }
        }

        public async Task<TourComment?> GetCommentByIdAsync(int commentId) =>
            await _db.TourComments.FindAsync(commentId);

        public async Task AddCommentAsync(TourComment comment) =>
            await _db.TourComments.AddAsync(comment);

        public Task DeleteCommentAsync(TourComment comment)
        {
            _db.TourComments.Remove(comment);
            return Task.CompletedTask;
        }
    }
}
