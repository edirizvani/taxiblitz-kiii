using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaxiBlitz.Application.Services.Interfaces;
using TaxiBlitz.Domain.Identity;

namespace TaxiBlitz.Web.Controllers
{
    [Authorize]
    public class ReferralController : Controller
    {
        private readonly IReferralService _referralService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReferralController(IReferralService referralService, UserManager<ApplicationUser> userManager)
        {
            _referralService = referralService;
            _userManager     = userManager;
        }

        public async Task<IActionResult> MyCode()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // Cookie is stale — user no longer exists in the database
                await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                return RedirectToAction("Login", "Account");
            }

            var code = await _referralService.GetOrCreateCodeAsync(user.Id);
            return View(code);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ValidateCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Json(new { valid = false, message = "No code provided." });

            var userId = User.Identity?.IsAuthenticated == true
                ? _userManager.GetUserId(User)
                : null;

            var (valid, discount, message, _) = await _referralService.ValidateCodeAsync(code, userId ?? "");
            return Json(new { valid, discountPercent = discount, message });
        }
    }
}
