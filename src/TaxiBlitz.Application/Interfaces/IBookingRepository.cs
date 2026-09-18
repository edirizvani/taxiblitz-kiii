using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Application.Interfaces
{
    public interface IBookingRepository : IRepository<BookingTour>
    {
        Task<List<BookingTour>> GetAllWithIncludesAsync();
        Task<BookingTour?> GetByIdWithIncludesAsync(int id);
        Task<List<BookingTour>> GetByUserEmailAsync(string email);
        Task<List<BookingTour>> GetByDriverIdAsync(int driverId);
        Task<List<BookingTour>> GetApprovedAsync();
        Task ReassignTourAsync(int tourId, int fallbackTourId);
        Task ReassignDriverAsync(int driverId, int fallbackDriverId);
    }
}
