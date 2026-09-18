using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TaxiBlitz.Application.Services.Interfaces;

namespace TaxiBlitz.Web.Controllers
{
    public class SitemapController : Controller
    {
        private readonly ITourService      _tourService;
        private readonly ITourStoryService _storyService;

        public SitemapController(ITourService tourService, ITourStoryService storyService)
        {
            _tourService  = tourService;
            _storyService = storyService;
        }

        [Route("sitemap.xml")]
        public async Task<IActionResult> Index()
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var tours   = await _tourService.GetActiveAsync();
            var stories = await _storyService.GetRecentFeaturedAsync(100);

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

            void AddUrl(string loc, string changefreq, string priority, DateTime? lastmod = null)
            {
                sb.AppendLine("  <url>");
                sb.AppendLine($"    <loc>{loc}</loc>");
                if (lastmod.HasValue)
                    sb.AppendLine($"    <lastmod>{lastmod.Value:yyyy-MM-dd}</lastmod>");
                sb.AppendLine($"    <changefreq>{changefreq}</changefreq>");
                sb.AppendLine($"    <priority>{priority}</priority>");
                sb.AppendLine("  </url>");
            }

            // Static pages
            AddUrl($"{baseUrl}/",              "weekly",  "1.0", DateTime.UtcNow);
            AddUrl($"{baseUrl}/Tours",         "weekly",  "0.9", DateTime.UtcNow);
            AddUrl($"{baseUrl}/Drivers",       "monthly", "0.7");
            AddUrl($"{baseUrl}/TourStories",   "weekly",  "0.7");
            AddUrl($"{baseUrl}/Manage/Gallery","monthly", "0.5");

            // Tour detail pages — use slug-based canonical URL when available
            foreach (var tour in tours)
            {
                var tourUrl = !string.IsNullOrWhiteSpace(tour.Slug)
                    ? $"{baseUrl}/tours/{tour.Slug}"
                    : $"{baseUrl}/Tours/Details/{tour.Id}";
                AddUrl(tourUrl, "monthly", "0.9");
            }

            // Tour story pages
            foreach (var story in stories)
            {
                AddUrl($"{baseUrl}/TourStories/Details/{story.Id}", "monthly", "0.6",
                    story.CreatedDate != default ? story.CreatedDate : (DateTime?)null);
            }

            sb.AppendLine("</urlset>");

            return Content(sb.ToString(), "application/xml", Encoding.UTF8);
        }
    }
}
