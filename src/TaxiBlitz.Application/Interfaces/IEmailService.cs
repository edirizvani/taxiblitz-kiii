using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Application.Interfaces
{
    public interface IEmailService
    {
        void SendAdminNewBookingAlert(BookingTour booking, string adminEmail);
        string BuildConfirmationText(BookingTour booking);
        string SendBookingConfirmation(BookingTour booking);
    }
}
