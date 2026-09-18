using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using TaxiBlitz.Application.Interfaces;
using TaxiBlitz.Application.ViewModels;
using TaxiBlitz.Domain.Identity;

namespace TaxiBlitz.Web.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IAuthEmailService _authEmailService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IAuthEmailService authEmailService)
        {
            _userManager      = userManager;
            _signInManager    = signInManager;
            _authEmailService = authEmailService;
        }

        // ── Login ────────────────────────────────────────────────────────

        [AllowAnonymous]
        public IActionResult Login(string? returnUrl)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth-login")]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Login", model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);

            if (existingUser == null)
            {
                await _userManager.CheckPasswordAsync(new ApplicationUser(), model.Password);
                ModelState.AddModelError("", "Invalid email or password.");
                return View("Login", model);
            }

            if (!await _userManager.IsEmailConfirmedAsync(existingUser))
            {
                ModelState.AddModelError("", "Please confirm your email address before logging in. Check your inbox for the confirmation link.");
                return View("Login", model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                await _signInManager.SignOutAsync();
                await _signInManager.SignInAsync(existingUser, isPersistent: model.RememberMe);
                return RedirectToLocal(model.ReturnUrl);
            }
            if (result.IsLockedOut)
            {
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(existingUser);
                ViewBag.LockoutEnd = lockoutEnd?.UtcDateTime.ToString("o");
                return View("Lockout");
            }
            if (result.RequiresTwoFactor)
                return RedirectToAction("SendCode", new { ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });

            ModelState.AddModelError("", "Incorrect email or password.");
            return View("Login", model);
        }

        // ── Register ─────────────────────────────────────────────────────

        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth-register")]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null)
            {
                var loginUrl = Url.Action("Login", "Account", null, Request.Scheme)!;
                await _authEmailService.SendAlreadyRegisteredAsync(
                    existing.Email,
                    !string.IsNullOrWhiteSpace(existing.FirstName) ? existing.FirstName : existing.Email,
                    loginUrl);
                return RedirectToAction("RegisterConfirmation", new { email = model.Email });
            }

            var user = new ApplicationUser
            {
                UserName    = model.Email,
                Email       = model.Email,
                PhoneNumber = model.PhoneNumber,
                FirstName   = model.FirstName,
                LastName    = model.LastName
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, "User");

            // Generate email confirmation token and send verification email
            var token        = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var confirmLink  = Url.Action("ConfirmEmail", "Account",
                new { userId = user.Id, code = encodedToken }, Request.Scheme);

            await _authEmailService.SendEmailConfirmationAsync(user.Email, user.FirstName, confirmLink);

            return RedirectToAction("RegisterConfirmation", new { email = model.Email });
        }

        [AllowAnonymous]
        public IActionResult RegisterConfirmation(string email)
        {
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth-forgot")]
        public async Task<IActionResult> ResendConfirmation(string email)
        {
            if (!string.IsNullOrWhiteSpace(email))
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user != null && !await _userManager.IsEmailConfirmedAsync(user))
                {
                    var token        = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                    var confirmLink  = Url.Action("ConfirmEmail", "Account",
                        new { userId = user.Id, code = encodedToken }, Request.Scheme);
                    await _authEmailService.SendEmailConfirmationAsync(
                        user.Email,
                        !string.IsNullOrWhiteSpace(user.FirstName) ? user.FirstName : user.Email,
                        confirmLink);
                }
            }
            TempData["Info"] = "If that email is registered and unconfirmed, we've sent a new confirmation link.";
            return RedirectToAction("RegisterConfirmation", new { email });
        }

        // ── Email Confirmation ────────────────────────────────────────────

        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
                return View("Error");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return View("Error");

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result       = await _userManager.ConfirmEmailAsync(user, decodedToken);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        // ── Forgot / Reset Password ───────────────────────────────────────

        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth-forgot")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            // Always show confirmation page to prevent user enumeration
            if (user == null)
                return View("ForgotPasswordConfirmation");

            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                // Resend the confirmation email so the user can unblock themselves
                var confirmToken  = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedConfirm = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(confirmToken));
                var confirmLink   = Url.Action("ConfirmEmail", "Account",
                    new { userId = user.Id, code = encodedConfirm }, Request.Scheme);
                await _authEmailService.SendEmailConfirmationAsync(
                    user.Email,
                    !string.IsNullOrWhiteSpace(user.FirstName) ? user.FirstName : user.Email,
                    confirmLink);

                TempData["Info"] = "Your email address is not yet confirmed. We've sent you a new confirmation link — please check your inbox before signing in.";
                return RedirectToAction("Login");
            }

            var token        = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var resetLink    = Url.Action("ResetPassword", "Account",
                new { userId = user.Id, code = encodedToken }, Request.Scheme);

            await _authEmailService.SendPasswordResetAsync(
                user.Email,
                !string.IsNullOrWhiteSpace(user.FirstName) ? user.FirstName : user.Email,
                resetLink);

            return View("ForgotPasswordConfirmation");
        }

        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(string userId, string code)
        {
            if (userId == null || code == null)
                return View("Error");

            var user = await _userManager.FindByIdAsync(userId);
            return View(new ResetPasswordViewModel
            {
                Code   = code,
                UserId = userId,
                Email  = user?.Email
            });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
                return RedirectToAction("ResetPasswordConfirmation");

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Code));
            var result       = await _userManager.ResetPasswordAsync(user, decodedToken, model.Password);
            if (result.Succeeded)
            {
                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);
                }
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("ResetPasswordConfirmation");
            }

            AddErrors(result);
            return View(model);
        }

        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        // ── Google / External OAuth ───────────────────────────────────────

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLogin(string provider, string returnUrl)
        {
            var schemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
            if (!schemes.Any(s => s.Name == provider))
            {
                TempData["GoogleError"] = $"Sign-in with {provider} is not available right now. Please contact the administrator.";
                return RedirectToAction("Login");
            }

            // Relative callback URL — the OAuth2 redirect_uri sent to Google is derived from
            // CallbackPath (/signin-google), not from this property. This is the internal
            // post-callback redirect target only.
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account");
            var properties  = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);

            // Store returnUrl in the OAuth state so it survives the round-trip to Google
            if (!string.IsNullOrEmpty(returnUrl))
                properties.Items["returnUrl"] = returnUrl;

            return Challenge(properties, provider);
        }

        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback()
        {
            var loginInfo = await _signInManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                TempData["GoogleError"] = "Could not retrieve your information from Google. Please try again.";
                return RedirectToAction("Login");
            }

            // Extract returnUrl from the OAuth state where we stored it
            string? returnUrl = null;
            loginInfo.AuthenticationProperties?.Items.TryGetValue("returnUrl", out returnUrl);

            var result = await _signInManager.ExternalLoginSignInAsync(
                loginInfo.LoginProvider, loginInfo.ProviderKey, isPersistent: false);

            if (result.Succeeded)
            {
                // Clean up the external auth cookie now that sign-in is complete
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
                return RedirectToLocal(returnUrl);
            }
            if (result.IsLockedOut)
                return View("Lockout");

            // New user — show the confirmation form
            var email        = loginInfo.Principal.FindFirstValue(ClaimTypes.Email);
            var existingUser = await _userManager.FindByEmailAsync(email);

            // Link Google to the existing account and sign in directly.
            // ProfileCompleteFilter handles redirecting to CompleteProfile if the profile is incomplete.
            if (existingUser != null)
            {
                var linkResult = await _userManager.AddLoginAsync(existingUser, loginInfo);
                if (linkResult.Succeeded || linkResult.Errors.All(e => e.Code == "LoginAlreadyAssociated"))
                {
                    await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
                    await _signInManager.SignInAsync(existingUser, isPersistent: false);
                    return RedirectToLocal(returnUrl);
                }
            }

            // Pre-fill names from Google claims and phone from existing profile
            var givenName = loginInfo.Principal.FindFirstValue(ClaimTypes.GivenName);
            var surname   = loginInfo.Principal.FindFirstValue(ClaimTypes.Surname);

            ViewBag.LoginProvider = loginInfo.LoginProvider;

            // Backup TempData in case external cookie expires before form submission
            TempData["ExtLoginProvider"]      = loginInfo.LoginProvider;
            TempData["ExtProviderKey"]         = loginInfo.ProviderKey;
            TempData["ExtProviderDisplayName"] = loginInfo.ProviderDisplayName;
            TempData["ExtReturnUrl"]           = returnUrl;

            return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel
            {
                Email               = email,
                PhoneNumber         = existingUser?.PhoneNumber,
                FirstName           = existingUser?.FirstName ?? givenName,
                LastName            = existingUser?.LastName  ?? surname,
                LoginProvider       = loginInfo.LoginProvider,
                ProviderKey         = loginInfo.ProviderKey,
                ProviderDisplayName = loginInfo.ProviderDisplayName,
                ReturnUrl           = returnUrl
            });
        }

        // Handles browser back / direct navigation to the confirmation URL
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ExternalLoginConfirmation()
        {
            return RedirectToAction("Login");
        }

        [HttpPost]
        [ActionName("ExternalLoginConfirmation")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLoginConfirmationPost(
            ExternalLoginConfirmationViewModel model)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Manage");

            if (!ModelState.IsValid)
                return View("ExternalLoginConfirmation", model);

            // Resolve provider info: prefer live external cookie, then model hidden fields, then TempData
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                var provider    = model.LoginProvider ?? TempData["ExtLoginProvider"] as string;
                var key         = model.ProviderKey   ?? TempData["ExtProviderKey"]   as string;
                var displayName = model.ProviderDisplayName ?? TempData["ExtProviderDisplayName"] as string;

                if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(key))
                {
                    TempData["GoogleError"] = "Your sign-in session expired. Please sign in with Google again.";
                    return RedirectToAction("Login");
                }

                info = new ExternalLoginInfo(
                    new ClaimsPrincipal(new ClaimsIdentity()),
                    provider, key, displayName ?? provider);
            }

            // Resolve returnUrl from model (hidden field) or TempData
            var returnUrl = model.ReturnUrl ?? TempData["ExtReturnUrl"] as string;

            // Find or create the user account
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName       = model.Email,
                    Email          = model.Email,
                    PhoneNumber    = model.PhoneNumber,
                    FirstName      = model.FirstName ?? string.Empty,
                    LastName       = model.LastName  ?? string.Empty,
                    EmailConfirmed = true   // Google has already verified this email address
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    AddErrors(createResult);
                    return View("ExternalLoginConfirmation", model);
                }

                await _userManager.AddToRoleAsync(user, "User");

                if (!string.IsNullOrEmpty(model.PhoneNumber))
                    await _userManager.SetPhoneNumberAsync(user, model.PhoneNumber);
            }
            else
            {
                bool needsUpdate = false;
                if (!string.IsNullOrWhiteSpace(model.PhoneNumber) && string.IsNullOrWhiteSpace(user.PhoneNumber))
                { user.PhoneNumber = model.PhoneNumber; needsUpdate = true; }
                if (!string.IsNullOrWhiteSpace(model.FirstName) && string.IsNullOrWhiteSpace(user.FirstName))
                { user.FirstName = model.FirstName; needsUpdate = true; }
                if (!string.IsNullOrWhiteSpace(model.LastName) && string.IsNullOrWhiteSpace(user.LastName))
                { user.LastName = model.LastName; needsUpdate = true; }
                if (needsUpdate)
                    await _userManager.UpdateAsync(user);
            }

            // Link the Google login to this account
            var addLoginResult = await _userManager.AddLoginAsync(user, info);
            if (!addLoginResult.Succeeded)
            {
                if (addLoginResult.Errors.All(e => e.Code == "LoginAlreadyAssociated"))
                {
                    // This Google account is already linked to a (possibly different) user —
                    // find the real owner and sign them in instead of the wrong account
                    var owner = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                    if (owner != null)
                    {
                        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
                        await _signInManager.SignInAsync(owner, isPersistent: false);
                        return RedirectToLocal(returnUrl);
                    }
                }

                AddErrors(addLoginResult);
                return View("ExternalLoginConfirmation", model);
            }

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToLocal(returnUrl);
        }

        // ── 2FA ──────────────────────────────────────────────────────────

        [AllowAnonymous]
        public async Task<IActionResult> SendCode(string returnUrl, bool rememberMe)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
                return View("Error");

            var userFactors   = await _userManager.GetValidTwoFactorProvidersAsync(user);
            var factorOptions = userFactors
                .Select(purpose => new SelectListItem { Text = purpose, Value = purpose })
                .ToList();

            return View(new SendCodeViewModel
            {
                Providers  = factorOptions,
                ReturnUrl  = returnUrl,
                RememberMe = rememberMe
            });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
                return View();

            var tfaUser = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (tfaUser == null)
                return View("Error");

            var code = await _userManager.GenerateTwoFactorTokenAsync(tfaUser, model.SelectedProvider);
            if (string.IsNullOrWhiteSpace(code))
                return View("Error");

            return RedirectToAction("VerifyCode", new
            {
                Provider   = model.SelectedProvider,
                ReturnUrl  = model.ReturnUrl,
                RememberMe = model.RememberMe
            });
        }

        [AllowAnonymous]
        public async Task<IActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
                return View("Error");

            return View(new VerifyCodeViewModel
            {
                Provider   = provider,
                ReturnUrl  = returnUrl,
                RememberMe = rememberMe
            });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.TwoFactorSignInAsync(
                model.Provider, model.Code, model.RememberMe, model.RememberBrowser);

            if (result.Succeeded)
                return RedirectToLocal(model.ReturnUrl);
            if (result.IsLockedOut)
                return View("Lockout");

            ModelState.AddModelError("", "Invalid code.");
            return View(model);
        }

        // ── Sign Out ─────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOff()
        {
            HttpContext.Session.Remove("ProfileComplete");
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // ── Admin ────────────────────────────────────────────────────────

        [Authorize(Roles = "Administrator")]
        public IActionResult AddUserToRole()
        {
            var model = new AddToRoleModel
            {
                Roles = new List<string> { "Administrator", "User", "Driver", "Receptionist" }
            };
            return View(model);
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUserToRole(AddToRoleModel model)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError("", "There is no user with the provided email.");
                    model.Roles = new List<string> { "Administrator", "User", "Driver", "Receptionist" };
                    return View(model);
                }

                await _userManager.AddToRoleAsync(user, model.SelectedRole);
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred: " + ex.Message);
                model.Roles = new List<string> { "Administrator", "User", "Driver", "Receptionist" };
                return View(model);
            }
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminConfirmEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["AdminError"] = "Email is required.";
                return RedirectToAction("AddUserToRole");
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                TempData["AdminError"] = $"No account found for {email}.";
                return RedirectToAction("AddUserToRole");
            }

            if (user.EmailConfirmed)
            {
                TempData["AdminInfo"] = $"{email} is already confirmed.";
                return RedirectToAction("AddUserToRole");
            }

            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
            TempData["AdminSuccess"] = $"Email confirmed for {email}. They can now log in.";
            return RedirectToAction("AddUserToRole");
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult ExternalLoginFailure()
        {
            return View();
        }

        // ── Helpers ──────────────────────────────────────────────────────

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }
    }
}
