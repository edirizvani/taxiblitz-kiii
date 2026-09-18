using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TaxiBlitz.Persistence;
using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Controllers;

public class AdminReceptionistsControllerTests
{
    [Fact]
    public async Task Receptionists_Anonymous_RedirectsToLogin()
    {
        var factory = new WebAppFactory();
        var client  = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Admin/Receptionists");

        Assert.True(response.StatusCode is HttpStatusCode.Found or HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Receptionists_AdminRole_ReturnsOk()
    {
        var factory = new WebAppFactory();
        var client  = await factory.CreateAuthenticatedClientAsync("admin@test.com", "Administrator");

        var response = await client.GetAsync("/Admin/Receptionists");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Receptionists_ReceptionistRole_ReturnsForbiddenOrRedirect()
    {
        var factory = new WebAppFactory();
        var client  = await factory.CreateAuthenticatedClientAsync("r@test.com", "Receptionist");

        var response = await client.GetAsync("/Admin/Receptionists");

        Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminCodes_AdminRole_ReturnsOk()
    {
        var factory = new WebAppFactory();
        var client  = await factory.CreateAuthenticatedClientAsync("admin2@test.com", "Administrator");

        var response = await client.GetAsync("/Admin/AdminCodes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GenerateCode_AdminPost_CreatesNewCodeInDb()
    {
        var factory = new WebAppFactory();
        var client  = await factory.CreateAuthenticatedClientAsync("admin3@test.com", "Administrator");

        await client.PostAsync("/Admin/GenerateCode",
            new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("", "") }));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userId = db.Users.Single(u => u.Email == "admin3@test.com").Id;
        Assert.True(db.ReferralCodes.Any(c => c.OwnerId == userId && c.Code.StartsWith("RC")));
    }

    [Fact]
    public async Task ReceptionistDetail_AdminRole_ReturnsOkOrNotFound()
    {
        var factory = new WebAppFactory();
        var client  = await factory.CreateAuthenticatedClientAsync("admin4@test.com", "Administrator");

        var response = await client.GetAsync("/Admin/ReceptionistDetail?userId=nonexistent-id");

        // Either NotFound (user doesn't exist) or OK — not a 500
        Assert.True(response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.OK or HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PayCommission_AdminPost_RedirectsToReceptionists()
    {
        var factory        = new WebAppFactory();
        var receptionistClient = await factory.CreateAuthenticatedClientAsync("recept@test.com", "Receptionist");
        var adminClient    = await factory.CreateAuthenticatedClientAsync("admin5@test.com", "Administrator");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var receptionistId = db.Users.Single(u => u.Email == "recept@test.com").Id;

        var response = await adminClient.PostAsync($"/Admin/PayCommission?userId={receptionistId}",
            new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("", "") }));

        Assert.True(response.StatusCode is HttpStatusCode.Found or HttpStatusCode.Redirect);
    }
}
