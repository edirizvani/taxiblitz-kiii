using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxiBlitz.Application.Services.Interfaces;
using TaxiBlitz.Domain.Entities;
using TaxiBlitz.Domain.Seo;
using TaxiBlitz.Infrastructure.Maps;

namespace TaxiBlitz.Web.Controllers
{
    public class ToursController : Controller
    {
        private readonly ITourService     _tourService;
        private readonly TourRouteService _routeService;
        private readonly IFavouriteService _favService;
        private readonly IWeatherService  _weatherService;

        public ToursController(ITourService tourService, TourRouteService routeService,
            IFavouriteService favService, IWeatherService weatherService)
        {
            _tourService    = tourService;
            _routeService   = routeService;
            _favService     = favService;
            _weatherService = weatherService;
        }

        public async Task<IActionResult> Index(string q, string sortBy, int? page)
        {
            int pageSize   = 9;
            int pageNumber = page ?? 1;

            var (tours, totalCount) = await _tourService.SearchAsync(q, sortBy, pageNumber, pageSize);

            ViewBag.Query       = q ?? "";
            ViewBag.SortBy      = sortBy ?? "";
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages  = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.TotalCount  = totalCount;

            if (User.Identity.IsAuthenticated)
            {
                var favs = await _favService.GetUserFavouritesAsync(User.FindFirstValue(ClaimTypes.NameIdentifier));
                ViewBag.FavouriteIds = favs.Select(f => f.TourId).ToHashSet();
            }

            ViewBag.Title          = "Tours from Ohrid — Private Day Trips, Albania & North Macedonia";
            ViewBag.MetaDescription = "Browse all private tours from Ohrid: Albania day trips, Tirana, Galicica, Saint Naum, Skopje, Mavrovo and more. Book your Ohrid tour with a local expert driver.";
            ViewBag.CanonicalUrl   = Url.Action("Index", "Tours", null, Request.Scheme);

            return View(tours);
        }

        // Legacy numeric-ID route — 301-redirects to the slug URL when available
        public async Task<IActionResult> Details(int? id)
        {
            if (!id.HasValue) return BadRequest();
            var tour = await _tourService.GetByIdAsync(id.Value);
            if (tour == null) return NotFound();
            if (!string.IsNullOrWhiteSpace(tour.Slug))
                return RedirectPermanent(Url.Action(nameof(DetailsBySlug), new { slug = tour.Slug }));
            return await RenderTourView(tour);
        }

        // Canonical slug-based route — /tours/{slug}
        [Route("tours/{slug:regex(.*-.*)}")]
        public async Task<IActionResult> DetailsBySlug(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return BadRequest();
            var tour = await _tourService.GetBySlugAsync(slug);
            if (tour == null) return NotFound();
            return await RenderTourView(tour);
        }

        private async Task<IActionResult> RenderTourView(Tour tour)
        {
            var routePoints  = await _routeService.BuildRoutePointsAsync(tour);
            var canonicalUrl = !string.IsNullOrWhiteSpace(tour.Slug)
                ? Url.Action(nameof(DetailsBySlug), "Tours", new { slug = tour.Slug }, Request.Scheme)
                : Url.Action("Details", "Tours", new { id = tour.Id }, Request.Scheme);

            var breadcrumbs = new[]
            {
                new BreadcrumbItem { Name = "Home",     Url = Url.Action("Index", "Home",  null, Request.Scheme) },
                new BreadcrumbItem { Name = "Tours",    Url = Url.Action("Index", "Tours", null, Request.Scheme) },
                new BreadcrumbItem { Name = tour.Title, Url = canonicalUrl, IsCurrentPage = true }
            };

            var (reviewCount, averageRating) = await _tourService.GetReviewStatsAsync();

            var durationText = string.IsNullOrWhiteSpace(tour.Duration)      ? "a full day"                      : tour.Duration;
            var startingText = string.IsNullOrWhiteSpace(tour.StartingPoint) ? "Ohrid"                           : tour.StartingPoint;

            var metaDesc = !string.IsNullOrWhiteSpace(tour.Description) && tour.Description.Length > 20
                ? (tour.Description.Length > 155 ? tour.Description.Substring(0, 152) + "…" : tour.Description)
                : $"Private {tour.Title} from {startingText}. {durationText} tour with an expert local driver. Book with Taxi Blitz Ohrid.";

            ViewBag.Title           = $"{tour.Title} — Private Tour from {startingText}";
            ViewBag.MetaDescription = metaDesc;
            ViewBag.CanonicalUrl    = canonicalUrl;
            ViewBag.OgImage         = string.IsNullOrWhiteSpace(tour.PhotoProfileUrl) ? "https://taxiblitzohrid.com/img/og-image.jpg" : tour.PhotoProfileUrl;
            ViewBag.BreadcrumbItems = breadcrumbs;
            ViewBag.TourSeo = new TourSeoViewModel
            {
                Tour         = tour,
                CanonicalUrl = canonicalUrl,
                ImageUrl     = string.IsNullOrWhiteSpace(tour.PhotoProfileUrl) ? Url.Content("~/img/background.jpg") : tour.PhotoProfileUrl,
                RoutePoints  = routePoints,
                Breadcrumbs  = breadcrumbs,
                FaqItems = new[]
                {
                    new FaqItem { Question = $"How do I book the {tour.Title}?",          Answer = $"Click the 'Book now' button on this page, fill in your travel date and passenger count, and you will receive instant confirmation. You can also contact us via WhatsApp or call +389 70 589 874." },
                    new FaqItem { Question = $"How long does the {tour.Title} take?",      Answer = $"The tour lasts approximately {durationText}. Departure is typically from {startingText}, and return to the same point can be arranged." },
                    new FaqItem { Question = $"Where does the {tour.Title} start?",        Answer = $"The tour departs from {startingText}. Hotel or accommodation pick-up within the Ohrid area is included at no extra charge." },
                    new FaqItem { Question = "Can I customise the itinerary?",             Answer = "Yes. All Taxi Blitz Ohrid tours can be adjusted — stops added or removed — to suit your group. Contact us before booking to discuss your preferences." },
                    new FaqItem { Question = "What is included in the tour price?",        Answer = $"The price of €{tour.Price} covers private vehicle, fuel, and a local English-speaking driver. Entrance fees, meals and tips are not included unless stated." },
                    new FaqItem { Question = "Is there free cancellation?",                Answer = "Yes. Cancellations made at least 24 hours before departure receive a full refund. Late cancellations may incur a fee." },
                    new FaqItem { Question = "How many passengers can the vehicle carry?", Answer = "Our standard vehicles comfortably seat up to 4 passengers with luggage. For larger groups please contact us in advance and we will arrange a suitable vehicle." }
                },
                ReviewCount   = reviewCount,
                AverageRating = averageRating
            };

            ViewBag.Weather = await _weatherService.GetForecastAsync(tour.StartingPoint ?? "Ohrid");

            if (User.Identity.IsAuthenticated)
                ViewBag.IsFavourite = await _favService.IsFavouriteAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), tour.Id);

            return View("Details", tour);
        }

        [Authorize(Roles = "Administrator")]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,Price,Duration,YouTubeLink,PhotoProfileUrl,RouteStopsText,CulturalHighlights,StartingPoint,EndingPoint")] Tour tour)
        {
            if (ModelState.IsValid)
            {
                await _tourService.AddAsync(tour);
                return RedirectToAction("Index");
            }
            return View(tour);
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (!id.HasValue) return BadRequest();
            var tour = await _tourService.GetByIdAsync(id.Value);
            if (tour == null) return NotFound();
            return View(tour);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit([Bind("Id,Title,Description,Price,Duration,YouTubeLink,PhotoProfileUrl,RouteStopsText,CulturalHighlights,StartingPoint,EndingPoint")] Tour tour)
        {
            if (ModelState.IsValid)
            {
                await _tourService.UpdateAsync(tour);
                return RedirectToAction("Index");
            }
            return View(tour);
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (!id.HasValue) return BadRequest();
            var tour = await _tourService.GetByIdAsync(id.Value);
            if (tour == null) return NotFound();
            return View(tour);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _tourService.DeleteAsync(id);
            return RedirectToAction("Index");
        }
    }
}
