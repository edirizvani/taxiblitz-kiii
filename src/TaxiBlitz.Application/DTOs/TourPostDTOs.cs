using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaxiBlitz.Application.DTOs
{
    public class TourPostDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Excerpt is required")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Excerpt must be between 10 and 1000 characters")]
        public string Excerpt { get; set; }

        [Required(ErrorMessage = "Content is required")]
        [MinLength(50, ErrorMessage = "Content must be at least 50 characters")]
        public string Content { get; set; }

        [Url(ErrorMessage = "Cover image must be a valid URL")]
        public string CoverImage { get; set; }

        public string Slug { get; set; }

        [StringLength(100)]
        public string TourDestination { get; set; }

        [DataType(DataType.Date)]
        public DateTime? TourDate { get; set; }

        public string Tags { get; set; }

        public int? RelatedTourId { get; set; }

        public bool IsFeatured { get; set; }

        public bool IsPublished { get; set; }

        public int ViewCount { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public string AuthorId { get; set; }

        public string AuthorName { get; set; }

        public List<TourPostImageDTO> Images { get; set; }

        public List<TourCommentDTO> Comments { get; set; }

        public TourPostDTO()
        {
            Images = new List<TourPostImageDTO>();
            Comments = new List<TourCommentDTO>();
            CreatedDate = DateTime.Now;
        }
    }

    public class TourPostImageDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Image URL is required")]
        [Url(ErrorMessage = "Image must be a valid URL")]
        public string ImageUrl { get; set; }

        [StringLength(200)]
        public string Caption { get; set; }

        public int DisplayOrder { get; set; }

        public int TourPostId { get; set; }

        public DateTime UploadedDate { get; set; }
    }

    public class TourCommentDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Comment is required")]
        [StringLength(1000, MinimumLength = 2, ErrorMessage = "Comment must be between 2 and 1000 characters")]
        public string Content { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public bool IsApproved { get; set; }

        public int TourPostId { get; set; }

        public string CommentAuthorId { get; set; }

        public string CommentAuthorName { get; set; }

        public int? ParentCommentId { get; set; }

        public List<TourCommentDTO> Replies { get; set; }

        public TourCommentDTO()
        {
            CreatedDate = DateTime.Now;
            IsApproved = false;
            Replies = new List<TourCommentDTO>();
        }
    }

    public class TourStoriesIndexDTO
    {
        public List<TourPostDTO> Posts { get; set; }

        public int TotalPages { get; set; }

        public int CurrentPage { get; set; }

        public int TotalCount { get; set; }

        public TourStoriesIndexDTO()
        {
            Posts = new List<TourPostDTO>();
            CurrentPage = 1;
        }
    }
}

