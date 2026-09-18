using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TaxiBlitz.Infrastructure.Email;

namespace TaxiBlitz.Tests.Services;

/// <summary>
/// Tests that AuthEmailService handles missing / empty SMTP config gracefully
/// without throwing exceptions. No actual email sending occurs.
/// </summary>
public class AuthEmailServiceTests
{
    private static AuthEmailService BuildService(Dictionary<string, string?> config)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build();
        return new AuthEmailService(configuration);
    }

    [Fact]
    public async Task SendEmailConfirmationAsync_MissingSmtpCredentials_DoesNotThrow()
    {
        var svc = BuildService(new Dictionary<string, string?>
        {
            ["SmtpHost"] = "smtp.gmail.com",
            ["SmtpPort"] = "587",
            ["SmtpUser"] = "",       // empty — should short-circuit gracefully
            ["SmtpPass"] = "",
            ["SmtpFrom"] = ""
        });

        // Should complete without throwing
        await svc.SendEmailConfirmationAsync(
            "recipient@test.com",
            "Test User",
            "https://example.com/confirm?token=abc");
    }

    [Fact]
    public async Task SendPasswordResetAsync_MissingSmtpCredentials_DoesNotThrow()
    {
        var svc = BuildService(new Dictionary<string, string?>
        {
            ["SmtpUser"] = null,
            ["SmtpPass"] = null
        });

        await svc.SendPasswordResetAsync(
            "recipient@test.com",
            "Test User",
            "https://example.com/reset?token=abc");
    }

    [Fact]
    public async Task SendEmailConfirmationAsync_NullConfig_DoesNotThrow()
    {
        // Completely empty config — all keys missing
        var svc = BuildService(new Dictionary<string, string?>());

        await svc.SendEmailConfirmationAsync(
            "recipient@test.com", "User", "https://example.com/confirm");
    }

    [Fact]
    public async Task SendPasswordResetAsync_NullConfig_DoesNotThrow()
    {
        var svc = BuildService(new Dictionary<string, string?>());

        await svc.SendPasswordResetAsync(
            "recipient@test.com", "User", "https://example.com/reset");
    }
}
