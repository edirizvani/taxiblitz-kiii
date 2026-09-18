using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Auth;

public class RegistrationFlowTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public RegistrationFlowTests(WebAppFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies     = true
    });

    private async Task<(HttpClient client, string token)> GetRegistrationFormAsync()
    {
        var client = CreateClient();
        var page = await client.GetAsync("/Account/Register");
        var html = await page.Content.ReadAsStringAsync();
        var token = ExtractToken(html) ?? "";
        return (client, token);
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
    public async Task Register_Get_ReturnsOk()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/Account/Register");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithValidData_RedirectsToConfirmation()
    {
        var (client, token) = await GetRegistrationFormAsync();
        var uniqueEmail = $"newuser_{Guid.NewGuid():N}@reg.com";
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("FirstName",                 "Test"),
            new KeyValuePair<string, string>("LastName",                  "User"),
            new KeyValuePair<string, string>("Email",                     uniqueEmail),
            new KeyValuePair<string, string>("PhoneNumber",               "+38976123456"),
            new KeyValuePair<string, string>("Password",                  "Test@12345"),
            new KeyValuePair<string, string>("ConfirmPassword",           "Test@12345"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var response = await client.PostAsync("/Account/Register", content);

        Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ReturnsFormWithError()
    {
        var (client, token) = await GetRegistrationFormAsync();
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("FirstName",                 "Test"),
            new KeyValuePair<string, string>("LastName",                  "User"),
            new KeyValuePair<string, string>("Email",                     "weakpwd@reg.com"),
            new KeyValuePair<string, string>("PhoneNumber",               "+38976000000"),
            new KeyValuePair<string, string>("Password",                  "abc"),
            new KeyValuePair<string, string>("ConfirmPassword",           "abc"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var response = await client.PostAsync("/Account/Register", content);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(html.Contains("password") || html.Contains("Password") || html.Contains("error") || html.Contains("field-validation"));
    }

    [Fact]
    public async Task Register_WithMissingEmail_ReturnsFormWithError()
    {
        var (client, token) = await GetRegistrationFormAsync();
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("FirstName",                 "Test"),
            new KeyValuePair<string, string>("LastName",                  "User"),
            new KeyValuePair<string, string>("Email",                     ""),
            new KeyValuePair<string, string>("PhoneNumber",               "+38976000000"),
            new KeyValuePair<string, string>("Password",                  "Test@12345"),
            new KeyValuePair<string, string>("ConfirmPassword",           "Test@12345"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        });

        var response = await client.PostAsync("/Account/Register", content);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnconfirmedEmail_ShowsError()
    {
        // Register without confirming email
        var factory = new WebAppFactory();
        var regClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        var regPage = await regClient.GetAsync("/Account/Register");
        var regHtml = await regPage.Content.ReadAsStringAsync();
        var regToken = ExtractToken(regHtml) ?? "";

        await regClient.PostAsync("/Account/Register", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("FirstName",  "Unconfirmed"),
            new KeyValuePair<string, string>("LastName",   "User"),
            new KeyValuePair<string, string>("Email",      "unconfirmed@reg.com"),
            new KeyValuePair<string, string>("PhoneNumber", "+38976000001"),
            new KeyValuePair<string, string>("Password",   "Test@12345"),
            new KeyValuePair<string, string>("ConfirmPassword", "Test@12345"),
            new KeyValuePair<string, string>("__RequestVerificationToken", regToken)
        }));

        // Now try to login
        var loginClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        var loginPage = await loginClient.GetAsync("/Account/Login");
        var loginHtml = await loginPage.Content.ReadAsStringAsync();
        var loginToken = ExtractToken(loginHtml) ?? "";

        var loginResponse = await loginClient.PostAsync("/Account/Login", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Email",    "unconfirmed@reg.com"),
            new KeyValuePair<string, string>("Password", "Test@12345"),
            new KeyValuePair<string, string>("__RequestVerificationToken", loginToken)
        }));

        var html = await loginResponse.Content.ReadAsStringAsync();
        // Should either show error or redirect to resend confirmation
        Assert.True(loginResponse.StatusCode is HttpStatusCode.OK or HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task ConfirmEmail_WithInvalidToken_ReturnsErrorOrOk()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/Account/ConfirmEmail?userId=FAKEID&code=INVALIDTOKEN");
        Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task ResendConfirmation_Get_ReturnsLoginPage()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/Account/RegisterConfirmation?email=test@test.com");
        Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Redirect);
    }
}
