using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Application.Interfaces
{
    public interface IFavouriteRepository
    {
        Task<IEnumerable<FavouriteTour>> GetByUserIdAsync(string userId);
        Task<bool> IsFavouriteAsync(string userId, int tourId);
        Task AddAsync(FavouriteTour favourite);
        Task RemoveAsync(string userId, int tourId);
        Task SaveChangesAsync();
    }
}
