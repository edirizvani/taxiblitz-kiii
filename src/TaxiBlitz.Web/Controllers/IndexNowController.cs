using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxiBlitz.Application.Services.Interfaces;

namespace TaxiBlitz.Web.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class IndexNowController : Controller
    {
        private const string Key         = "taxiblitz2024ohrid";
        private const string IndexNowUrl = "https://api.indexnow.org/indexnow";

        private readonly ITourService      _tourService;
        private readonly ITourStoryService _storyService;
        private readonly IHttpClientFactory _httpClientFactory;

        public IndexNowController(
            ITourService tourService,
            ITourStoryService storyService,
            IHttpClientFactory httpClientFactory)
        {
            _tourService       = tourService;
            _storyService      = storyService;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost]
        [Route("indexnow/submit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit()
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var urls = new List<string>
            {
                $"{baseUrl}/",
                $"{baseUrl}/Tours",
                $"{baseUrl}/Drivers",
                $"{baseUrl}/TourStories",
                $"{baseUrl}/Manage/Gallery",
            };

            var tours = await _tourService.GetActiveAsync();
            foreach (var tour in tours)
                urls.Add($"{baseUrl}/Tours/Details/{tour.Id}");

            var stories = await _storyService.GetRecentFeaturedAsync(100);
            foreach (var story in stories)
                urls.Add($"{baseUrl}/TourStories/Details/{story.Id}");

            var payload = new
            {
                host        = Request.Host.Host,
                key         = Key,
                keyLocation = $"{baseUrl}/{Key}.txt",
                urlList     = urls
            };

            var json    = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var client   = _httpClientFactory.CreateClient();
            var response = await client.PostAsync(IndexNowUrl, content);

            TempData["IndexNowStatus"]  = (int)response.StatusCode;
            TempData["IndexNowMessage"] = response.IsSuccessStatusCode
                ? $"Success — {urls.Count} URLs submitted."
                : $"Failed ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync()}";

            return RedirectToAction(nameof(Result));
        }

        [HttpGet]
        [Route("indexnow/submit")]
        public IActionResult Result()
        {
            ViewBag.Status  = TempData["IndexNowStatus"];
            ViewBag.Message = TempData["IndexNowMessage"];
            return View();
        }
    }
}
