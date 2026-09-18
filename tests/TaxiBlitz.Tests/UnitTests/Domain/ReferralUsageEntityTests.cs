using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Tests.UnitTests.Domain;

public class ReferralUsageEntityTests
{
    [Fact]
    public void CommissionPaidAt_DefaultsToNull()
    {
        var usage = new ReferralUsage();
        Assert.Null(usage.CommissionPaidAt);
    }

    [Fact]
    public void CommissionPaidAt_CanBeSetAndRead()
    {
        var now   = DateTime.UtcNow;
        var usage = new ReferralUsage { CommissionPaidAt = now };
        Assert.Equal(now, usage.CommissionPaidAt);
    }

    [Fact]
    public void UsedAt_DefaultsToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var usage  = new ReferralUsage();
        Assert.True(usage.UsedAt >= before && usage.UsedAt <= DateTime.UtcNow.AddSeconds(1));
    }
}
