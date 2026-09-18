namespace TaxiBlitz.Shared.Extensions
{
    public static class DateExtensions
    {
        public static string ToDisplayDate(this DateTime date) =>
            date.ToString("MMMM dd, yyyy");

        public static string ToDisplayDate(this DateTime? date) =>
            date.HasValue ? date.Value.ToDisplayDate() : "";
    }
}
