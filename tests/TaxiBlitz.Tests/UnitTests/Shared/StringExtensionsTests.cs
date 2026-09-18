using TaxiBlitz.Shared.Extensions;

namespace TaxiBlitz.Tests.UnitTests.Shared;

public class StringExtensionsTests
{
    // ── ToSlug ───────────────────────────────────────────────────────

    [Fact]
    public void ToSlug_LowercasesInput()
    {
        Assert.Equal("hello-world", "HELLO WORLD".ToSlug());
    }

    [Fact]
    public void ToSlug_ReplacesSpacesWithHyphens()
    {
        Assert.Equal("hello-world", "hello world".ToSlug());
    }

    [Fact]
    public void ToSlug_RemovesSpecialCharacters()
    {
        var result = "Café & Bár!".ToSlug();
        Assert.DoesNotContain("&", result);
        Assert.DoesNotContain("!", result);
    }

    [Fact]
    public void ToSlug_CollapseMultipleHyphens()
    {
        Assert.Equal("hello-world", "hello---world".ToSlug());
    }

    [Fact]
    public void ToSlug_TrimsLeadingTrailingHyphens()
    {
        Assert.Equal("hello", "-hello-".ToSlug());
    }

    [Fact]
    public void ToSlug_WithEmptyString_ReturnsEmpty()
    {
        Assert.Equal("", "".ToSlug());
    }

    [Fact]
    public void ToSlug_WithNull_ReturnsEmpty()
    {
        Assert.Equal("", ((string?)null)!.ToSlug());
    }

    [Fact]
    public void ToSlug_WithUnderscores_ConvertsToHyphens()
    {
        Assert.Equal("hello-world", "hello_world".ToSlug());
    }

    [Fact]
    public void ToSlug_WithMixedWhitespace_Normalizes()
    {
        Assert.Equal("my-first-tour", "My First Tour".ToSlug());
    }

    // ── Truncate ─────────────────────────────────────────────────────

    [Fact]
    public void Truncate_ReturnsOriginal_WhenShorterThanMax()
    {
        Assert.Equal("Hi", "Hi".Truncate(10));
    }

    [Fact]
    public void Truncate_ReturnsOriginal_WhenExactlyMax()
    {
        Assert.Equal("Hello", "Hello".Truncate(5));
    }

    [Fact]
    public void Truncate_AddsEllipsis_WhenLongerThanMax()
    {
        var result = "Hello World".Truncate(5);
        Assert.StartsWith("Hello", result);
        Assert.Contains("…", result);
    }

    [Fact]
    public void Truncate_WithNullInput_ReturnsNull()
    {
        Assert.Null(((string?)null).Truncate(5));
    }

    [Fact]
    public void Truncate_WithEmptyInput_ReturnsEmpty()
    {
        Assert.Equal("", "".Truncate(5));
    }

    [Fact]
    public void Truncate_WithZeroMax_ReturnsEllipsis()
    {
        Assert.Equal("…", "Hello".Truncate(0));
    }
}
