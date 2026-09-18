using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Infrastructure.Receipts
{
    public class ReceiptService : IReceiptService
    {
        public ReceiptService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenerateBookingReceipt(BookingTour booking)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(48);
                    page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(11).FontColor("#1A231E"));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("TaxiBlitz Ohrid")
                                    .FontSize(22).FontColor("#0B100E").Bold();
                                c.Item().Text("Your local guide to Lake Ohrid")
                                    .FontSize(11).FontColor("#5A6560");
                            });
                            row.ConstantItem(120).AlignRight().Column(c =>
                            {
                                c.Item().Text("BOOKING RECEIPT")
                                    .FontSize(10).FontColor("#5A6560").Bold()
                                    .LetterSpacing(0.1f);
                                c.Item().Text("#" + booking.Id)
                                    .FontSize(18).Bold().FontColor("#0891B2");
                            });
                        });
                        col.Item().PaddingTop(12).LineHorizontal(1.5f).LineColor("#E8ECEB");
                    });

                    page.Content().PaddingTop(24).Column(col =>
                    {
                        // Status badge
                        string statusColor = booking.Status == "Approved" ? "#16A34A"
                            : booking.Status == "Pending" ? "#CA8A04" : "#DC2626";
                        col.Item().Row(row =>
                        {
                            row.AutoItem().Background(statusColor + "22").Padding(6)
                                .PaddingHorizontal(14).Text(booking.Status.ToUpper())
                                .FontSize(10).Bold().FontColor(statusColor);
                        });
                        col.Item().PaddingTop(20).Text("Booking Details")
                            .FontSize(14).Bold().FontColor("#0B100E");
                        col.Item().PaddingTop(8).Table(table =>
                        {
                            table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });

                            void Row(string label, string value)
                            {
                                table.Cell().BorderBottom(1).BorderColor("#E8ECEB").Padding(8)
                                    .Text(label).FontColor("#5A6560").FontSize(10);
                                table.Cell().BorderBottom(1).BorderColor("#E8ECEB").Padding(8)
                                    .Text(value ?? "-").Bold().FontColor("#0B100E");
                            }

                            Row("Tour",           booking.Tour?.Title ?? "-");
                            Row("Date & Time",    booking.BookingDateTime.ToString("dddd, MMMM d, yyyy · HH:mm"));
                            Row("People",         booking.NumberOfPeople.ToString());
                            Row("Driver",         booking.Driver?.Name ?? "To be assigned");
                            Row("Booked by",      booking.NameOfBookMaker);
                            Row("Contact email",  booking.CustomerEmail);
                            Row("Phone",          booking.PhoneNumber);
                            if (booking.Tour != null)
                            {
                                Row("Tour price",     "€" + booking.Tour.Price.ToString("F2"));
                                if (booking.DiscountAmount.HasValue && booking.DiscountAmount > 0)
                                    Row("Discount",   "-€" + booking.DiscountAmount.Value.ToString("F2"));
                                decimal finalPrice = booking.Tour.Price - (booking.DiscountAmount ?? 0);
                                Row("Final price",    "€" + finalPrice.ToString("F2"));
                            }
                        });

                        col.Item().PaddingTop(28).Background("#F4F6F5").Padding(16).Column(note =>
                        {
                            note.Item().Text("Important Information").Bold().FontSize(11);
                            note.Item().PaddingTop(6).Text(
                                "• Free cancellation up to 24 hours before the tour.\n" +
                                "• Please arrive 10 minutes before departure.\n" +
                                "• For changes contact us on WhatsApp: +389 70 589 874"
                            ).FontColor("#5A6560").FontSize(10);
                        });
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("TaxiBlitz Ohrid  ·  zulirizvani@gmail.com  ·  +389 70 589 874")
                            .FontSize(9).FontColor("#8A9490");
                        text.Span("  ·  Generated ").FontSize(9).FontColor("#8A9490");
                        text.Span(DateTime.Now.ToString("MMM d, yyyy")).FontSize(9).FontColor("#8A9490");
                    });
                });
            }).GeneratePdf();
        }
    }
}
