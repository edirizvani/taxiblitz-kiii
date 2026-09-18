using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using TaxiBlitz.Application.Interfaces;

namespace TaxiBlitz.Infrastructure.Email;

public class AuthEmailService : IAuthEmailService
{
    private readonly IConfiguration _config;

    public AuthEmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailConfirmationAsync(string toEmail, string userName, string confirmationLink)
    {
        string subject = "Confirm your TaxiBlitz Ohrid account";
        string body = $@"<!DOCTYPE html>
<html>
<body style=""font-family:Arial,sans-serif;background:#f4f4f4;margin:0;padding:0;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
    <tr><td align=""center"" style=""padding:40px 0;"">
      <table width=""560"" cellpadding=""0"" cellspacing=""0"" style=""background:#fff;border-radius:8px;overflow:hidden;"">
        <tr><td style=""background:#1a1a2e;padding:28px 32px;"">
          <h1 style=""color:#fff;margin:0;font-size:22px;"">TaxiBlitz Ohrid</h1>
        </td></tr>
        <tr><td style=""padding:32px;"">
          <h2 style=""color:#1a1a2e;margin-top:0;"">Confirm your email address</h2>
          <p style=""color:#555;line-height:1.6;"">Hi {userName},</p>
          <p style=""color:#555;line-height:1.6;"">Thanks for signing up! Click the button below to confirm your email and activate your account.</p>
          <p style=""text-align:center;margin:32px 0;"">
            <a href=""{confirmationLink}"" style=""background:#e63946;color:#fff;padding:14px 32px;border-radius:6px;text-decoration:none;font-weight:bold;font-size:15px;"">Confirm Email</a>
          </p>
          <p style=""color:#888;font-size:13px;"">If the button doesn't work, copy and paste this link into your browser:</p>
          <p style=""color:#888;font-size:12px;word-break:break-all;"">{confirmationLink}</p>
          <p style=""color:#888;font-size:13px;"">If you didn't create an account, you can safely ignore this email.</p>
        </td></tr>
        <tr><td style=""background:#f8f8f8;padding:20px 32px;text-align:center;"">
          <p style=""color:#aaa;font-size:12px;margin:0;"">TaxiBlitz Ohrid &mdash; Explore Ohrid with us</p>
        </td></tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";

        await SendAsync(toEmail, subject, body);
    }

    public async Task SendPasswordResetAsync(string toEmail, string userName, string resetLink)
    {
        string subject = "Reset your TaxiBlitz Ohrid password";
        string body = $@"<!DOCTYPE html>
<html>
<body style=""font-family:Arial,sans-serif;background:#f4f4f4;margin:0;padding:0;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
    <tr><td align=""center"" style=""padding:40px 0;"">
      <table width=""560"" cellpadding=""0"" cellspacing=""0"" style=""background:#fff;border-radius:8px;overflow:hidden;"">
        <tr><td style=""background:#1a1a2e;padding:28px 32px;"">
          <h1 style=""color:#fff;margin:0;font-size:22px;"">TaxiBlitz Ohrid</h1>
        </td></tr>
        <tr><td style=""padding:32px;"">
          <h2 style=""color:#1a1a2e;margin-top:0;"">Reset your password</h2>
          <p style=""color:#555;line-height:1.6;"">Hi {userName},</p>
          <p style=""color:#555;line-height:1.6;"">We received a request to reset the password for your account. Click the button below to set a new password.</p>
          <p style=""text-align:center;margin:32px 0;"">
            <a href=""{resetLink}"" style=""background:#e63946;color:#fff;padding:14px 32px;border-radius:6px;text-decoration:none;font-weight:bold;font-size:15px;"">Reset Password</a>
          </p>
          <p style=""color:#888;font-size:13px;"">If the button doesn't work, copy and paste this link into your browser:</p>
          <p style=""color:#888;font-size:12px;word-break:break-all;"">{resetLink}</p>
          <p style=""color:#888;font-size:13px;""><strong>This link expires in 1 hour.</strong></p>
          <p style=""color:#888;font-size:13px;"">If you didn't request a password reset, you can safely ignore this email. Your password will not change.</p>
        </td></tr>
        <tr><td style=""background:#f8f8f8;padding:20px 32px;text-align:center;"">
          <p style=""color:#aaa;font-size:12px;margin:0;"">TaxiBlitz Ohrid &mdash; Explore Ohrid with us</p>
        </td></tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";

        await SendAsync(toEmail, subject, body);
    }

    public async Task SendAlreadyRegisteredAsync(string toEmail, string userName, string loginUrl)
    {
        string subject = "Someone tried to register with your TaxiBlitz Ohrid account";
        string body = $@"<!DOCTYPE html>
<html>
<body style=""font-family:Arial,sans-serif;background:#f4f4f4;margin:0;padding:0;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
    <tr><td align=""center"" style=""padding:40px 0;"">
      <table width=""560"" cellpadding=""0"" cellspacing=""0"" style=""background:#fff;border-radius:8px;overflow:hidden;"">
        <tr><td style=""background:#1a1a2e;padding:28px 32px;"">
          <h1 style=""color:#fff;margin:0;font-size:22px;"">TaxiBlitz Ohrid</h1>
        </td></tr>
        <tr><td style=""padding:32px;"">
          <h2 style=""color:#1a1a2e;margin-top:0;"">Someone tried to register with your email</h2>
          <p style=""color:#555;line-height:1.6;"">Hi {userName},</p>
          <p style=""color:#555;line-height:1.6;"">Someone attempted to create a TaxiBlitz Ohrid account using your email address. Since you already have an account, no new account was created.</p>
          <p style=""color:#555;line-height:1.6;"">If this was you, you can sign in directly:</p>
          <p style=""text-align:center;margin:32px 0;"">
            <a href=""{loginUrl}"" style=""background:#e63946;color:#fff;padding:14px 32px;border-radius:6px;text-decoration:none;font-weight:bold;font-size:15px;"">Sign in to your account</a>
          </p>
          <p style=""color:#888;font-size:13px;"">If you did not attempt to register, you can safely ignore this email. Your account remains secure.</p>
        </td></tr>
        <tr><td style=""background:#f8f8f8;padding:20px 32px;text-align:center;"">
          <p style=""color:#aaa;font-size:12px;margin:0;"">TaxiBlitz Ohrid &mdash; Explore Ohrid with us</p>
        </td></tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";

        await SendAsync(toEmail, subject, body);
    }

    private async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        string host = _config["SmtpHost"];
        string portStr = _config["SmtpPort"] ?? "587";
        string user = _config["SmtpUser"];
        string pass = _config["SmtpPass"];
        string from = _config["SmtpFrom"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
        {
            Console.Error.WriteLine($"[AuthEmailService] SMTP not configured — skipping email to {toEmail} ({subject})");
            return;
        }

        try
        {
            int port = int.TryParse(portStr, out int p) ? p : 587;
            var message = new MailMessage
            {
                From       = new MailAddress(from ?? user, "TaxiBlitz Ohrid"),
                Subject    = subject,
                Body       = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            using var smtp = new SmtpClient(host, port)
            {
                EnableSsl   = !bool.TryParse(_config["SmtpEnableSsl"], out var ssl) || ssl,   // default true; false for Mailpit
                Credentials = new NetworkCredential(user, pass)
            };
            await smtp.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[AuthEmailService] Failed to send email to {toEmail}: {ex.Message}");
        }
    }
}
