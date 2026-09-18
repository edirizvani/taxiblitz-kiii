using TaxiBlitz.Application.Interfaces;
using TaxiBlitz.Application.Services.Interfaces;
using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Shared.Extensions;

namespace TaxiBlitz.Application.Services
{
    public class TourService : ITourService
    {
        private readonly ITourRepository    _tours;
        private readonly IBookingRepository _bookings;
        public TourService(ITourRepository tours, IBookingRepository bookings)
        {
            _tours    = tours;
            _bookings = bookings;
        }

        public async Task<(List<Tour> Tours, int TotalCount)> SearchAsync(string? q, string? sortBy, int page, int pageSize)
        {
            int skip       = (page - 1) * pageSize;
            var tours      = await _tours.SearchAsync(q, sortBy, skip, pageSize);
            int totalCount = await _tours.CountAsync(q);
            return (tours, totalCount);
        }

        public Task<Tour?> GetByIdAsync(int id)    => _tours.GetByIdAsync(id);
        public Task<Tour?> GetBySlugAsync(string s) => _tours.GetBySlugAsync(s);
        public Task<List<Tour>> GetActiveAsync()    => _tours.GetActiveAsync();

        public async Task<(int ReviewCount, double? AverageRating)> GetReviewStatsAsync()
        {
            var (count, avg) = await _tours.GetReviewStatsAsync();
            return (count, avg);
        }

        public async Task AddAsync(Tour tour)
        {
            tour.Slug = await UniqueSlugAsync(tour.Title, null);
            await _tours.AddAsync(tour);
            await _tours.SaveChangesAsync();
        }

        public async Task UpdateAsync(Tour tour)
        {
            // Regenerate slug only when it's missing (existing tours get slugs on first edit)
            if (string.IsNullOrWhiteSpace(tour.Slug))
                tour.Slug = await UniqueSlugAsync(tour.Title, tour.Id);
            await _tours.UpdateAsync(tour);
            await _tours.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            await _bookings.ReassignTourAsync(id, 15);
            var tour = await _tours.GetByIdAsync(id);
            if (tour == null) return;
            await _tours.DeleteAsync(tour);
            await _tours.SaveChangesAsync();
        }

        private async Task<string> UniqueSlugAsync(string title, int? excludeId)
        {
            var baseSlug = title.ToSlug();
            if (string.IsNullOrEmpty(baseSlug)) baseSlug = "tour";

            var candidate = baseSlug;
            int suffix    = 2;
            while (await _tours.SlugExistsAsync(candidate, excludeId))
            {
                candidate = $"{baseSlug}-{suffix++}";
            }
            return candidate;
        }
    }
}
