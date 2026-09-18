using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaxiBlitz.Application.Services.Interfaces;
using TaxiBlitz.Domain.Entities;

namespace TaxiBlitz.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IReviewService    _reviewService;
        private readonly ITourService      _tourService;
        private readonly ITourStoryService _storyService;
        private readonly IDriverService    _driverService;

        public HomeController(IReviewService reviewService, ITourService tourService,
            ITourStoryService storyService, IDriverService driverService)
        {
            _reviewService = reviewService;
            _tourService   = tourService;
            _storyService  = storyService;
            _driverService = driverService;
        }

        public async Task<IActionResult> Index()
        {
            var allReviews = await _reviewService.GetAllAsync();
            var (reviewCount, reviewAverage) = await _reviewService.GetStatsAsync();

            // Fisher-Yates shuffle
            var rng = new Random();
            int n   = allReviews.Count;
            while (n > 1)
            {
                n--;
                int k   = rng.Next(n + 1);
                var tmp = allReviews[k];
                allReviews[k] = allReviews[n];
                allReviews[n] = tmp;
            }

            ViewBag.Reviews              = allReviews;
            ViewBag.FeaturedTours        = await _tourService.GetActiveAsync();
            ViewBag.ReviewRatingCount    = reviewCount;
            ViewBag.ReviewRatingAverage  = reviewAverage;
            ViewBag.RecentStories        = await _storyService.GetRecentFeaturedAsync(3);
            ViewBag.FeaturedDrivers      = await _driverService.GetFeaturedAsync(3);

            ViewBag.Title          = "Taxi Ohrid — Private Tours, Airport Transfers & Day Trips";
            ViewBag.MetaDescription = "Taxi Blitz Ohrid: private tours, airport transfers and day trips across Lake Ohrid, Skopje, Tirana and Albania. Local drivers with 25+ years experience. Book 24/7.";
            ViewBag.CanonicalUrl   = Url.Action("Index", "Home", null, Request.Scheme);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(string reviewerName, string reviewText, string imageUrl, int? rating)
        {
            var review = new Review
            {
                ReviewerName = reviewerName,
                Text         = reviewText,
                ReviewDate   = DateTime.Now,
                ImageUrl     = imageUrl,
                Rating       = rating.HasValue && rating.Value >= 1 && rating.Value <= 5 ? rating : 5
            };

            bool added = await _reviewService.TryAddAsync(review);

            if (!added)
            {
                TempData["Error"] = "The daily limit of 5 reviews has been reached. Please try again tomorrow.";
                return RedirectToAction("Index");
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    review = new
                    {
                        review.ReviewerName,
                        review.Text,
                        review.ImageUrl,
                        review.Rating,
                        ReviewDate = review.ReviewDate.ToString("yyyy-MM-dd")
                    }
                });
            }

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Error(int? statusCode)
        {
            if (statusCode == 404) return View("Error404");
            return View("Error500");
        }
    }
}
