using TaxiBlitz.Application.Interfaces;
using TaxiBlitz.Application.Services.Interfaces;
using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookings;
        private readonly IEmailService      _email;
        public BookingService(IBookingRepository bookings, IEmailService email)
        {
            _bookings = bookings;
            _email    = email;
        }

        public Task<List<BookingTour>> GetAllWithIncludesAsync() => _bookings.GetAllWithIncludesAsync();

        public Task<BookingTour?> GetByIdAsync(int id) => _bookings.GetByIdAsync(id);

        public Task<List<BookingTour>> GetByUserEmailAsync(string email) => _bookings.GetByUserEmailAsync(email);

        public async Task<List<BookingTour>> GetByDriverEmailAsync(string email)
        {
            // Driver entity has email; find driver id first via repository
            // We rely on the fact that bookings are fetched by driver email indirectly:
            // caller should already have resolved driverId. This method returns bookings by email lookup.
            // Since IDriverRepository isn't injected here, we join via bookings that include driver nav.
            var all = await _bookings.GetAllWithIncludesAsync();
            return all.Where(b => b.Driver?.Email == email).ToList();
        }

        public async Task<List<object>> GetApprovedCalendarItemsAsync()
        {
            var approved = await _bookings.GetApprovedAsync();
            return approved.Select(b => (object)new
            {
                title      = b.Tour?.Title,
                start      = b.BookingDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                end        = b.BookingDateTime.AddHours(2).ToString("yyyy-MM-ddTHH:mm:ss"),
                color      = "#28a745",
                driverName = b.Driver?.Name
            }).ToList();
        }

        public async Task AddAsync(BookingTour booking, string adminEmail)
        {
            booking.Status = "Pending";
            await _bookings.AddAsync(booking);
            await _bookings.SaveChangesAsync();

            var saved = await _bookings.GetByIdWithIncludesAsync(booking.Id);
            if (saved != null)
                _email.SendAdminNewBookingAlert(saved, adminEmail);
        }

        public async Task UpdateAsync(BookingTour booking)
        {
            await _bookings.UpdateAsync(booking);
            await _bookings.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var booking = await _bookings.GetByIdAsync(id);
            if (booking == null) return;
            await _bookings.DeleteAsync(booking);
            await _bookings.SaveChangesAsync();
        }

        public async Task<(string ConfirmationText, string? EmailError)> ApproveAsync(int id)
        {
            var booking = await _bookings.GetByIdWithIncludesAsync(id);
            if (booking == null) return ("", "Booking not found.");

            booking.Status = "Approved";
            await _bookings.SaveChangesAsync();

            string confirmationText = _email.BuildConfirmationText(booking);
            string? emailError      = _email.SendBookingConfirmation(booking);
            return (confirmationText, string.IsNullOrEmpty(emailError) ? null : emailError);
        }

        public async Task CancelAsync(int id)
        {
            var booking = await _bookings.GetByIdAsync(id);
            if (booking == null) return;
            booking.Status = "Canceled";
            await _bookings.SaveChangesAsync();
        }
    }
}
