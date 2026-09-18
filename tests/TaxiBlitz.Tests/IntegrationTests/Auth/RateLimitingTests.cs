using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using TaxiBlitz.Tests.Infrastructure;

namespace TaxiBlitz.Tests.IntegrationTests.Auth;

public class RateLimitingTests : IClassFixture<RateLimitWebAppFactory>
{
    private readonly RateLimitWebAppFactory _factory;

    public RateLimitingTests(RateLimitWebAppFactory factory)
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
    public async Task LoginEndpoint_After10Requests_Returns429()
    {
        // Use own factory so this test is independent of shared fixture state
        using var factory = new RateLimitWebAppFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });

        HttpStatusCode? lastStatus = null;
        for (int i = 0; i < 11; i++)
        {
            var page = await client.GetAsync("/Account/Login");
            var token = ExtractToken(await page.Content.ReadAsStringAsync()) ?? "";
            var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Email", $"test{i}@rate.com"),
                new KeyValuePair<string, string>("Password", "Test@12345"),
                new KeyValuePair<string, string>("__RequestVerificationToken", token)
            }));
            lastStatus = response.StatusCode;
            if (response.StatusCode == HttpStatusCode.TooManyRequests) break;
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, lastStatus);
    }

    [Fact]
    public async Task RegisterEndpoint_After5Requests_Returns429()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });

        HttpStatusCode? lastStatus = null;
        for (int i = 0; i < 6; i++)
        {
            var page = await client.GetAsync("/Account/Register");
            var token = ExtractToken(await page.Content.ReadAsStringAsync()) ?? "";
            var response = await client.PostAsync("/Account/Register", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("FirstName",  "Test"),
                new KeyValuePair<string, string>("LastName",   "User"),
                new KeyValuePair<string, string>("Email",      $"reg{i}@rate.com"),
                new KeyValuePair<string, string>("PhoneNumber", "+38976000000"),
                new KeyValuePair<string, string>("Password",   "Test@12345"),
                new KeyValuePair<string, string>("ConfirmPassword", "Test@12345"),
                new KeyValuePair<string, string>("__RequestVerificationToken", token)
            }));
            lastStatus = response.StatusCode;
            if (response.StatusCode == HttpStatusCode.TooManyRequests) break;
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, lastStatus);
    }

    [Fact]
    public async Task ForgotPasswordEndpoint_After5Requests_Returns429()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });

        HttpStatusCode? lastStatus = null;
        for (int i = 0; i < 6; i++)
        {
            var page = await client.GetAsync("/Account/ForgotPassword");
            var token = ExtractToken(await page.Content.ReadAsStringAsync()) ?? "";
            var response = await client.PostAsync("/Account/ForgotPassword", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Email", $"forgot{i}@rate.com"),
                new KeyValuePair<string, string>("__RequestVerificationToken", token)
            }));
            lastStatus = response.StatusCode;
            if (response.StatusCode == HttpStatusCode.TooManyRequests) break;
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, lastStatus);
    }

    [Fact]
    public async Task LoginEndpoint_WithinLimit_DoesNotReturn429()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });

        for (int i = 0; i < 5; i++)
        {
            var page = await client.GetAsync("/Account/Login");
            var token = ExtractToken(await page.Content.ReadAsStringAsync()) ?? "";
            var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Email", $"within{i}@rate.com"),
                new KeyValuePair<string, string>("Password", "Test@12345"),
                new KeyValuePair<string, string>("__RequestVerificationToken", token)
            }));
            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }
    }
}
