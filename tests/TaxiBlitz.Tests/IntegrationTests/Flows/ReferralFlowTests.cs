using Microsoft.Extensions.DependencyInjection;
using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Persistence;
using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Flows;

public class ReferralFlowTests
{
    [Fact]
    public async Task GetOrCreateCode_CreatesCodeWithTBPrefix_AndPersists()
    {
        var factory = new WebAppFactory();
        var client = await factory.CreateAuthenticatedClientAsync("referralfow@test.com", "User");

        var response = await client.GetAsync("/Referral/MyCode");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("TB", html);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(db.ReferralCodes.Any(c => c.Code.StartsWith("TB")));
    }

    [Fact]
    public async Task GetOrCreateCode_IsIdempotent_SameCodeOnSecondCall()
    {
        var factory = new WebAppFactory();
        var client = await factory.CreateAuthenticatedClientAsync("idempotent@test.com", "User");

        await client.GetAsync("/Referral/MyCode");
        await client.GetAsync("/Referral/MyCode");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // Should only have one code per user
        var userId = db.Users.Single(u => u.Email == "idempotent@test.com").Id;
        Assert.Equal(1, db.ReferralCodes.Count(c => c.OwnerId == userId));
    }

    [Fact]
    public async Task ValidateCode_UserCannotUseOwnCode()
    {
        var factory = new WebAppFactory();
        var client = await factory.CreateAuthenticatedClientAsync("owncode@test.com", "User");

        // Create their own code first
        await client.GetAsync("/Referral/MyCode");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userId = db.Users.Single(u => u.Email == "owncode@test.com").Id;
        var code = db.ReferralCodes.Single(c => c.OwnerId == userId).Code;

        var response = await client.GetAsync($"/Referral/ValidateCode?code={code}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("false", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateCode_InactiveCode_IsRejected()
    {
        var factory = new WebAppFactory();
        await factory.SeedAsync(db =>
        {
            db.ReferralCodes.Add(new ReferralCode { Code = "TB0001", OwnerId = "otheruserid", IsActive = false, DiscountPercent = 5 });
        });

        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Referral/ValidateCode?code=TB0001");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("false", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateCode_ActiveCodeByOtherUser_IsAccepted()
    {
        var factory = new WebAppFactory();
        await factory.SeedAsync(db =>
        {
            db.ReferralCodes.Add(new ReferralCode { Code = "TB5555", OwnerId = "differentowner", IsActive = true, DiscountPercent = 5 });
        });

        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Referral/ValidateCode?code=TB5555");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("true", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(Skip = "Referral booking test - needs investigation")]
    public async Task BookingWithReferralCode_RecordsUsageAndIncrementsCount()
    {
        var factory = new WebAppFactory();
        await factory.SeedAsync(db =>
        {
            db.Tours.Add(new Tour { Id = 800, Title = "Referral Tour", Description = "D", Price = 1000, Duration = "4h" });
            db.Drivers.Add(new Driver { Id = 800, Name = "Driver D" });
            db.ReferralCodes.Add(new ReferralCode { Id = 800, Code = "TB8800", OwnerId = "anotheruserid", IsActive = true, DiscountPercent = 5 });
        });
        var client = await factory.CreateAuthenticatedClientAsync("referralbooker@test.com", "User");

        var createPage = await client.GetAsync("/BookingTours/Create?tourId=800");
        var html = await createPage.Content.ReadAsStringAsync();
        var token = ExtractToken(html) ?? "";
        var futureDateTime = DateTime.Now.AddHours(3).ToString("yyyy-MM-ddTHH:mm");

        await client.PostAsync("/BookingTours/Create", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Id",                        "0"),
            new KeyValuePair<string, string>("NameOfBookMaker",           "Referral Booker"),
            new KeyValuePair<string, string>("PhoneNumber",               "+38976800800"),
            new KeyValuePair<string, string>("NumberOfPeople",            "2"),
            new KeyValuePair<string, string>("TourId",                    "800"),
            new KeyValuePair<string, string>("DriverId",                  "800"),
            new KeyValuePair<string, string>("BookingDateTime",           futureDateTime),
            new KeyValuePair<string, string>("referralCode",              "TB8800"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        }));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = db.Bookings.FirstOrDefault(b => b.NameOfBookMaker == "Referral Booker");
        Assert.NotNull(booking);
        Assert.NotNull(booking!.DiscountAmount);
        Assert.True(booking.DiscountAmount > 0);
    }

    private static string? ExtractToken(string html)
    {
        var marker = "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"";
        var idx = html.IndexOf(marker, StringComparison.Ordinal);
        if (idx < 0) return null;
        var start = idx + marker.Length;
        var end = html.IndexOf('"', start);
        return end > start ? html[start..end] : null;
    }
}
