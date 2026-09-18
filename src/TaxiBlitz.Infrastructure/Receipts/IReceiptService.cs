using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Infrastructure.Receipts
{
    public interface IReceiptService
    {
        byte[] GenerateBookingReceipt(BookingTour booking);
    }
}
