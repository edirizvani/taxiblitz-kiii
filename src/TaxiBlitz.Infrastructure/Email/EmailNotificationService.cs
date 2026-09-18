using System;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TaxiBlitz.Application.Interfaces;
using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Infrastructure.Email
{
    public class EmailNotificationService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailNotificationService> _logger;

        public EmailNotificationService(IConfiguration config, ILogger<EmailNotificationService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public string BuildConfirmationText(BookingTour booking)
        {
            string driver = booking.Driver != null ? booking.Driver.Name : "Will be assigned";
            string tour   = booking.Tour   != null ? booking.Tour.Title  : "—";
            string price  = booking.Tour   != null ? $"${booking.Tour.Price:F2}" : "—";
            string finalPrice = booking.Tour != null
                ? $"${(booking.Tour.Price - (booking.DiscountAmount ?? 0)):F2}"
                : "—";
            string discount = booking.DiscountAmount.HasValue && booking.DiscountAmount > 0
                ? $" (Discount: ${booking.DiscountAmount:F2})"
                : "";

            return string.Format(
@"Dear {0},

Great news — your booking has been confirmed!

  Tour:     {1}
  Date:     {2}
  People:   {3}
  Driver:   {4}
  Price:    {5}{6}
  Final:    {7}

Have questions or need to make changes?
  Phone / WhatsApp:  +389 70 589 874
  Email:             zulirizvani@gmail.com
  Viber:             +389 70 589 874

We look forward to showing you Ohrid!

TaxiBlitz Ohrid",
                booking.NameOfBookMaker,
                tour,
                booking.BookingDateTime.ToString("dddd, MMMM d, yyyy · HH:mm"),
                booking.NumberOfPeople,
                driver,
                price,
                discount,
                finalPrice);
        }

        public void SendAdminNewBookingAlert(BookingTour booking, string adminEmail)
        {
            if (string.IsNullOrWhiteSpace(adminEmail)) return;

            string driver = booking.Driver != null ? booking.Driver.Name : "Not selected";
            string tour   = booking.Tour   != null ? booking.Tour.Title  : "—";

            string price  = booking.Tour != null ? $"${booking.Tour.Price:F2}" : "—";
            string finalPrice = booking.Tour != null
                ? $"${(booking.Tour.Price - (booking.DiscountAmount ?? 0)):F2}"
                : "—";
            string discount = booking.DiscountAmount.HasValue && booking.DiscountAmount > 0
                ? $" (Discount: ${booking.DiscountAmount:F2})"
                : "";

            string body = string.Format(
@"A new booking has been submitted and is waiting for your approval.

  Customer:  {0}
  Email:     {1}
  Phone:     {2}
  Tour:      {3}
  Date:      {4}
  People:    {5}
  Driver:    {6}
  Price:     {7}{8}
  Final:     {9}

Log in to the admin dashboard to approve or reject this booking.",
                booking.NameOfBookMaker,
                booking.CustomerEmail,
                booking.PhoneNumber,
                tour,
                booking.BookingDateTime.ToString("dddd, MMMM d, yyyy · HH:mm"),
                booking.NumberOfPeople,
                driver,
                price,
                discount,
                finalPrice);

            try
            {
                string host = _config["SmtpHost"];
                int    port = int.TryParse(_config["SmtpPort"], out int p) ? p : 587;
                string user = _config["SmtpUser"];
                string pass = _config["SmtpPass"];
                string from = _config["SmtpFrom"];

                if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from) ||
                    string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
                    return;

                string subject = string.Format("New booking pending approval — {0} on {1}",
                    tour, booking.BookingDateTime.ToString("MMM d, yyyy"));

                var message = new MailMessage
                {
                    From       = new MailAddress(from, "TaxiBlitz Ohrid"),
                    Subject    = subject,
                    Body       = body,
                    IsBodyHtml = false
                };
                message.To.Add(adminEmail);

                using (var smtp = new SmtpClient(host, port))
                {
                    smtp.EnableSsl   = !bool.TryParse(_config["SmtpEnableSsl"], out var ssl) || ssl;   // default true; false for Mailpit
                    smtp.Credentials = new NetworkCredential(user, pass);
                    smtp.Send(message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send admin booking alert email to {AdminEmail}", adminEmail);
            }
        }

        public string SendBookingConfirmation(BookingTour booking)
        {
            if (string.IsNullOrWhiteSpace(booking.CustomerEmail))
                return "No customer email on record.";

            try
            {
                string host = _config["SmtpHost"];
                int    port = int.TryParse(_config["SmtpPort"], out int p) ? p : 587;
                string user = _config["SmtpUser"];
                string pass = _config["SmtpPass"];
                string from = _config["SmtpFrom"];

                if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from) ||
                    string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
                    return "SMTP not fully configured — ensure SmtpHost, SmtpFrom, SmtpUser and SmtpPass are set.";

                var message = new MailMessage
                {
                    From       = new MailAddress(from, "TaxiBlitz Ohrid"),
                    Subject    = "Your TaxiBlitz Ohrid booking is confirmed",
                    Body       = BuildConfirmationText(booking),
                    IsBodyHtml = false
                };
                message.To.Add(booking.CustomerEmail);

                using (var smtp = new SmtpClient(host, port))
                {
                    smtp.EnableSsl   = !bool.TryParse(_config["SmtpEnableSsl"], out var ssl) || ssl;   // default true; false for Mailpit
                    smtp.Credentials = new NetworkCredential(user, pass);
                    smtp.Send(message);
                }

                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
