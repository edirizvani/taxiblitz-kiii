using Moq;
using TaxiBlitz.Application.Interfaces;
using TaxiBlitz.Application.Services;
using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Domain.Identity;

namespace TaxiBlitz.Tests.UnitTests.Application.Services;

public class ReferralServiceCommissionTests
{
    private readonly Mock<IReferralRepository> _repo = new();
    private ReferralService Sut() => new(_repo.Object);

    // ── GenerateNewCodeAsync ─────────────────────────────────────────

    [Fact]
    public async Task GenerateNewCodeAsync_DelegatesToRepository()
    {
        var expected = new ReferralCode { Code = "RC1A2B3C", OwnerId = "user1", MaxUses = 1 };
        _repo.Setup(r => r.GenerateNewAsync("user1")).ReturnsAsync(expected);

        var result = await Sut().GenerateNewCodeAsync("user1");

        Assert.Equal(expected, result);
        _repo.Verify(r => r.GenerateNewAsync("user1"), Times.Once);
    }

    // ── GetReceptionistDashboardAsync ────────────────────────────────

    [Fact]
    public async Task GetReceptionistDashboardAsync_NoCodes_ReturnsZeroTotals()
    {
        _repo.Setup(r => r.GetAllByOwnerIdAsync("user1")).ReturnsAsync(new List<ReferralCode>());

        var vm = await Sut().GetReceptionistDashboardAsync("user1");

        Assert.Equal(0m, vm.TotalEarned);
        Assert.Equal(0m, vm.TotalPaid);
        Assert.Equal(0m, vm.TotalPending);
        Assert.Empty(vm.Codes);
    }

    [Fact]
    public async Task GetReceptionistDashboardAsync_UnusedCode_AddsNotUsedRow()
    {
        var code = new ReferralCode { Id = 1, Code = "RC1A2B3C", OwnerId = "user1", IsActive = true, Usages = new List<ReferralUsage>() };
        _repo.Setup(r => r.GetAllByOwnerIdAsync("user1")).ReturnsAsync(new List<ReferralCode> { code });

        var vm = await Sut().GetReceptionistDashboardAsync("user1");

        Assert.Single(vm.Codes);
        Assert.Equal("Not Used", vm.Codes[0].BookingStatus);
        Assert.Null(vm.Codes[0].CommissionAmount);
    }

    [Fact]
    public async Task GetReceptionistDashboardAsync_ApprovedBooking_CountsAsEarned()
    {
        var booking = new BookingTour { Id = 1, Status = "Approved" };
        var usedBy  = new ApplicationUser { Email = "customer@test.com" };
        var usage   = new ReferralUsage { ReferralCodeId = 1, DiscountAmount = 10m, Booking = booking, UsedByUser = usedBy };
        var code    = new ReferralCode { Id = 1, Code = "RC1A2B3C", OwnerId = "owner1", IsActive = true, Usages = new List<ReferralUsage> { usage } };
        _repo.Setup(r => r.GetAllByOwnerIdAsync("owner1")).ReturnsAsync(new List<ReferralCode> { code });

        var vm = await Sut().GetReceptionistDashboardAsync("owner1");

        Assert.Equal(10m, vm.TotalEarned);
        Assert.Equal(10m, vm.TotalPending);
        Assert.Equal(0m,  vm.TotalPaid);
        Assert.Equal(10m, vm.Codes[0].CommissionAmount);
    }

    [Fact]
    public async Task GetReceptionistDashboardAsync_PendingBooking_DoesNotCountAsEarned()
    {
        var booking = new BookingTour { Id = 1, Status = "Pending" };
        var usage   = new ReferralUsage { ReferralCodeId = 1, DiscountAmount = 10m, Booking = booking };
        var code    = new ReferralCode { Id = 1, Code = "RC1A2B3C", OwnerId = "owner1", IsActive = true, Usages = new List<ReferralUsage> { usage } };
        _repo.Setup(r => r.GetAllByOwnerIdAsync("owner1")).ReturnsAsync(new List<ReferralCode> { code });

        var vm = await Sut().GetReceptionistDashboardAsync("owner1");

        Assert.Equal(0m, vm.TotalEarned);
        Assert.Null(vm.Codes[0].CommissionAmount);
    }

    [Fact]
    public async Task GetReceptionistDashboardAsync_CanceledBooking_DoesNotCountAsEarned()
    {
        var booking = new BookingTour { Id = 1, Status = "Canceled" };
        var usage   = new ReferralUsage { ReferralCodeId = 1, DiscountAmount = 10m, Booking = booking };
        var code    = new ReferralCode { Id = 1, Code = "RC1A2B3C", OwnerId = "owner1", IsActive = true, Usages = new List<ReferralUsage> { usage } };
        _repo.Setup(r => r.GetAllByOwnerIdAsync("owner1")).ReturnsAsync(new List<ReferralCode> { code });

        var vm = await Sut().GetReceptionistDashboardAsync("owner1");

        Assert.Equal(0m, vm.TotalEarned);
    }

    [Fact]
    public async Task GetReceptionistDashboardAsync_PaidCommission_SubtractsFromPending()
    {
        var booking = new BookingTour { Id = 1, Status = "Approved" };
        var usage   = new ReferralUsage { ReferralCodeId = 1, DiscountAmount = 20m, Booking = booking, CommissionPaidAt = DateTime.UtcNow };
        var code    = new ReferralCode { Id = 1, Code = "RC1A2B3C", OwnerId = "owner1", IsActive = true, Usages = new List<ReferralUsage> { usage } };
        _repo.Setup(r => r.GetAllByOwnerIdAsync("owner1")).ReturnsAsync(new List<ReferralCode> { code });

        var vm = await Sut().GetReceptionistDashboardAsync("owner1");

        Assert.Equal(20m, vm.TotalEarned);
        Assert.Equal(20m, vm.TotalPaid);
        Assert.Equal(0m,  vm.TotalPending);
        Assert.True(vm.Codes[0].CommissionPaid);
    }

    [Fact]
    public async Task GetReceptionistDashboardAsync_MultipleApprovedCodes_SumsTotals()
    {
        var b1 = new BookingTour { Id = 1, Status = "Approved" };
        var b2 = new BookingTour { Id = 2, Status = "Approved" };
        var u1 = new ReferralUsage { ReferralCodeId = 1, DiscountAmount = 10m, Booking = b1 };
        var u2 = new ReferralUsage { ReferralCodeId = 2, DiscountAmount = 15m, Booking = b2, CommissionPaidAt = DateTime.UtcNow };
        var codes = new List<ReferralCode>
        {
            new() { Id = 1, Code = "RC111111", OwnerId = "owner1", IsActive = true, Usages = new List<ReferralUsage> { u1 } },
            new() { Id = 2, Code = "RC222222", OwnerId = "owner1", IsActive = true, Usages = new List<ReferralUsage> { u2 } }
        };
        _repo.Setup(r => r.GetAllByOwnerIdAsync("owner1")).ReturnsAsync(codes);

        var vm = await Sut().GetReceptionistDashboardAsync("owner1");

        Assert.Equal(25m, vm.TotalEarned);
        Assert.Equal(15m, vm.TotalPaid);
        Assert.Equal(10m, vm.TotalPending);
    }

    // ── PayCommissionAsync ───────────────────────────────────────────

    [Fact]
    public async Task PayCommissionAsync_DelegatesToRepository()
    {
        _repo.Setup(r => r.MarkCommissionPaidAsync("owner1")).ReturnsAsync(3);

        var count = await Sut().PayCommissionAsync("owner1");

        Assert.Equal(3, count);
        _repo.Verify(r => r.MarkCommissionPaidAsync("owner1"), Times.Once);
    }
}
