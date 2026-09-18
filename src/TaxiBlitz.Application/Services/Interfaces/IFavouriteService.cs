using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Application.Services.Interfaces
{
    public interface IFavouriteService
    {
        Task<IEnumerable<FavouriteTour>> GetUserFavouritesAsync(string userId);
        Task<bool> IsFavouriteAsync(string userId, int tourId);
        Task<bool> ToggleAsync(string userId, int tourId);
    }
}
