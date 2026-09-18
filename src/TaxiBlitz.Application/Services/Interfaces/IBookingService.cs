using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Application.Services.Interfaces
{
    public interface IBookingService
    {
        Task<List<BookingTour>> GetAllWithIncludesAsync();
        Task<BookingTour?> GetByIdAsync(int id);
        Task<List<BookingTour>> GetByUserEmailAsync(string email);
        Task<List<BookingTour>> GetByDriverEmailAsync(string email);
        Task<List<object>> GetApprovedCalendarItemsAsync();
        Task AddAsync(BookingTour booking, string adminEmail);
        Task UpdateAsync(BookingTour booking);
        Task DeleteAsync(int id);
        Task<(string ConfirmationText, string? EmailError)> ApproveAsync(int id);
        Task CancelAsync(int id);
    }
}
