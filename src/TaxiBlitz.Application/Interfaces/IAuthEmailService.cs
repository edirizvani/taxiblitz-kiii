namespace TaxiBlitz.Application.Interfaces;

public interface IAuthEmailService
{
    Task SendEmailConfirmationAsync(string toEmail, string userName, string confirmationLink);
    Task SendPasswordResetAsync(string toEmail, string userName, string resetLink);
    Task SendAlreadyRegisteredAsync(string toEmail, string userName, string loginUrl);
}
