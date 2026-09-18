using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Domain.Identity;
using TaxiBlitz.Persistence;
using TaxiBlitz.Persistence.Repositories;
using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Repositories;

public class TourPostRepositoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly TourPostRepository _repo;

    public TourPostRepositoryTests()
    {
        _db   = InMemoryDbContextFactory.Create();
        _repo = new TourPostRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    private TourPost Published(string title = "Published", DateTime? created = null, bool featured = false) => new TourPost
    {
        Title = title, Excerpt = "E", Content = "C", IsPublished = true, IsFeatured = featured,
        CreatedDate = created ?? DateTime.Now
    };

    private TourPost Unpublished(string title = "Draft") => new TourPost
    {
        Title = title, Excerpt = "E", Content = "C", IsPublished = false, CreatedDate = DateTime.Now
    };

    [Fact]
    public async Task GetPublishedPagedAsync_ReturnsOnlyPublished()
    {
        _db.TourPosts.AddRange(Published("A"), Published("B"), Unpublished("C"));
        await _db.SaveChangesAsync();

        var result = await _repo.GetPublishedPagedAsync(0, 100);

        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.True(p.IsPublished));
    }

    [Fact]
    public async Task GetPublishedPagedAsync_OrdersByCreatedDateDesc()
    {
        _db.TourPosts.AddRange(
            Published("Old",  DateTime.Now.AddDays(-3)),
            Published("New",  DateTime.Now),
            Published("Mid",  DateTime.Now.AddDays(-1)));
        await _db.SaveChangesAsync();

        var result = await _repo.GetPublishedPagedAsync(0, 100);

        Assert.Equal("New", result[0].Title);
    }

    [Fact]
    public async Task GetPublishedPagedAsync_RespectsSkipAndTake()
    {
        for (int i = 1; i <= 5; i++)
            _db.TourPosts.Add(Published($"Post {i}", DateTime.Now.AddDays(-i)));
        await _db.SaveChangesAsync();

        var result = await _repo.GetPublishedPagedAsync(2, 2);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task CountPublishedAsync_CountsOnlyPublished()
    {
        _db.TourPosts.AddRange(Published("A"), Published("B"), Unpublished("C"));
        await _db.SaveChangesAsync();

        var count = await _repo.CountPublishedAsync();

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_ReturnsNullForUnpublished()
    {
        var post = Unpublished();
        _db.TourPosts.Add(post);
        await _db.SaveChangesAsync();

        var result = await _repo.GetByIdWithDetailsAsync(post.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdWithDetailsAsync_ReturnsPublishedPost()
    {
        var post = Published("Detail Test");
        _db.TourPosts.Add(post);
        await _db.SaveChangesAsync();

        var result = await _repo.GetByIdWithDetailsAsync(post.Id);

        Assert.NotNull(result);
        Assert.Equal("Detail Test", result!.Title);
    }

    [Fact]
    public async Task GetRelatedAsync_ExcludesCurrentPost()
    {
        _db.TourPosts.AddRange(
            new TourPost { Id = 0, Title = "Post1", Excerpt = "E", Content = "C", IsPublished = true, Tags = "adventure", CreatedDate = DateTime.Now },
            new TourPost { Id = 0, Title = "Post2", Excerpt = "E", Content = "C", IsPublished = true, Tags = "adventure", CreatedDate = DateTime.Now },
            new TourPost { Id = 0, Title = "Post3", Excerpt = "E", Content = "C", IsPublished = true, Tags = "adventure", CreatedDate = DateTime.Now });
        await _db.SaveChangesAsync();

        var excludeId = _db.TourPosts.First().Id;
        var result = await _repo.GetRelatedAsync(excludeId, "adventure", null, 10);

        Assert.DoesNotContain(result, p => p.Id == excludeId);
    }

    [Fact]
    public async Task GetRelatedAsync_LimitsCount()
    {
        for (int i = 0; i < 5; i++)
            _db.TourPosts.Add(new TourPost { Title = $"P{i}", Excerpt = "E", Content = "C", IsPublished = true, Tags = "adventure", CreatedDate = DateTime.Now });
        await _db.SaveChangesAsync();

        var result = await _repo.GetRelatedAsync(0, "adventure", null, 2);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task IncrementViewCountAsync_IncrementsCorrectly()
    {
        var post = Published("ViewTest");
        post.ViewCount = 5;
        _db.TourPosts.Add(post);
        await _db.SaveChangesAsync();

        await _repo.IncrementViewCountAsync(post.Id);

        Assert.Equal(6, _db.TourPosts.Find(post.Id)!.ViewCount);
    }

    [Fact]
    public async Task IncrementViewCountAsync_WhenNotFound_DoesNotThrow()
    {
        await _repo.IncrementViewCountAsync(9999);
        // No exception = pass
    }

    [Fact]
    public async Task GetCommentByIdAsync_ReturnsComment()
    {
        var post = Published();
        _db.TourPosts.Add(post);
        await _db.SaveChangesAsync();

        var comment = new TourComment { Content = "Nice!", TourPostId = post.Id, CommentAuthorId = "u1" };
        _db.TourComments.Add(comment);
        await _db.SaveChangesAsync();

        var result = await _repo.GetCommentByIdAsync(comment.Id);

        Assert.NotNull(result);
        Assert.Equal("Nice!", result!.Content);
    }

    [Fact]
    public async Task AddCommentAsync_PersistsComment()
    {
        var post = Published();
        _db.TourPosts.Add(post);
        await _db.SaveChangesAsync();

        var comment = new TourComment { Content = "Great!", TourPostId = post.Id, CommentAuthorId = "u1" };
        await _repo.AddCommentAsync(comment);
        await _repo.SaveChangesAsync();

        Assert.Single(_db.TourComments);
    }

    [Fact]
    public async Task DeleteCommentAsync_RemovesComment()
    {
        var post = Published();
        _db.TourPosts.Add(post);
        await _db.SaveChangesAsync();

        var comment = new TourComment { Content = "ToDelete", TourPostId = post.Id, CommentAuthorId = "u1" };
        _db.TourComments.Add(comment);
        await _db.SaveChangesAsync();

        await _repo.DeleteCommentAsync(comment);
        await _db.SaveChangesAsync();

        Assert.Empty(_db.TourComments);
    }

    [Fact]
    public async Task GetAllForManagementAsync_IncludesAll_OrderedByCreatedDateDesc()
    {
        // Use explicit dates so ordering is deterministic (Unpublished ctor also sets DateTime.Now)
        var old   = Published("Old",  DateTime.Now.AddDays(-2));
        var draft = Unpublished("Draft"); draft.CreatedDate = DateTime.Now.AddDays(-1);
        var fresh = Published("New",  DateTime.Now);
        _db.TourPosts.AddRange(old, draft, fresh);
        await _db.SaveChangesAsync();

        var result = await _repo.GetAllForManagementAsync();

        Assert.Equal(3, result.Count);
        Assert.Equal("New", result[0].Title);
    }
}
