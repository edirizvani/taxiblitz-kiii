using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Controllers;

public class ProfileCompleteFilterTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public ProfileCompleteFilterTests(WebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Filter_UnauthenticatedUser_DoesNotRedirectToCompleteProfile()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Tours");
        // Anonymous users should redirect to Login, not to CompleteProfile
        var location = response.Headers.Location?.ToString() ?? "";
        Assert.DoesNotContain("CompleteProfile", location);
    }

    [Fact]
    public async Task Filter_AccountController_AlwaysBypassedForAnonymous()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Account/Login");
        // Should render Login, not redirect to CompleteProfile
        var location = response.Headers.Location?.ToString() ?? "";
        Assert.DoesNotContain("CompleteProfile", location);
    }

    [Fact]
    public async Task Filter_ManageCompleteProfile_NotRedirectedToItself()
    {
        var factory = new WebAppFactory();
        var client = await factory.CreateAuthenticatedClientAsync("incomplete2@test.com", "User");
        var response = await client.GetAsync("/Manage/CompleteProfile");
        // Should return 200, not a redirect loop
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Filter_UserWithCompleteProfile_CanAccessTours()
    {
        var factory = new WebAppFactory();
        // CreateAuthenticatedClientAsync creates user with FirstName, LastName, PhoneNumber set
        var client = await factory.CreateAuthenticatedClientAsync("complete@test.com", "User");
        var response = await client.GetAsync("/Tours");
        // Complete profile user should NOT be redirected to CompleteProfile
        var location = response.Headers.Location?.ToString() ?? "";
        Assert.DoesNotContain("CompleteProfile", location);
    }

    [Fact]
    public async Task Filter_HomeError_AlwaysBypassed()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Home/Error");
        // Error page should not redirect to CompleteProfile
        var location = response.Headers.Location?.ToString() ?? "";
        Assert.DoesNotContain("CompleteProfile", location);
    }

    [Fact]
    public async Task Filter_AjaxRequests_AlwaysBypassed()
    {
        var factory = new WebAppFactory();
        var client = await factory.CreateAuthenticatedClientAsync("ajaxuser@test.com", "User");
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

        var response = await client.GetAsync("/Tours");
        var location = response.Headers.Location?.ToString() ?? "";
        Assert.DoesNotContain("CompleteProfile", location);
    }
}
