using Microsoft.EntityFrameworkCore;
using TaxiBlitz.Application.Interfaces;
using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Persistence.Repositories
{
    public class FavouriteRepository : IFavouriteRepository
    {
        private readonly AppDbContext _db;
        public FavouriteRepository(AppDbContext db) => _db = db;

        public async Task<IEnumerable<FavouriteTour>> GetByUserIdAsync(string userId)
            => await _db.Favourites.Include(f => f.Tour).Where(f => f.UserId == userId)
                        .OrderByDescending(f => f.CreatedAt).ToListAsync();

        public async Task<bool> IsFavouriteAsync(string userId, int tourId)
            => await _db.Favourites.AnyAsync(f => f.UserId == userId && f.TourId == tourId);

        public async Task AddAsync(FavouriteTour favourite)
            => await _db.Favourites.AddAsync(favourite);

        public async Task RemoveAsync(string userId, int tourId)
        {
            var fav = await _db.Favourites.FirstOrDefaultAsync(f => f.UserId == userId && f.TourId == tourId);
            if (fav != null) _db.Favourites.Remove(fav);
        }

        public Task SaveChangesAsync() => _db.SaveChangesAsync();
    }
}
