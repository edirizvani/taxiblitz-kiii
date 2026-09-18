using TaxiBlitz.Application.Interfaces;
using TaxiBlitz.Application.Services.Interfaces;
using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Application.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviews;
        public ReviewService(IReviewRepository reviews) => _reviews = reviews;

        public async Task<List<Review>> GetAllAsync() =>
            (await _reviews.GetAllAsync()).ToList();

        public Task<Review?> GetByIdAsync(int id) => _reviews.GetByIdAsync(id);

        public async Task<(int Count, double? Average)> GetStatsAsync()
        {
            var (count, avg) = await _reviews.GetStatsAsync();
            return (count, avg);
        }

        public async Task AddAsync(Review review)
        {
            await _reviews.AddAsync(review);
            await _reviews.SaveChangesAsync();
        }

        public async Task<bool> TryAddAsync(Review review)
        {
            int todayCount = await _reviews.GetTodayCountAsync();
            if (todayCount >= 5) return false;

            await _reviews.AddAsync(review);
            await _reviews.SaveChangesAsync();
            return true;
        }

        public async Task UpdateAsync(Review review)
        {
            await _reviews.UpdateAsync(review);
            await _reviews.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var review = await _reviews.GetByIdAsync(id);
            if (review == null) return;
            await _reviews.DeleteAsync(review);
            await _reviews.SaveChangesAsync();
        }
    }
}
