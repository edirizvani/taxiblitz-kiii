using System;
using System.ComponentModel.DataAnnotations;

namespace TaxiBlitz.Domain.Entities
{
    public class GeoCoordinateCache
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(250)]
        public string Query { get; set; }

        [StringLength(500)]
        public string DisplayName { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        [StringLength(100)]
        public string Provider { get; set; }

        public DateTime ResolvedAtUtc { get; set; }
    }
}

