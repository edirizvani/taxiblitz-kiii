using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TaxiBlitz.Domain.Entities
{
    public class Driver
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }

        public string? Email { get; set; }
        public string? Bio { get; set; }
        public string? PhotoProfileUrl { get; set; }
        public List<string> PictureUrls { get; set; } = new();

        // Extended profile attributes — all nullable so existing records are unaffected
        [Range(0.0, 5.0)]
        public double? Rating { get; set; }
        public string? Languages { get; set; }      // e.g. "English, Macedonian, German"
        public string? VehicleType { get; set; }    // e.g. "Mercedes Vito 8-seater"
        public string? CoverageAreas { get; set; }  // e.g. "Ohrid, Struga, Bitola, Skopje"
        public int? ExperienceYears { get; set; }
        public int? TripsCompleted { get; set; }
        public string? Specialties { get; set; }    // e.g. "Airport transfers, Day tours"
    }
}
