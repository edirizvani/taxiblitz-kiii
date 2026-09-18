using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TaxiBlitz.Domain.Entities
{
    public class BookingTour
    {
        [Key]
        public int Id { get; set; } // Primary Key
        [Required]
        [StringLength(100, ErrorMessage = "BookMaker name cannot exceed 100 characters.")]
        public String NameOfBookMaker { get; set; }

        [ForeignKey("Tour")]
        public int TourId { get; set; } // Foreign Key referencing Tours

        [ForeignKey("Driver")]
        public int? DriverId { get; set; } // Foreign Key referencing Drivers (nullable)

        [Required]
        [StringLength(256)]
        public string CustomerEmail { get; set; } // Captured during booking, from logged-in user

        [Required]
        public string PhoneNumber { get; set; } // Contact number for the client

        [Required]
        public int NumberOfPeople { get; set; } // Number of people for the booking

        [Required]
        public DateTime BookingDateTime { get; set; } // Date and time of the trip

        public string Status { get; set; } = "Waiting for Approval";

        public decimal? DiscountAmount { get; set; }
        public int? ReferralCodeId { get; set; }

        public virtual Tour? Tour { get; set; }
        public virtual Driver? Driver { get; set; }
        public virtual ReferralCode? ReferralCode { get; set; }
    }
}
