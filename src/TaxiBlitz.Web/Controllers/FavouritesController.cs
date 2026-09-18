using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxiBlitz.Application.Services.Interfaces;

namespace TaxiBlitz.Web.Controllers
{
    [Authorize]
    public class FavouritesController : Controller
    {
        private readonly IFavouriteService _favService;
        private readonly ITourService      _tourService;

        public FavouritesController(IFavouriteService favService, ITourService tourService)
        {
            _favService  = favService;
            _tourService = tourService;
        }

        public async Task<IActionResult> MyWishlist()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var favs = await _favService.GetUserFavouritesAsync(userId);
            return View(favs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id)
        {
            var tour = await _tourService.GetByIdAsync(id);
            if (tour == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isFav = await _favService.ToggleAsync(userId, id);
            return Json(new { isFavourite = isFav });
        }
    }
}
