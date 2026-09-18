using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Persistence;
using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Controllers;

public class ReceptionistControllerTests
{
    [Fact]
    public async Task Dashboard_Anonymous_RedirectsToLogin()
    {
        var factory = new WebAppFactory();
        var client  = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Receptionist/Dashboard");

        Assert.True(response.StatusCode is HttpStatusCode.Found or HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Dashboard_ReceptionistRole_ReturnsOk()
    {
        var factory = new WebAppFactory();
        var client  = await factory.CreateAuthenticatedClientAsync("receptionist@test.com", "Receptionist");

        var response = await client.GetAsync("/Receptionist/Dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_UserRole_ReturnsForbiddenOrRedirect()
    {
        var factory = new WebAppFactory();
        var client  = await factory.CreateAuthenticatedClientAsync("regular@test.com", "User");

        var response = await client.GetAsync("/Receptionist/Dashboard");

        // Non-receptionist gets challenge (redirect to login or 403)
        Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Dashboard_AdministratorRole_ReturnsOk()
    {
        var factory = new WebAppFactory();
        var client  = await factory.CreateAuthenticatedClientAsync("admin@test.com", "Administrator");

        var response = await client.GetAsync("/Receptionist/Dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GenerateCode_Post_ReceptionistRole_RedirectsToDashboard()
    {
        var factory = new WebAppFactory();
        var client  = await factory.CreateAuthenticatedClientAsync("recept2@test.com", "Receptionist");

        var response = await client.PostAsync("/Receptionist/GenerateCode",
            new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("", "") }));

        Assert.True(response.StatusCode is HttpStatusCode.Found or HttpStatusCode.Redirect);
        Assert.Contains("/Receptionist/Dashboard", response.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateCode_Post_Anonymous_RedirectsToLogin()
    {
        var factory = new WebAppFactory();
        var client  = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsync("/Receptionist/GenerateCode",
            new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("", "") }));

        Assert.True(response.StatusCode is HttpStatusCode.Found or HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task GenerateCode_Post_CreatesNewCodeInDb()
    {
        var factory = new WebAppFactory();
        var client  = await factory.CreateAuthenticatedClientAsync("codegen@test.com", "Receptionist");

        await client.PostAsync("/Receptionist/GenerateCode",
            new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("", "") }));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userId = db.Users.Single(u => u.Email == "codegen@test.com").Id;
        Assert.True(db.ReferralCodes.Any(c => c.OwnerId == userId && c.Code.StartsWith("RC")));
    }
}
