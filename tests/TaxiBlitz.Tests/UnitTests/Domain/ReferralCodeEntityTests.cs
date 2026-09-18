using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Tests.UnitTests.Domain;

public class ReferralCodeEntityTests
{
    [Fact]
    public void DiscountPercent_DefaultsFive()
    {
        var code = new ReferralCode();
        Assert.Equal(5m, code.DiscountPercent);
    }

    [Fact]
    public void IsActive_DefaultsTrue()
    {
        var code = new ReferralCode();
        Assert.True(code.IsActive);
    }

    [Fact]
    public void UsageCount_DefaultsZero()
    {
        var code = new ReferralCode();
        Assert.Equal(0, code.UsageCount);
    }

    [Fact]
    public void CreatedAt_DefaultsToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var code = new ReferralCode();
        Assert.True(code.CreatedAt >= before && code.CreatedAt <= DateTime.UtcNow.AddSeconds(1));
    }
}
