using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TaxiBlitz.Domain.Entities
{
    public class PersonalizedDrive
    {
        public int Id { get; set; }  // Primary Key

        [Required]
        public int DriverId { get; set; }  // Foreign Key for Driver

        public virtual Driver Driver { get; set; }  // Navigation property for Driver

        [Required]
        [StringLength(100)]
        public string StartingPoint { get; set; }  // Required Starting Point

        [Required]
        [StringLength(100)]
        public string EndingPoint { get; set; }  // Required Ending Point

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }  // Required Date

        [Required]
        [StringLength(2000)]
        public string Description { get; set; }  // Required Description

        // Price initially undefined, but will be managed by the administrator
        public decimal? Price { get; set; }

        // Foreign Key relationships (if applicable)
        // Example: If you plan to link PersonalizedDrive to Tour, you can add a TourId here:
        // public int? TourId { get; set; }  // Foreign Key to a Tour, nullable if not necessary

    }
}
