using Microsoft.EntityFrameworkCore;
using TaxiBlitz.Application.Interfaces;
using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Persistence.Repositories
{
    public class TourRepository : ITourRepository
    {
        private readonly AppDbContext _db;
        public TourRepository(AppDbContext db) => _db = db;

        public async Task<Tour?> GetByIdAsync(int id) =>
            await _db.Tours.FindAsync(id);

        public async Task<IReadOnlyList<Tour>> GetAllAsync() =>
            await _db.Tours.ToListAsync();

        public async Task AddAsync(Tour entity) =>
            await _db.Tours.AddAsync(entity);

        public Task UpdateAsync(Tour entity)
        {
            _db.Entry(entity).State = EntityState.Modified;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Tour entity)
        {
            _db.Tours.Remove(entity);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync() =>
            await _db.SaveChangesAsync();

        public async Task<List<Tour>> GetActiveAsync() =>
            await _db.Tours.Where(t => t.Title != "Deleted").ToListAsync();

        public async Task<List<Tour>> SearchAsync(string? q, string? sortBy, int skip, int take)
        {
            var query = _db.Tours.Where(t => t.Title != "Deleted").AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(t =>
                    t.Title.Contains(term) ||
                    t.Description.Contains(term) ||
                    t.StartingPoint.Contains(term) ||
                    t.EndingPoint.Contains(term));
            }

            query = sortBy switch
            {
                "price-asc"  => query.OrderBy(t => t.Price),
                "price-desc" => query.OrderByDescending(t => t.Price),
                "title"      => query.OrderBy(t => t.Title),
                _            => query.OrderBy(t => t.Id)
            };

            return await query.Skip(skip).Take(take).ToListAsync();
        }

        public async Task<int> CountAsync(string? q)
        {
            var query = _db.Tours.Where(t => t.Title != "Deleted").AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(t =>
                    t.Title.Contains(term) ||
                    t.Description.Contains(term) ||
                    t.StartingPoint.Contains(term) ||
                    t.EndingPoint.Contains(term));
            }
            return await query.CountAsync();
        }

        public async Task<(int count, double? average)> GetReviewStatsAsync()
        {
            var rated = _db.Reviews.Where(r => r.Rating.HasValue);
            int count     = await rated.CountAsync();
            double? avg   = count > 0 ? await rated.AverageAsync(r => (double)r.Rating!.Value) : null;
            return (count, avg);
        }

        public async Task<Tour?> GetBySlugAsync(string slug) =>
            await _db.Tours.FirstOrDefaultAsync(t => t.Slug == slug);

        public async Task<bool> SlugExistsAsync(string slug, int? excludeId = null) =>
            await _db.Tours.AnyAsync(t => t.Slug == slug && (excludeId == null || t.Id != excludeId));
    }
}
