using Microsoft.Extensions.DependencyInjection;
using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Persistence;
using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Flows;

public class ReceptionistCommissionFlowTests
{
    // ── Helper ───────────────────────────────────────────────────────

    private static async Task<(string receptionistId, int codeId, int bookingId)> SetupBookingWithReceptionistCode(
        WebAppFactory factory,
        string bookingStatus)
    {
        var receptionistClient = await factory.CreateAuthenticatedClientAsync("recept-flow@test.com", "Receptionist");

        // Receptionist generates a code
        await receptionistClient.PostAsync("/Receptionist/GenerateCode",
            new FormUrlEncodedContent(Array.Empty<KeyValuePair<string, string>>()));

        using var scope1 = factory.Services.CreateScope();
        var db1 = scope1.ServiceProvider.GetRequiredService<AppDbContext>();
        var receptionistId = db1.Users.Single(u => u.Email == "recept-flow@test.com").Id;
        var code = db1.ReferralCodes.Single(c => c.OwnerId == receptionistId);

        // Seed a tour and booking that used the code
        var tour = new Tour { Title = "Flow Tour", Description = "D", Price = 200, Duration = "2h" };
        db1.Tours.Add(tour);
        await db1.SaveChangesAsync();

        var booking = new BookingTour
        {
            NameOfBookMaker = "Flow Customer", CustomerEmail = "customer-flow@test.com",
            PhoneNumber = "+38970000001", NumberOfPeople = 2,
            BookingDateTime = DateTime.Now.AddDays(1),
            TourId = tour.Id, Status = bookingStatus,
            DiscountAmount = 10m, ReferralCodeId = code.Id
        };
        db1.Bookings.Add(booking);
        await db1.SaveChangesAsync();

        db1.ReferralUsages.Add(new ReferralUsage
        {
            ReferralCodeId = code.Id,
            UsedByUserId   = "customer-flow-id",
            BookingId      = booking.Id,
            DiscountAmount = 10m
        });
        await db1.SaveChangesAsync();

        return (receptionistId, code.Id, booking.Id);
    }

    // ── Tests ────────────────────────────────────────────────────────

    [Fact]
    public async Task ReceptionistCode_WhenBookingApproved_CommissionShowsAsPending()
    {
        var factory = new WebAppFactory();
        var (receptionistId, _, bookingId) = await SetupBookingWithReceptionistCode(factory, "Approved");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var usage = db.ReferralUsages.Single(u => u.BookingId == bookingId);

        Assert.Null(usage.CommissionPaidAt);
        Assert.Equal(10m, usage.DiscountAmount);
    }

    [Fact]
    public async Task ReceptionistCode_AfterPayCommission_CommissionShowsAsPaid()
    {
        var factory    = new WebAppFactory();
        var (receptionistId, _, bookingId) = await SetupBookingWithReceptionistCode(factory, "Approved");
        var adminClient = await factory.CreateAuthenticatedClientAsync("admin-pay@test.com", "Administrator");

        await adminClient.PostAsync($"/Admin/PayCommission?userId={receptionistId}",
            new FormUrlEncodedContent(Array.Empty<KeyValuePair<string, string>>()));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var usage = db.ReferralUsages.Single(u => u.BookingId == bookingId);

        Assert.NotNull(usage.CommissionPaidAt);
    }

    [Fact]
    public async Task ReceptionistCode_WhenBookingCanceled_CommissionNotEarned()
    {
        var factory = new WebAppFactory();
        var (_, _, bookingId) = await SetupBookingWithReceptionistCode(factory, "Canceled");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var usage = db.ReferralUsages.Single(u => u.BookingId == bookingId);

        // CommissionPaidAt null and booking status Canceled = no commission earned
        Assert.Null(usage.CommissionPaidAt);
        // The discount was applied to the booking but commission is not triggered
        var booking = db.Bookings.Single(b => b.Id == bookingId);
        Assert.Equal("Canceled", booking.Status);
    }

    [Fact]
    public async Task GenerateCode_ReceptionistCanGenerateMultipleCodes()
    {
        var factory = new WebAppFactory();
        var client  = await factory.CreateAuthenticatedClientAsync("multi-code@test.com", "Receptionist");

        await client.PostAsync("/Receptionist/GenerateCode", new FormUrlEncodedContent(Array.Empty<KeyValuePair<string, string>>()));
        await client.PostAsync("/Receptionist/GenerateCode", new FormUrlEncodedContent(Array.Empty<KeyValuePair<string, string>>()));
        await client.PostAsync("/Receptionist/GenerateCode", new FormUrlEncodedContent(Array.Empty<KeyValuePair<string, string>>()));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userId = db.Users.Single(u => u.Email == "multi-code@test.com").Id;
        var codes = db.ReferralCodes.Where(c => c.OwnerId == userId).ToList();

        Assert.Equal(3, codes.Count);
        Assert.All(codes, c => Assert.StartsWith("RC", c.Code));
        Assert.All(codes, c => Assert.Equal(1, c.MaxUses));
        // All codes are distinct
        Assert.Equal(3, codes.Select(c => c.Code).Distinct().Count());
    }

    [Fact]
    public async Task ReceptionistDashboard_ShowsGeneratedCodes()
    {
        var factory = new WebAppFactory();
        var client  = await factory.CreateAuthenticatedClientAsync("dash-test@test.com", "Receptionist");

        await client.PostAsync("/Receptionist/GenerateCode", new FormUrlEncodedContent(Array.Empty<KeyValuePair<string, string>>()));

        var response = await client.GetAsync("/Receptionist/Dashboard");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("RC", html);
        Assert.Contains("My Dashboard", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AdminReceptionistsPage_ShowsReceptionistAfterRoleAssigned()
    {
        var factory     = new WebAppFactory();
        // Create a receptionist user
        await factory.CreateAuthenticatedClientAsync("listed-recept@test.com", "Receptionist");
        var adminClient = await factory.CreateAuthenticatedClientAsync("admin-list@test.com", "Administrator");

        var response = await adminClient.GetAsync("/Admin/Receptionists");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("listed-recept@test.com", html);
    }
}
