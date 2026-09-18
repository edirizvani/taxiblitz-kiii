using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Flows;

public class ProfileCompletionGateTests
{
    [Fact]
    public async Task AlreadyCompleteUser_CanAccessTours()
    {
        // CreateAuthenticatedClientAsync sets FirstName="Test", LastName="User", PhoneNumber="+38976000000"
        // so the profile is complete — should not be redirected
        var factory = new WebAppFactory();
        var client = await factory.CreateAuthenticatedClientAsync("completeuser@gate.com", "User");

        var response = await client.GetAsync("/Tours");

        var location = response.Headers.Location?.ToString() ?? "";
        Assert.DoesNotContain("CompleteProfile", location);
    }

    [Fact]
    public async Task CompleteProfile_Page_AlwaysAccessible_ForAuthenticatedUser()
    {
        var factory = new WebAppFactory();
        var client = await factory.CreateAuthenticatedClientAsync("gateuser@gate.com", "User");

        var response = await client.GetAsync("/Manage/CompleteProfile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AccountController_AlwaysBypassed()
    {
        var factory = new WebAppFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Account/Login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_AccessingProtectedPage_RedirectsToLoginNotCompleteProfile()
    {
        var factory = new WebAppFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Tours/Create");

        Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found);
        var location = response.Headers.Location?.ToString() ?? "";
        Assert.DoesNotContain("CompleteProfile", location);
    }

    [Fact]
    public async Task AjaxRequest_NotRedirectedToCompleteProfile()
    {
        var factory = new WebAppFactory();
        var client = await factory.CreateAuthenticatedClientAsync("ajaxgate@gate.com", "User");
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

        var response = await client.GetAsync("/Tours");

        var location = response.Headers.Location?.ToString() ?? "";
        Assert.DoesNotContain("CompleteProfile", location);
    }

    [Fact]
    public async Task HomeError_AlwaysBypassed()
    {
        var factory = new WebAppFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Home/Error");

        var location = response.Headers.Location?.ToString() ?? "";
        Assert.DoesNotContain("CompleteProfile", location);
    }

    [Fact]
    public async Task EditProfile_AlwaysBypassed_ForAuthenticatedUser()
    {
        var factory = new WebAppFactory();
        var client = await factory.CreateAuthenticatedClientAsync("editgate@gate.com", "User");

        var response = await client.GetAsync("/Manage/EditProfile");

        var location = response.Headers.Location?.ToString() ?? "";
        Assert.DoesNotContain("CompleteProfile", location);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
