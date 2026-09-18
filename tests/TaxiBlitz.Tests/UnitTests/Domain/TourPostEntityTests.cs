using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Tests.UnitTests.Domain;

public class TourPostEntityTests
{
    [Fact]
    public void Constructor_SetsCreatedDateToNow()
    {
        var before = DateTime.Now.AddSeconds(-1);
        var post = new TourPost();
        Assert.True(post.CreatedDate >= before && post.CreatedDate <= DateTime.Now.AddSeconds(1));
    }

    [Fact]
    public void Constructor_SetsIsPublishedFalse()
    {
        var post = new TourPost();
        Assert.False(post.IsPublished);
    }

    [Fact]
    public void Constructor_SetsIsFeaturedFalse()
    {
        var post = new TourPost();
        Assert.False(post.IsFeatured);
    }

    [Fact]
    public void Constructor_SetsViewCountToZero()
    {
        var post = new TourPost();
        Assert.Equal(0, post.ViewCount);
    }

    [Fact]
    public void Constructor_InitializesEmptyCollections()
    {
        var post = new TourPost();
        Assert.NotNull(post.Images);
        Assert.NotNull(post.Comments);
        Assert.Empty(post.Images);
        Assert.Empty(post.Comments);
    }
}
