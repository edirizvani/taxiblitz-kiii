using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TaxiBlitz.Domain.Entities
{
    public class Review
    {
        [Key]
        public int Id { get; set; } // Unique identifier for the review
        [Required]
        [StringLength(100)]
        public string ReviewerName { get; set; } // Name of the person who wrote the review
        [Required]
        [StringLength(2000)]
        public string Text { get; set; } // Text content of the review
        public DateTime ReviewDate { get; set; } // Date the review was posted
        [StringLength(500)]
        public string ImageUrl { get; set; } // URL to an image of the reviewer (optional)
        [Range(1, 5)]
        public int? Rating { get; set; } // Optional 1-5 rating used for aggregate rating schema

        // Additional properties can be added here as needed
    }
}
