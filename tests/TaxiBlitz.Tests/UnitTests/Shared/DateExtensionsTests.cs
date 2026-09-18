using TaxiBlitz.Shared.Extensions;

namespace TaxiBlitz.Tests.UnitTests.Shared;

public class DateExtensionsTests
{
    [Fact]
    public void ToDisplayDate_DateTime_FormatsCorrectly()
    {
        var date = new DateTime(2025, 1, 15);
        Assert.Equal("January 15, 2025", date.ToDisplayDate());
    }

    [Fact]
    public void ToDisplayDate_DateTime_HandlesEndOfYear()
    {
        var date = new DateTime(2024, 12, 31);
        Assert.Equal("December 31, 2024", date.ToDisplayDate());
    }

    [Fact]
    public void ToDisplayDate_NullableDateTime_WhenHasValue_Formats()
    {
        DateTime? date = new DateTime(2025, 6, 1);
        Assert.Equal("June 01, 2025", date.ToDisplayDate());
    }

    [Fact]
    public void ToDisplayDate_NullableDateTime_WhenNull_ReturnsEmpty()
    {
        DateTime? date = null;
        Assert.Equal("", date.ToDisplayDate());
    }

    [Fact]
    public void ToDisplayDate_SingleDigitDay_PadsCorrectly()
    {
        var date = new DateTime(2025, 3, 5);
        Assert.Equal("March 05, 2025", date.ToDisplayDate());
    }
}
