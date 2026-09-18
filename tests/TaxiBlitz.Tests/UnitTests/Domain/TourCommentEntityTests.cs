using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Tests.UnitTests.Domain;

public class TourCommentEntityTests
{
    [Fact]
    public void Constructor_SetsIsApprovedFalse()
    {
        var comment = new TourComment();
        Assert.False(comment.IsApproved);
    }

    [Fact]
    public void Constructor_SetsCreatedDateToNow()
    {
        var before = DateTime.Now.AddSeconds(-1);
        var comment = new TourComment();
        Assert.True(comment.CreatedDate >= before && comment.CreatedDate <= DateTime.Now.AddSeconds(1));
    }

    [Fact]
    public void Constructor_InitializesEmptyRepliesCollection()
    {
        var comment = new TourComment();
        Assert.NotNull(comment.Replies);
        Assert.Empty(comment.Replies);
    }

    [Fact]
    public void ParentCommentId_IsNullable()
    {
        var comment = new TourComment { ParentCommentId = null };
        Assert.Null(comment.ParentCommentId);
    }
}
