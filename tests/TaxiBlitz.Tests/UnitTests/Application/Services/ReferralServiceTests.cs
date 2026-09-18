using Moq;
using TaxiBlitz.Application.Interfaces;
using TaxiBlitz.Application.Services;
using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Tests.UnitTests.Application.Services;

public class ReferralServiceTests
{
    private readonly Mock<IReferralRepository> _repo = new();
    private ReferralService Sut() => new(_repo.Object);

    // ── GetOrCreateCodeAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetOrCreateCodeAsync_WhenCodeExists_ReturnsExistingWithoutCreating()
    {
        var existing = new ReferralCode { Code = "TB1234", OwnerId = "user1" };
        _repo.Setup(r => r.GetByOwnerIdAsync("user1")).ReturnsAsync(existing);

        var result = await Sut().GetOrCreateCodeAsync("user1");

        Assert.Equal(existing, result);
        _repo.Verify(r => r.CreateAsync(It.IsAny<ReferralCode>()), Times.Never);
    }

    [Fact]
    public async Task GetOrCreateCodeAsync_WhenNoCode_CreatesAndReturns()
    {
        _repo.Setup(r => r.GetByOwnerIdAsync("user1")).ReturnsAsync((ReferralCode?)null);
        _repo.Setup(r => r.CreateAsync(It.IsAny<ReferralCode>()))
             .ReturnsAsync<ReferralCode, IReferralRepository, ReferralCode>(c => c);

        var result = await Sut().GetOrCreateCodeAsync("user1");

        Assert.Equal("user1", result.OwnerId);
        _repo.Verify(r => r.CreateAsync(It.IsAny<ReferralCode>()), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateCodeAsync_GeneratedCode_HasTBPrefix()
    {
        _repo.Setup(r => r.GetByOwnerIdAsync(It.IsAny<string>())).ReturnsAsync((ReferralCode?)null);
        _repo.Setup(r => r.CreateAsync(It.IsAny<ReferralCode>()))
             .ReturnsAsync<ReferralCode, IReferralRepository, ReferralCode>(c => c);

        var result = await Sut().GetOrCreateCodeAsync("someuser");

        Assert.StartsWith("TB", result.Code);
    }

    [Fact]
    public async Task GetOrCreateCodeAsync_GeneratedCode_HasFourDigitSuffix()
    {
        _repo.Setup(r => r.GetByOwnerIdAsync(It.IsAny<string>())).ReturnsAsync((ReferralCode?)null);
        _repo.Setup(r => r.CreateAsync(It.IsAny<ReferralCode>()))
             .ReturnsAsync<ReferralCode, IReferralRepository, ReferralCode>(c => c);

        var result = await Sut().GetOrCreateCodeAsync("user1");

        Assert.Matches(@"^TB\d{4}$", result.Code);
    }

    // ── ValidateCodeAsync ────────────────────────────────────────────

    [Fact]
    public async Task ValidateCodeAsync_WithEmptyCode_ReturnsFalse()
    {
        var (valid, _, message, _) = await Sut().ValidateCodeAsync("", "user1");
        Assert.False(valid);
        Assert.False(string.IsNullOrEmpty(message));
    }

    [Fact]
    public async Task ValidateCodeAsync_WithWhitespaceCode_ReturnsFalse()
    {
        var (valid, _, _, _) = await Sut().ValidateCodeAsync("   ", "user1");
        Assert.False(valid);
    }

    [Fact]
    public async Task ValidateCodeAsync_WhenCodeNotFound_ReturnsFalse()
    {
        _repo.Setup(r => r.GetByCodeAsync(It.IsAny<string>())).ReturnsAsync((ReferralCode?)null);

        var (valid, _, message, _) = await Sut().ValidateCodeAsync("TB9999", "user1");

        Assert.False(valid);
        Assert.Contains("not found", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateCodeAsync_WhenCodeInactive_ReturnsFalse()
    {
        var code = new ReferralCode { Code = "TB1234", OwnerId = "owner1", IsActive = false };
        _repo.Setup(r => r.GetByCodeAsync("TB1234")).ReturnsAsync(code);

        var (valid, _, message, _) = await Sut().ValidateCodeAsync("TB1234", "user1");

        Assert.False(valid);
        Assert.Contains("no longer active", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateCodeAsync_WhenOwnerTriesToUseOwnCode_ReturnsFalse()
    {
        var code = new ReferralCode { Code = "TB1234", OwnerId = "user1", IsActive = true };
        _repo.Setup(r => r.GetByCodeAsync("TB1234")).ReturnsAsync(code);

        var (valid, _, message, _) = await Sut().ValidateCodeAsync("TB1234", "user1");

        Assert.False(valid);
        Assert.Contains("own", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateCodeAsync_WithValidCode_ReturnsTrue()
    {
        var code = new ReferralCode { Code = "TB1234", Id = 1, OwnerId = "owner1", IsActive = true, DiscountPercent = 5m };
        _repo.Setup(r => r.GetByCodeAsync("TB1234")).ReturnsAsync(code);
        _repo.Setup(r => r.HasUserUsedCodeAsync(1, "user2")).ReturnsAsync(false);

        var (valid, discount, _, codeId) = await Sut().ValidateCodeAsync("TB1234", "user2");

        Assert.True(valid);
        Assert.Equal(5m, discount);
        Assert.Equal(1, codeId);
    }

    [Fact]
    public async Task ValidateCodeAsync_NormalizesCodeToUppercase()
    {
        _repo.Setup(r => r.GetByCodeAsync("TB1234")).ReturnsAsync((ReferralCode?)null);

        await Sut().ValidateCodeAsync("tb1234", "user1");

        _repo.Verify(r => r.GetByCodeAsync("TB1234"), Times.Once);
    }

    // ── RecordUsageAsync ─────────────────────────────────────────────

    [Fact]
    public async Task RecordUsageAsync_WhenCodeNotFound_DoesNothing()
    {
        _repo.Setup(r => r.GetByCodeAsync(It.IsAny<string>())).ReturnsAsync((ReferralCode?)null);

        await Sut().RecordUsageAsync("TB9999", 1, "user1", 50m);

        _repo.Verify(r => r.AddUsageAsync(It.IsAny<ReferralUsage>()), Times.Never);
        _repo.Verify(r => r.IncrementUsageAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RecordUsageAsync_WhenCodeFound_AddsUsageAndIncrements()
    {
        var code = new ReferralCode { Id = 7, Code = "TB1234" };
        _repo.Setup(r => r.GetByCodeAsync("TB1234")).ReturnsAsync(code);

        await Sut().RecordUsageAsync("TB1234", 1, "user1", 50m);

        _repo.Verify(r => r.AddUsageAsync(It.Is<ReferralUsage>(u =>
            u.ReferralCodeId == 7 &&
            u.UsedByUserId   == "user1" &&
            u.BookingId      == 1 &&
            u.DiscountAmount == 50m)), Times.Once);
        _repo.Verify(r => r.IncrementUsageAsync(7), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
