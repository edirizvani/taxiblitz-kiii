using Moq;
using TaxiBlitz.Application.Interfaces;
using TaxiBlitz.Application.Services;
using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Tests.UnitTests.Application.Services;

public class TourStoryServiceTests
{
    private readonly Mock<ITourPostRepository> _repo = new();
    private TourStoryService Sut() => new(_repo.Object);

    // ── AddAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_SetsAuthorId()
    {
        TourPost? saved = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<TourPost>()))
             .Callback<TourPost>(p => saved = p)
             .Returns(Task.CompletedTask);

        var post = new TourPost { Title = "My Story", Excerpt = "X", Content = "Y" };
        await Sut().AddAsync(post, "author1", Array.Empty<string>(), Array.Empty<string>());

        Assert.Equal("author1", saved!.AuthorId);
    }

    [Fact]
    public async Task AddAsync_SetsSlugFromTitle()
    {
        TourPost? saved = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<TourPost>()))
             .Callback<TourPost>(p => saved = p)
             .Returns(Task.CompletedTask);

        var post = new TourPost { Title = "My First Tour", Excerpt = "X", Content = "Y" };
        await Sut().AddAsync(post, "author1", Array.Empty<string>(), Array.Empty<string>());

        Assert.Equal("my-first-tour", saved!.Slug);
    }

    [Fact]
    public async Task AddAsync_SetsCreatedDateToNow()
    {
        var before = DateTime.Now.AddSeconds(-1);
        _repo.Setup(r => r.AddAsync(It.IsAny<TourPost>())).Returns(Task.CompletedTask);

        var post = new TourPost { Title = "T", Excerpt = "E", Content = "C" };
        await Sut().AddAsync(post, "a1", Array.Empty<string>(), Array.Empty<string>());

        Assert.True(post.CreatedDate >= before && post.CreatedDate <= DateTime.Now.AddSeconds(1));
    }

    [Fact]
    public async Task AddAsync_AddsImageUrls_WhenProvided()
    {
        _repo.Setup(r => r.AddAsync(It.IsAny<TourPost>())).Returns(Task.CompletedTask);

        var post = new TourPost { Title = "T", Excerpt = "E", Content = "C" };
        await Sut().AddAsync(post, "a1",
            new[] { "http://img1.jpg", "http://img2.jpg" },
            new[] { "Cap1", "Cap2" });

        Assert.Equal(2, post.Images.Count);
    }

    [Fact]
    public async Task AddAsync_SkipsEmptyImageUrls()
    {
        _repo.Setup(r => r.AddAsync(It.IsAny<TourPost>())).Returns(Task.CompletedTask);

        var post = new TourPost { Title = "T", Excerpt = "E", Content = "C" };
        await Sut().AddAsync(post, "a1",
            new[] { "http://img1.jpg", "", "http://img3.jpg" },
            new[] { "Cap1", "", "Cap3" });

        Assert.Equal(2, post.Images.Count);
    }

    // ── AddCommentAsync ──────────────────────────────────────────────

    [Fact]
    public async Task AddCommentAsync_SetsIsApprovedTrue()
    {
        TourComment? saved = null;
        _repo.Setup(r => r.AddCommentAsync(It.IsAny<TourComment>()))
             .Callback<TourComment>(c => saved = c)
             .Returns(Task.CompletedTask);

        await Sut().AddCommentAsync(1, "Great post!", "user1");

        Assert.True(saved!.IsApproved);
    }

    [Fact]
    public async Task AddCommentAsync_SetsCorrectAuthorId()
    {
        TourComment? saved = null;
        _repo.Setup(r => r.AddCommentAsync(It.IsAny<TourComment>()))
             .Callback<TourComment>(c => saved = c)
             .Returns(Task.CompletedTask);

        await Sut().AddCommentAsync(1, "Great!", "user2");

        Assert.Equal("user2", saved!.CommentAuthorId);
    }

    // ── DeleteCommentAsync ───────────────────────────────────────────

    [Fact]
    public async Task DeleteCommentAsync_AllowsOwner()
    {
        var comment = new TourComment { Id = 1, CommentAuthorId = "owner1" };
        _repo.Setup(r => r.GetCommentByIdAsync(1)).ReturnsAsync(comment);

        await Sut().DeleteCommentAsync(1, "owner1", false);

        _repo.Verify(r => r.DeleteCommentAsync(comment), Times.Once);
    }

    [Fact]
    public async Task DeleteCommentAsync_AllowsAdmin()
    {
        var comment = new TourComment { Id = 1, CommentAuthorId = "owner1" };
        _repo.Setup(r => r.GetCommentByIdAsync(1)).ReturnsAsync(comment);

        await Sut().DeleteCommentAsync(1, "adminUser", true);

        _repo.Verify(r => r.DeleteCommentAsync(comment), Times.Once);
    }

    [Fact]
    public async Task DeleteCommentAsync_DeniesNonOwnerNonAdmin()
    {
        var comment = new TourComment { Id = 1, CommentAuthorId = "owner1" };
        _repo.Setup(r => r.GetCommentByIdAsync(1)).ReturnsAsync(comment);

        await Sut().DeleteCommentAsync(1, "otherUser", false);

        _repo.Verify(r => r.DeleteCommentAsync(It.IsAny<TourComment>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCommentAsync_DoesNothingWhenCommentNotFound()
    {
        _repo.Setup(r => r.GetCommentByIdAsync(99)).ReturnsAsync((TourComment?)null);

        await Sut().DeleteCommentAsync(99, "anyone", true);

        _repo.Verify(r => r.DeleteCommentAsync(It.IsAny<TourComment>()), Times.Never);
    }

    // ── GetRecentFeaturedAsync ───────────────────────────────────────

    [Fact]
    public async Task GetRecentFeaturedAsync_ReturnsFeaturedFirst()
    {
        var posts = new List<TourPost>
        {
            new() { Id = 1, IsPublished = true, IsFeatured = false, CreatedDate = DateTime.Now.AddDays(-2) },
            new() { Id = 2, IsPublished = true, IsFeatured = true,  CreatedDate = DateTime.Now.AddDays(-1) },
            new() { Id = 3, IsPublished = true, IsFeatured = false, CreatedDate = DateTime.Now }
        };
        _repo.Setup(r => r.GetAllForManagementAsync()).ReturnsAsync(posts);

        var result = await Sut().GetRecentFeaturedAsync(3);

        Assert.Equal(2, result[0].Id);
    }

    [Fact]
    public async Task GetRecentFeaturedAsync_OnlyIncludesPublished()
    {
        var posts = new List<TourPost>
        {
            new() { IsPublished = true,  IsFeatured = false, CreatedDate = DateTime.Now },
            new() { IsPublished = false, IsFeatured = false, CreatedDate = DateTime.Now }
        };
        _repo.Setup(r => r.GetAllForManagementAsync()).ReturnsAsync(posts);

        var result = await Sut().GetRecentFeaturedAsync(5);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetRecentFeaturedAsync_LimitsToCount()
    {
        var posts = Enumerable.Range(1, 5)
            .Select(i => new TourPost { IsPublished = true, CreatedDate = DateTime.Now })
            .ToList();
        _repo.Setup(r => r.GetAllForManagementAsync()).ReturnsAsync(posts);

        var result = await Sut().GetRecentFeaturedAsync(3);

        Assert.Equal(3, result.Count);
    }

    // ── GetByIdWithDetailsAsync / ViewCount ──────────────────────────

    [Fact]
    public async Task GetByIdWithDetailsAsync_IncrementsViewCount()
    {
        var post = new TourPost { Id = 5, IsPublished = true };
        _repo.Setup(r => r.GetByIdWithDetailsAsync(5)).ReturnsAsync(post);

        await Sut().GetByIdWithDetailsAsync(5);

        _repo.Verify(r => r.IncrementViewCountAsync(5), Times.Once);
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_WhenNotFound_DoesNotIncrementViewCount()
    {
        _repo.Setup(r => r.GetByIdWithDetailsAsync(99)).ReturnsAsync((TourPost?)null);

        await Sut().GetByIdWithDetailsAsync(99);

        _repo.Verify(r => r.IncrementViewCountAsync(It.IsAny<int>()), Times.Never);
    }

    // ── GetPublishedPagedAsync ───────────────────────────────────────

    [Fact]
    public async Task GetPublishedPagedAsync_CalculatesTotalPagesCorrectly()
    {
        _repo.Setup(r => r.CountPublishedAsync()).ReturnsAsync(10);
        _repo.Setup(r => r.GetPublishedPagedAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new List<TourPost>());

        var (_, totalPages) = await Sut().GetPublishedPagedAsync(1, 3);

        Assert.Equal(4, totalPages); // ceil(10/3)
    }

    [Fact]
    public async Task GetPublishedPagedAsync_CalculatesCorrectSkip()
    {
        _repo.Setup(r => r.CountPublishedAsync()).ReturnsAsync(30);
        _repo.Setup(r => r.GetPublishedPagedAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new List<TourPost>());

        await Sut().GetPublishedPagedAsync(3, 9);

        // page=3, pageSize=9 → skip = (3-1)*9 = 18
        _repo.Verify(r => r.GetPublishedPagedAsync(18, 9), Times.Once);
    }
}
