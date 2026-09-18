using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TaxiBlitz.Application.ViewModels;
using TaxiBlitz.Domain.Identity;

namespace TaxiBlitz.Web.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ManageController> _logger;

        public ManageController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment env,
            ILogger<ManageController> logger)
        {
            _userManager   = userManager;
            _signInManager = signInManager;
            _env           = env;
            _logger        = logger;
        }

        public async Task<IActionResult> Index(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess   ? "Your password has been set."
                : message == ManageMessageId.SetTwoFactorSuccess  ? "Your two-factor authentication provider has been set."
                : message == ManageMessageId.Error                ? "An error has occurred."
                : message == ManageMessageId.AddPhoneSuccess      ? "Your phone number was added."
                : message == ManageMessageId.RemovePhoneSuccess   ? "Your phone number was removed."
                : "";

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var model = new IndexViewModel
            {
                HasPassword      = user.PasswordHash != null,
                PhoneNumber      = await _userManager.GetPhoneNumberAsync(user),
                TwoFactor        = await _userManager.GetTwoFactorEnabledAsync(user),
                Logins           = await _userManager.GetLoginsAsync(user),
                BrowserRemembered = await _signInManager.IsTwoFactorClientRememberedAsync(user)
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveLogin(string loginProvider, string providerKey)
        {
            ManageMessageId? message;
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var result = await _userManager.RemoveLoginAsync(user, loginProvider, providerKey);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                message = ManageMessageId.RemoveLoginSuccess;
            }
            else
            {
                message = ManageMessageId.Error;
            }
            return RedirectToAction("ManageLogins", new { Message = message });
        }

        public IActionResult AddPhoneNumber()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPhoneNumber(AddPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var code = await _userManager.GenerateChangePhoneNumberTokenAsync(user, model.Number);
            return RedirectToAction("VerifyPhoneNumber", new { PhoneNumber = model.Number });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableTwoFactorAuthentication()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            await _userManager.SetTwoFactorEnabledAsync(user, true);
            await _signInManager.RefreshSignInAsync(user);
            return RedirectToAction("Index", "Manage");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableTwoFactorAuthentication()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            await _userManager.SetTwoFactorEnabledAsync(user, false);
            await _signInManager.RefreshSignInAsync(user);
            return RedirectToAction("Index", "Manage");
        }

        public async Task<IActionResult> VerifyPhoneNumber(string phoneNumber)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return View("Error");
            return phoneNumber == null ? View("Error") : View(new VerifyPhoneNumberViewModel { PhoneNumber = phoneNumber });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var result = await _userManager.ChangePhoneNumberAsync(user, model.PhoneNumber, model.Code);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                return RedirectToAction("Index", new { Message = ManageMessageId.AddPhoneSuccess });
            }

            ModelState.AddModelError("", "Failed to verify phone");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePhoneNumber()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var result = await _userManager.SetPhoneNumberAsync(user, null);
            if (!result.Succeeded)
                return RedirectToAction("Index", new { Message = ManageMessageId.Error });

            await _signInManager.RefreshSignInAsync(user);
            return RedirectToAction("Index", new { Message = ManageMessageId.RemovePhoneSuccess });
        }

        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);
                }
                await _signInManager.RefreshSignInAsync(user);
                return RedirectToAction("Index", new { Message = ManageMessageId.ChangePasswordSuccess });
            }
            AddErrors(result);
            return View(model);
        }

        public IActionResult SetPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return RedirectToAction("Login", "Account");
                var result = await _userManager.AddPasswordAsync(user, model.NewPassword);
                if (result.Succeeded)
                {
                    await _signInManager.RefreshSignInAsync(user);
                    return RedirectToAction("Index", new { Message = ManageMessageId.SetPasswordSuccess });
                }
                AddErrors(result);
            }

            return View(model);
        }

        public async Task<IActionResult> ManageLogins(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return View("Error");

            var userLogins   = await _userManager.GetLoginsAsync(user);
            var otherLogins  = (await _signInManager.GetExternalAuthenticationSchemesAsync())
                                .Where(auth => userLogins.All(ul => auth.Name != ul.LoginProvider))
                                .ToList();

            ViewBag.ShowRemoveButton = user.PasswordHash != null || userLogins.Count > 1;
            return View(new ManageLoginsViewModel
            {
                CurrentLogins = userLogins,
                OtherLogins   = otherLogins
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult LinkLogin(string provider)
        {
            var userId      = _userManager.GetUserId(User);
            var redirectUrl = Url.Action(nameof(LinkLoginCallback), "Manage");
            var properties  = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, userId);
            return Challenge(properties, provider);
        }

        public async Task<IActionResult> LinkLoginCallback()
        {
            var userId    = _userManager.GetUserId(User);
            var loginInfo = await _signInManager.GetExternalLoginInfoAsync(userId);
            if (loginInfo == null)
                return RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });

            var user   = await _userManager.GetUserAsync(User);
            var result = await _userManager.AddLoginAsync(user, loginInfo);
            return result.Succeeded
                ? RedirectToAction("ManageLogins")
                : RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
        }

        public async Task<IActionResult> CompleteProfile(string returnUrl)
        {
            var user = await _userManager.GetUserAsync(User);
            var model = new CompleteProfileViewModel
            {
                FirstName         = user?.FirstName,
                LastName          = user?.LastName,
                PhoneNumber       = user?.PhoneNumber,
                City              = user?.City,
                Country           = user?.Country,
                Bio               = user?.Bio,
                DateOfBirth       = user?.DateOfBirth,
                Gender            = user?.Gender,
                PreferredLanguage = user?.PreferredLanguage
            };
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteProfile(CompleteProfileViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                ViewBag.ReturnUrl = returnUrl;
                ModelState.AddModelError("", "User not found.");
                return View(model);
            }

            user.FirstName         = model.FirstName;
            user.LastName          = model.LastName;
            user.PhoneNumber       = model.PhoneNumber;
            user.City              = model.City;
            user.Country           = model.Country;
            user.Bio               = model.Bio;
            user.DateOfBirth       = model.DateOfBirth;
            user.Gender            = model.Gender;
            user.PreferredLanguage = model.PreferredLanguage;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                AddErrors(result);
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            HttpContext.Session.SetString("ProfileComplete", "true");

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            var model = new EditProfileViewModel
            {
                FirstName         = user?.FirstName,
                LastName          = user?.LastName,
                City              = user?.City,
                Country           = user?.Country,
                Bio               = user?.Bio,
                DateOfBirth       = user?.DateOfBirth,
                Gender            = user?.Gender,
                PreferredLanguage = user?.PreferredLanguage
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                ModelState.AddModelError("", "User not found.");
                return View(model);
            }

            user.FirstName         = model.FirstName;
            user.LastName          = model.LastName;
            user.City              = model.City;
            user.Country           = model.Country;
            user.Bio               = model.Bio;
            user.DateOfBirth       = model.DateOfBirth;
            user.Gender            = model.Gender;
            user.PreferredLanguage = model.PreferredLanguage;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(model);
            }

            HttpContext.Session.SetString("ProfileComplete", "true");
            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction("EditProfile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePicture(IFormFile profilePicture)
        {
            if (profilePicture == null || profilePicture.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select an image.";
                return RedirectToAction("EditProfile");
            }

            var ext     = Path.GetExtension(profilePicture.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            if (!System.Array.Exists(allowed, e => e == ext))
            {
                TempData["ErrorMessage"] = "Only JPG, PNG, GIF, or WEBP files are allowed.";
                return RedirectToAction("EditProfile");
            }

            if (profilePicture.Length > 5 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "Image must be under 5 MB.";
                return RedirectToAction("EditProfile");
            }

            var folderPath = Path.Combine(_env.WebRootPath, "profile-pics");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var fileName = Guid.NewGuid() + ext;
            var filePath = Path.Combine(folderPath, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
                await profilePicture.CopyToAsync(stream);

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                user.ProfilePictureUrl = "/profile-pics/" + fileName;
                await _userManager.UpdateAsync(user);
            }

            return RedirectToAction("EditProfile");
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadImage(IFormFile imageFile)
        {
            try
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var folderPath = Path.Combine(_env.WebRootPath, "gallery");
                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);

                    var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(imageFile.FileName)}";
                    var filePath = Path.Combine(folderPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await imageFile.CopyToAsync(stream);

                    if (System.IO.File.Exists(filePath))
                        TempData["SuccessMessage"] = "Image uploaded successfully!";
                    else
                        TempData["ErrorMessage"] = "Error: File was not saved properly.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Please select an image to upload.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading gallery image");
                TempData["ErrorMessage"] = "An error occurred. Please try again.";
            }

            return RedirectToAction("Gallery");
        }

        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public IActionResult UploadImage()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Gallery()
        {
            var fullPath = Path.Combine(_env.WebRootPath, "gallery");
            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);

            var images = Directory.GetFiles(fullPath).Select(Path.GetFileName).ToList();
            return View(images);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public IActionResult DeleteImage(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Invalid file name." });
                TempData["ErrorMessage"] = "Invalid file name.";
                return RedirectToAction("UploadImage");
            }

            var galleryRoot = Path.GetFullPath(Path.Combine(_env.WebRootPath, "gallery"));
            var resolvedPath = Path.GetFullPath(Path.Combine(galleryRoot, fileName));
            if (!resolvedPath.StartsWith(galleryRoot + Path.DirectorySeparatorChar))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Invalid file name." });
                return Forbid();
            }

            try
            {
                var filePath = resolvedPath;
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = true });
                    TempData["SuccessMessage"] = "Image deleted successfully.";
                }
                else
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = false, message = "Image not found." });
                    TempData["ErrorMessage"] = "Image not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting gallery image {FileName}", fileName);
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Error deleting image." });
                TempData["ErrorMessage"] = "Error deleting image. Please try again.";
            }

            return RedirectToAction("UploadImage");
        }

        #region Helpers
        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
        }

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
        }
        #endregion
    }
}
