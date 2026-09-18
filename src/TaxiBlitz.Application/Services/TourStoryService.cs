using TaxiBlitz.Application.Interfaces;
using TaxiBlitz.Application.Services.Interfaces;
using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Shared.Extensions;

namespace TaxiBlitz.Application.Services
{
    public class TourStoryService : ITourStoryService
    {
        private readonly ITourPostRepository _posts;
        public TourStoryService(ITourPostRepository posts) => _posts = posts;

        public async Task<(List<TourPost> Posts, int TotalPages)> GetPublishedPagedAsync(int page, int pageSize)
        {
            int total      = await _posts.CountPublishedAsync();
            int totalPages = (int)Math.Ceiling((double)total / pageSize);
            var posts      = await _posts.GetPublishedPagedAsync((page - 1) * pageSize, pageSize);
            return (posts, totalPages);
        }

        public async Task<TourPost?> GetByIdWithDetailsAsync(int id)
        {
            var post = await _posts.GetByIdWithDetailsAsync(id);
            if (post != null) await _posts.IncrementViewCountAsync(id);
            return post;
        }

        public Task<TourPost?> GetByIdForEditAsync(int id) => _posts.GetByIdWithImagesAsync(id);

        public Task<List<TourPost>> GetRelatedAsync(TourPost post, int count) =>
            _posts.GetRelatedAsync(post.Id, post.Tags, post.TourDestination, count);

        public Task<List<TourPost>> GetAllForManagementAsync() => _posts.GetAllForManagementAsync();

        public async Task<List<TourPost>> GetRecentFeaturedAsync(int count)
        {
            var all = await _posts.GetAllForManagementAsync();
            return all
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.CreatedDate)
                .Take(count)
                .ToList();
        }

        public async Task AddAsync(TourPost post, string authorId, string[] imageUrls, string[] imageCaptions)
        {
            post.AuthorId    = authorId;
            post.CreatedDate = DateTime.Now;
            post.Slug        = post.Title.ToSlug();
            post.CoverImage  ??= string.Empty;
            post.TourDestination ??= string.Empty;
            post.Tags        ??= string.Empty;

            await _posts.AddAsync(post);

            if (imageUrls != null)
            {
                for (int i = 0; i < imageUrls.Length; i++)
                {
                    if (!string.IsNullOrEmpty(imageUrls[i]))
                    {
                        post.Images.Add(new TourPostImage
                        {
                            ImageUrl     = imageUrls[i],
                            Caption      = imageCaptions?.ElementAtOrDefault(i) ?? "",
                            DisplayOrder = i,
                            UploadedDate = DateTime.Now
                        });
                    }
                }
            }

            await _posts.SaveChangesAsync();
        }

        public async Task UpdateAsync(TourPost incoming, string[] imageUrls, string[] imageCaptions)
        {
            var existing = await _posts.GetByIdWithImagesAsync(incoming.Id);
            if (existing == null) return;

            existing.Title           = incoming.Title;
            existing.Excerpt         = incoming.Excerpt;
            existing.Content         = incoming.Content;
            existing.CoverImage      = incoming.CoverImage ?? string.Empty;
            existing.TourDestination = incoming.TourDestination ?? string.Empty;
            existing.TourDate        = incoming.TourDate;
            existing.Tags            = incoming.Tags ?? string.Empty;
            existing.RelatedTourId   = incoming.RelatedTourId;
            existing.IsPublished     = incoming.IsPublished;
            existing.IsFeatured      = incoming.IsFeatured;
            existing.UpdatedDate     = DateTime.Now;

            existing.Images.Clear();

            if (imageUrls != null)
            {
                for (int i = 0; i < imageUrls.Length; i++)
                {
                    if (!string.IsNullOrEmpty(imageUrls[i]))
                    {
                        existing.Images.Add(new TourPostImage
                        {
                            ImageUrl     = imageUrls[i],
                            Caption      = imageCaptions?.ElementAtOrDefault(i) ?? "",
                            DisplayOrder = i,
                            UploadedDate = DateTime.Now
                        });
                    }
                }
            }

            await _posts.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var post = await _posts.GetByIdAsync(id);
            if (post == null) return;
            await _posts.DeleteAsync(post);
            await _posts.SaveChangesAsync();
        }

        public async Task AddCommentAsync(int postId, string content, string authorId)
        {
            var comment = new TourComment
            {
                Content         = content,
                TourPostId      = postId,
                CommentAuthorId = authorId,
                CreatedDate     = DateTime.Now,
                IsApproved      = true
            };
            await _posts.AddCommentAsync(comment);
            await _posts.SaveChangesAsync();
        }

        public async Task DeleteCommentAsync(int commentId, string currentUserId, bool isAdmin)
        {
            var comment = await _posts.GetCommentByIdAsync(commentId);
            if (comment == null) return;
            if (comment.CommentAuthorId != currentUserId && !isAdmin) return;
            await _posts.DeleteCommentAsync(comment);
            await _posts.SaveChangesAsync();
        }
    }
}
