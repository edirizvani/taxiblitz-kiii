using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TaxiBlitz.Shared.Extensions
{
    public static class StringExtensions
    {
        public static string ToSlug(this string title)
        {
            if (string.IsNullOrEmpty(title)) return "";

            // Decompose Unicode (e.g. ë → e + combining diacritic), then keep only ASCII
            var normalized = title.Normalize(NormalizationForm.FormD);
            var ascii      = new StringBuilder();
            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    ascii.Append(c);
            }

            var slug = ascii.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
            slug = Regex.Replace(slug, @"[\s_]+", "-");
            slug = Regex.Replace(slug, @"[^a-z0-9-]", "");
            slug = Regex.Replace(slug, @"-+", "-");
            return slug.Trim('-');
        }

        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength) return value;
            return value[..maxLength] + "…";
        }
    }
}
