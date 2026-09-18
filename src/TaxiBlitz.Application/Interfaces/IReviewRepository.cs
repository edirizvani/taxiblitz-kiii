using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Application.Interfaces
{
    public interface IReviewRepository : IRepository<Review>
    {
        Task<int> GetTodayCountAsync();
        Task<(int count, double? average)> GetStatsAsync();
    }
}
