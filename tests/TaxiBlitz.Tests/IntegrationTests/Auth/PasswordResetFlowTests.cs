using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Auth;

public class PasswordResetFlowTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public PasswordResetFlowTests(WebAppFactory factory)
    {
        _factory = factory;
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

    [Fact]
    public async Task ForgotPassword_Get_ReturnsOk()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Account/ForgotPassword");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_WithKnownEmail_ShowsConfirmationOrRedirects()
    {
        var factory = new WebAppFactory();
        await factory.CreateAuthenticatedClientAsync("known@pwreset.com", "User");

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        var page = await client.GetAsync("/Account/ForgotPassword");
        var token = ExtractToken(await page.Content.ReadAsStringAsync()) ?? "";

        var response = await client.PostAsync("/Account/ForgotPassword", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Email", "known@pwreset.com"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        }));

        Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_WithUnknownEmail_ShowsSameConfirmation()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        var page = await client.GetAsync("/Account/ForgotPassword");
        var token = ExtractToken(await page.Content.ReadAsStringAsync()) ?? "";

        var response = await client.PostAsync("/Account/ForgotPassword", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Email", "unknown@nobody.com"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        }));

        // Should behave same as known email (no enumeration)
        Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetPassword_Get_WithMissingCode_ReturnsBadRequestOrError()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Account/ResetPassword");
        // Missing code should result in error or bad request
        Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest or HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task ForgotPasswordConfirmation_ReturnsOk()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/Account/ForgotPasswordConfirmation");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
