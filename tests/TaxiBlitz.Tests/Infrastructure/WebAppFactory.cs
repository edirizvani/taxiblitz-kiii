using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TaxiBlitz.Application.Interfaces;
using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Domain.Identity;
using TaxiBlitz.Infrastructure.Receipts;
using TaxiBlitz.Persistence;

namespace TaxiBlitz.Tests.Infrastructure;

public class WebAppFactory : WebApplicationFactory<Program>
{
    public Mock<IAuthEmailService> AuthEmailServiceMock { get; } = new();
    public Mock<IEmailService> EmailServiceMock { get; } = new();

    // Shared root ensures all DbContext instances in this factory see the same InMemory store
    private readonly InMemoryDatabaseRoot _dbRoot = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" environment triggers InMemory db in Program.cs — no SQL Server provider conflict
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Program.cs skips AddDbContext in "Testing" environment.
            // Use a shared InMemoryDatabaseRoot and disable service provider caching
            // so all scopes (seed + request) see the same InMemory store.
            // _dbRoot already ensures all scopes share the same InMemory store.
            // Do NOT call EnableServiceProviderCaching(false) here — it forces EF Core
            // to build a new RuntimeModel per DbContext instance, so the IdentityUserRole<string>
            // RuntimeEntityType differs between contexts and the InMemory store throws:
            // "The property 'UserId' belongs to IdentityUserRole<string> but is being used
            //  with an instance of IdentityUserRole<string>."
            services.AddDbContext<AppDbContext>(opts =>
                opts.UseInMemoryDatabase("WebAppFactory-Tests", _dbRoot));

            // Replace email services with no-op mocks
            services.RemoveAll<IAuthEmailService>();
            services.AddSingleton(AuthEmailServiceMock.Object);

            services.RemoveAll<IEmailService>();
            services.AddSingleton(EmailServiceMock.Object);

            // Replace receipt service with stub
            services.RemoveAll<IReceiptService>();
            services.AddSingleton<IReceiptService>(new StubReceiptService());

            // Bypass antiforgery validation in tests
            services.RemoveAll<IAntiforgery>();
            services.AddSingleton<IAntiforgery>(new NoOpAntiforgery());

            // Use in-memory data protection so no file system keys are needed
            services.AddDataProtection().UseEphemeralDataProtectionProvider();

            // Add a test authentication scheme that reads X-Test-Auth header
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "TestAuth";
                options.DefaultChallengeScheme    = "TestAuth";
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestAuth", opts => { });
        });
    }

    public async Task SeedAsync(Action<AppDbContext> seed)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        seed(db);
        await db.SaveChangesAsync();
    }

    /// <summary>Creates a client authenticated as a user with the given role using the TestAuth header.</summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string role, string password = "Test@123456")
    {
        // Create the user in the DB so the app can look them up
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();

        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole(role));

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName       = email,
                Email          = email,
                EmailConfirmed = true,
                FirstName      = "Test",
                LastName       = "User",
                PhoneNumber    = "+38976000000"
            };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Could not create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        if (!await userManager.IsInRoleAsync(user, role))
            await userManager.AddToRoleAsync(user, role);

        // Use X-Test-Auth header with the actual user ID so TestAuthHandler can set correct NameIdentifier
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        client.DefaultRequestHeaders.Add("X-Test-Auth", $"{email}|{role}|{user.Id}");
        return client;
    }

    private static string? ExtractAntiForgeryToken(string html)
    {
        // Try both attribute orders that Razor may generate
        foreach (var marker in new[]
        {
            "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"",
            "__RequestVerificationToken\" value=\""
        })
        {
            var idx = html.IndexOf(marker, StringComparison.Ordinal);
            if (idx >= 0)
            {
                var start = idx + marker.Length;
                var end = html.IndexOf('"', start);
                if (end > start) return html[start..end];
            }
        }

        // Regex-style: find <input ... name="__RequestVerificationToken" ... value="TOKEN"
        var nameIdx = html.IndexOf("__RequestVerificationToken", StringComparison.Ordinal);
        if (nameIdx < 0) return null;
        var valueMarker = "value=\"";
        var valueIdx = html.IndexOf(valueMarker, nameIdx, StringComparison.Ordinal);
        if (valueIdx < 0)
        {
            // value might come before name in attribute order
            var inputStart = html.LastIndexOf("<input", nameIdx, StringComparison.Ordinal);
            if (inputStart >= 0)
            {
                valueIdx = html.IndexOf(valueMarker, inputStart, StringComparison.Ordinal);
            }
        }
        if (valueIdx < 0) return null;
        var vStart = valueIdx + valueMarker.Length;
        var vEnd = html.IndexOf('"', vStart);
        return vEnd > vStart ? html[vStart..vEnd] : null;
    }

    private sealed class StubReceiptService : IReceiptService
    {
        public byte[] GenerateBookingReceipt(BookingTour booking) => new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
    }
}

/// <summary>Subclass with rate limiting ENABLED (for RateLimitingTests).</summary>
public class RateLimitWebAppFactory : WebApplicationFactory<Program>
{
    private readonly InMemoryDatabaseRoot _dbRoot = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.AddDbContext<AppDbContext>(opts =>
                opts.UseInMemoryDatabase("RateLimit-Tests", _dbRoot));

            services.RemoveAll<IAuthEmailService>();
            services.AddSingleton(new Mock<IAuthEmailService>().Object);
            services.RemoveAll<IEmailService>();
            services.AddSingleton(new Mock<IEmailService>().Object);
            services.RemoveAll<IReceiptService>();
            services.AddSingleton<IReceiptService>(new StubReceipt());
            services.RemoveAll<IAntiforgery>();
            services.AddSingleton<IAntiforgery>(new NoOpAntiforgery());
        });
    }

    private sealed class StubReceipt : IReceiptService
    {
        public byte[] GenerateBookingReceipt(BookingTour booking) => Array.Empty<byte>();
    }
}

/// <summary>
/// Test auth handler that authenticates requests based on X-Test-Auth header.
/// Header format: "email|role"
/// For requests without this header, it returns NoResult so cookie auth falls through.
/// For challenge (unauthorized access), redirects to /Account/Login.
/// </summary>
internal sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-Auth", out var headerValue))
            return Task.FromResult(AuthenticateResult.NoResult());

        var parts = headerValue.ToString().Split('|');
        if (parts.Length < 2)
            return Task.FromResult(AuthenticateResult.Fail("Invalid X-Test-Auth header format"));

        var email  = parts[0];
        var role   = parts[1];
        var userId = parts.Length >= 3 ? parts[2] : email; // actual GUID if provided

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name,           email),
            new Claim(ClaimTypes.Email,          email),
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role,           role)
        };

        var identity  = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var ticket    = new AuthenticationTicket(principal, "TestAuth");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 302;
        Response.Headers.Location = "/Account/Login?ReturnUrl=" + Uri.EscapeDataString(Request.Path);
        return Task.CompletedTask;
    }
}

/// <summary>No-op antiforgery implementation that accepts all tokens (for integration tests).</summary>
internal sealed class NoOpAntiforgery : IAntiforgery
{
    public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext)
        => new AntiforgeryTokenSet("test-token", "test-cookie", "test-field", "test-header");

    public AntiforgeryTokenSet GetTokens(HttpContext httpContext)
        => new AntiforgeryTokenSet("test-token", "test-cookie", "test-field", "test-header");

    public Task<bool> IsRequestValidAsync(HttpContext httpContext) => Task.FromResult(true);

    public void SetCookieTokenAndHeader(HttpContext httpContext) { }

    public Task ValidateRequestAsync(HttpContext httpContext) => Task.CompletedTask;
}
