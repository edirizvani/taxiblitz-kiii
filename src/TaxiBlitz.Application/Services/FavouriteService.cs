using TaxiBlitz.Application.Interfaces;
using TaxiBlitz.Application.Services.Interfaces;
using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Application.Services
{
    public class FavouriteService : IFavouriteService
    {
        private readonly IFavouriteRepository _repo;
        public FavouriteService(IFavouriteRepository repo) => _repo = repo;

        public Task<IEnumerable<FavouriteTour>> GetUserFavouritesAsync(string userId)
            => _repo.GetByUserIdAsync(userId);

        public Task<bool> IsFavouriteAsync(string userId, int tourId)
            => _repo.IsFavouriteAsync(userId, tourId);

        public async Task<bool> ToggleAsync(string userId, int tourId)
        {
            bool exists = await _repo.IsFavouriteAsync(userId, tourId);
            if (exists)
            {
                await _repo.RemoveAsync(userId, tourId);
                await _repo.SaveChangesAsync();
                return false;
            }
            await _repo.AddAsync(new FavouriteTour { UserId = userId, TourId = tourId });
            await _repo.SaveChangesAsync();
            return true;
        }
    }
}
