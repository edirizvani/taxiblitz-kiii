using System;
using TaxiBlitz.Domain.Identity;

namespace TaxiBlitz.Domain.Entities
{
    public class ReferralUsage
    {
        public int Id { get; set; }
        public int ReferralCodeId { get; set; }
        public string UsedByUserId { get; set; }
        public int? BookingId { get; set; }
        public decimal DiscountAmount { get; set; }
        public DateTime UsedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CommissionPaidAt { get; set; }

        public ReferralCode ReferralCode { get; set; }
        public ApplicationUser UsedByUser { get; set; }
        public BookingTour? Booking { get; set; }
    }
}
