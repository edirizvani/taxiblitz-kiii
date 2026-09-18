using Microsoft.AspNetCore.Identity;

namespace TaxiBlitz.Domain.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName  { get; set; } = string.Empty;
        public string City      { get; set; } = string.Empty;
        public string Country   { get; set; } = string.Empty;
        public string ProfilePictureUrl { get; set; } = string.Empty;
        public string Bio       { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string Gender    { get; set; } = string.Empty;
        public string PreferredLanguage { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
