using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TaxiBlitz.Domain.Identity;

namespace TaxiBlitz.Domain.Entities
{
    public class TourPost
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(1000)]

        public string Excerpt { get; set; }

        [Required]
        
        public string Content { get; set; }

        [StringLength(300)]
        public string? CoverImage { get; set; }

        [StringLength(200)]
        public string? Slug { get; set; }

        [StringLength(100)]
        public string? TourDestination { get; set; }

        public DateTime? TourDate { get; set; }

        [StringLength(500)]
        public string? Tags { get; set; }

        public int? RelatedTourId { get; set; }

        [ForeignKey("RelatedTourId")]
        public virtual Tour? RelatedTour { get; set; }

        public bool IsFeatured { get; set; }

        public bool IsPublished { get; set; }

        public int ViewCount { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public string? AuthorId { get; set; }

        [ForeignKey("AuthorId")]
        public virtual ApplicationUser? Author { get; set; }

        public virtual ICollection<TourPostImage> Images { get; set; }

        public virtual ICollection<TourComment> Comments { get; set; }

        public TourPost()
        {
            Images = new HashSet<TourPostImage>();
            Comments = new HashSet<TourComment>();
            CreatedDate = DateTime.Now;
            IsPublished = false;
            IsFeatured = false;
            ViewCount = 0;
        }
    }

    public class TourPostImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(300)]
        public string ImageUrl { get; set; }

        [StringLength(200)]
        public string Caption { get; set; }

        public int DisplayOrder { get; set; }

        public int TourPostId { get; set; }

        [ForeignKey("TourPostId")]
        public virtual TourPost TourPost { get; set; }

        public DateTime UploadedDate { get; set; }

        public TourPostImage()
        {
            UploadedDate = DateTime.Now;
            DisplayOrder = 0;
        }
    }

    public class TourComment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public bool IsApproved { get; set; }

        public int TourPostId { get; set; }

        [ForeignKey("TourPostId")]
        public virtual TourPost TourPost { get; set; }

        public string CommentAuthorId { get; set; }

        [ForeignKey("CommentAuthorId")]
        public virtual ApplicationUser CommentAuthor { get; set; }

        public int? ParentCommentId { get; set; }

        [ForeignKey("ParentCommentId")]
        public virtual TourComment ParentComment { get; set; }

        public virtual ICollection<TourComment> Replies { get; set; }

        public TourComment()
        {
            CreatedDate = DateTime.Now;
            IsApproved = false;
            Replies = new HashSet<TourComment>();
        }
    }
}

