using Microsoft.EntityFrameworkCore;
using TaxiBlitz.Application.Interfaces;
using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Persistence.Repositories
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly AppDbContext _db;
        public ReviewRepository(AppDbContext db) => _db = db;

        public async Task<Review?> GetByIdAsync(int id) =>
            await _db.Reviews.FindAsync(id);

        public async Task<IReadOnlyList<Review>> GetAllAsync() =>
            await _db.Reviews.ToListAsync();

        public async Task AddAsync(Review entity) =>
            await _db.Reviews.AddAsync(entity);

        public Task UpdateAsync(Review entity)
        {
            _db.Entry(entity).State = EntityState.Modified;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Review entity)
        {
            _db.Reviews.Remove(entity);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync() =>
            await _db.SaveChangesAsync();

        public async Task<int> GetTodayCountAsync()
        {
            var today    = DateTime.Today;
            var tomorrow = today.AddDays(1);
            return await _db.Reviews.CountAsync(r => r.ReviewDate >= today && r.ReviewDate < tomorrow);
        }

        public async Task<(int count, double? average)> GetStatsAsync()
        {
            var rated = _db.Reviews.Where(r => r.Rating.HasValue);
            int count   = await rated.CountAsync();
            double? avg = count > 0 ? await rated.AverageAsync(r => (double)r.Rating!.Value) : null;
            return (count, avg);
        }
    }
}
