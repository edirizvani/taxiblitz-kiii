using TaxiBlitz.Domain.Identity;

namespace TaxiBlitz.Domain.Entities
{
    public class FavouriteTour
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int TourId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser User { get; set; }
        public Tour Tour { get; set; }
    }
}
