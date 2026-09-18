using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Tests.UnitTests.Domain;

public class BookingTourEntityTests
{
    [Fact]
    public void Status_DefaultsToWaitingForApproval()
    {
        var booking = new BookingTour();
        Assert.Equal("Waiting for Approval", booking.Status);
    }

    [Fact]
    public void DiscountAmount_IsNullableDecimal()
    {
        var booking = new BookingTour { DiscountAmount = null };
        Assert.Null(booking.DiscountAmount);
    }

    [Fact]
    public void ReferralCodeId_IsNullable()
    {
        var booking = new BookingTour { ReferralCodeId = null };
        Assert.Null(booking.ReferralCodeId);
    }
}
