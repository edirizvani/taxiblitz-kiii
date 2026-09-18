using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Moq;
using TaxiBlitz.Application.Interfaces;
using TaxiBlitz.Application.ViewModels;
using TaxiBlitz.Domain.Identity;
using TaxiBlitz.Tests.Helpers;
using TaxiBlitz.Web.Controllers;

// Disambiguate between Microsoft.AspNetCore.Identity.SignInResult and Microsoft.AspNetCore.Mvc.SignInResult
using IdentitySignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace TaxiBlitz.Tests.Controllers;

public class AccountControllerTests
{
    // ── helpers ──────────────────────────────────────────────────────────

    private static (AccountController ctrl,
                    Mock<UserManager<ApplicationUser>> um,
                    Mock<SignInManager<ApplicationUser>> sm,
                    Mock<IAuthEmailService> email)
        Build()
    {
        var um    = MockHelpers.CreateMockUserManager();
        var sm    = MockHelpers.CreateMockSignInManager(um);
        var email = new Mock<IAuthEmailService>();
        var ctrl  = MockHelpers.CreateController(um, sm, email);
        return (ctrl, um, sm, email);
    }

    private static ApplicationUser FakeUser(string id = "u1", string emailAddr = "user@test.com") =>
        new ApplicationUser { Id = id, Email = emailAddr, UserName = emailAddr };

    // ── Login ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_Post_ValidCredentials_RedirectsToHome()
    {
        var (ctrl, um, sm, _) = Build();

        um.Setup(u => u.FindByEmailAsync("user@test.com"))
            .ReturnsAsync(FakeUser());
        um.Setup(u => u.IsEmailConfirmedAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(true);
        sm.Setup(s => s.PasswordSignInAsync("user@test.com", "Pass1!", true, true))
            .ReturnsAsync(IdentitySignInResult.Success);

        var result = await ctrl.Login(
            new LoginViewModel { Email = "user@test.com", Password = "Pass1!", RememberMe = true });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home",  redirect.ControllerName);
    }

    [Fact]
    public async Task Login_Post_InvalidCredentials_ShowsError()
    {
        var (ctrl, um, sm, _) = Build();

        um.Setup(u => u.FindByEmailAsync("user@test.com"))
            .ReturnsAsync(FakeUser());
        um.Setup(u => u.IsEmailConfirmedAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(true);
        sm.Setup(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), true))
            .ReturnsAsync(IdentitySignInResult.Failed);

        var result = await ctrl.Login(
            new LoginViewModel { Email = "user@test.com", Password = "wrong" });

        var view = Assert.IsType<ViewResult>(result);
        Assert.False(ctrl.ModelState.IsValid);
    }

    [Fact]
    public async Task Login_Post_LockedOut_ReturnsLockoutView()
    {
        var (ctrl, um, sm, _) = Build();

        um.Setup(u => u.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(FakeUser());
        um.Setup(u => u.IsEmailConfirmedAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(true);
        sm.Setup(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), true))
            .ReturnsAsync(IdentitySignInResult.LockedOut);

        var result = await ctrl.Login(
            new LoginViewModel { Email = "user@test.com", Password = "pass" });

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Lockout", view.ViewName);
    }

    [Fact]
    public async Task Login_Post_UnconfirmedEmail_ShowsEmailConfirmError()
    {
        var (ctrl, um, _, _) = Build();

        um.Setup(u => u.FindByEmailAsync("user@test.com")).ReturnsAsync(FakeUser());
        um.Setup(u => u.IsEmailConfirmedAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(false);

        var result = await ctrl.Login(
            new LoginViewModel { Email = "user@test.com", Password = "pass" });

        var view = Assert.IsType<ViewResult>(result);
        Assert.False(ctrl.ModelState.IsValid);
        Assert.Contains(ctrl.ModelState.Values,
            v => v.Errors.Any(e => e.ErrorMessage.Contains("confirm your email")));
    }

    [Fact]
    public async Task Login_Post_RequiresTwoFactor_RedirectsToSendCode()
    {
        var (ctrl, um, sm, _) = Build();

        um.Setup(u => u.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(FakeUser());
        um.Setup(u => u.IsEmailConfirmedAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(true);
        sm.Setup(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), true))
            .ReturnsAsync(IdentitySignInResult.TwoFactorRequired);

        var result = await ctrl.Login(
            new LoginViewModel { Email = "user@test.com", Password = "pass" });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("SendCode", redirect.ActionName);
    }

    // ── Register ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_Post_Valid_SendsConfirmationEmailAndRedirects()
    {
        var (ctrl, um, _, email) = Build();
        var user = FakeUser();

        um.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), "ValidPass1!"))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, string>((u, _) => u.Id = "new-id");
        um.Setup(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"))
            .ReturnsAsync(IdentityResult.Success);
        um.Setup(u => u.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync("raw-token");
        email.Setup(e => e.SendEmailConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await ctrl.Register(new RegisterViewModel
        {
            FirstName = "John", LastName = "Doe",
            Email = "john@test.com", PhoneNumber = "+389 70 000 000",
            Password = "ValidPass1!", ConfirmPassword = "ValidPass1!"
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("RegisterConfirmation", redirect.ActionName);
        email.Verify(e => e.SendEmailConfirmationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Register_Post_InvalidModel_ReturnsView()
    {
        var (ctrl, _, _, _) = Build();
        ctrl.ModelState.AddModelError("Email", "Required");

        var result = await ctrl.Register(new RegisterViewModel());

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Register_Post_IdentityFailure_ShowsError()
    {
        var (ctrl, um, _, _) = Build();

        um.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "DuplicateEmail", Description = "Email already taken." }));

        var result = await ctrl.Register(new RegisterViewModel
        {
            FirstName = "A", LastName = "B", Email = "dup@test.com",
            PhoneNumber = "+1 555", Password = "Pass1!", ConfirmPassword = "Pass1!"
        });

        var view = Assert.IsType<ViewResult>(result);
        Assert.False(ctrl.ModelState.IsValid);
    }

    // ── ConfirmEmail ──────────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmEmail_ValidToken_ReturnsConfirmEmailView()
    {
        var (ctrl, um, _, _) = Build();
        var user = FakeUser();
        var rawToken    = "test-token";
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));

        um.Setup(u => u.FindByIdAsync("u1")).ReturnsAsync(user);
        um.Setup(u => u.ConfirmEmailAsync(user, rawToken))
            .ReturnsAsync(IdentityResult.Success);

        var result = await ctrl.ConfirmEmail("u1", encodedToken);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("ConfirmEmail", view.ViewName);
    }

    [Fact]
    public async Task ConfirmEmail_InvalidToken_ReturnsErrorView()
    {
        var (ctrl, um, _, _) = Build();
        var user = FakeUser();
        var rawToken     = "bad-token";
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));

        um.Setup(u => u.FindByIdAsync("u1")).ReturnsAsync(user);
        um.Setup(u => u.ConfirmEmailAsync(user, rawToken))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token" }));

        var result = await ctrl.ConfirmEmail("u1", encodedToken);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Error", view.ViewName);
    }

    [Fact]
    public async Task ConfirmEmail_NullParams_ReturnsErrorView()
    {
        var (ctrl, _, _, _) = Build();

        var result = await ctrl.ConfirmEmail(null, null);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Error", view.ViewName);
    }

    // ── ForgotPassword ────────────────────────────────────────────────────

    [Fact]
    public async Task ForgotPassword_Post_ValidEmail_SendsResetEmail()
    {
        var (ctrl, um, _, email) = Build();
        var user = FakeUser();

        um.Setup(u => u.FindByEmailAsync("user@test.com")).ReturnsAsync(user);
        um.Setup(u => u.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
        um.Setup(u => u.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");
        email.Setup(e => e.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await ctrl.ForgotPassword(new ForgotPasswordViewModel { Email = "user@test.com" });

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("ForgotPasswordConfirmation", view.ViewName);
        email.Verify(e => e.SendPasswordResetAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_Post_UnknownEmail_ShowsConfirmationWithoutSendingEmail()
    {
        var (ctrl, um, _, email) = Build();

        um.Setup(u => u.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await ctrl.ForgotPassword(new ForgotPasswordViewModel { Email = "nobody@test.com" });

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("ForgotPasswordConfirmation", view.ViewName);
        email.Verify(e => e.SendPasswordResetAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ForgotPassword_Post_UnconfirmedEmail_ResendConfirmationEmailAndRedirectsToLogin()
    {
        var (ctrl, um, _, email) = Build();
        var user = FakeUser();

        um.Setup(u => u.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        um.Setup(u => u.IsEmailConfirmedAsync(user)).ReturnsAsync(false);
        um.Setup(u => u.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync("confirm-token");
        email.Setup(e => e.SendEmailConfirmationAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await ctrl.ForgotPassword(new ForgotPasswordViewModel { Email = "user@test.com" });

        // Must redirect to Login (not return the ForgotPasswordConfirmation view)
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirect.ActionName);

        // Confirmation email must be re-sent once; password-reset email must NOT be sent
        email.Verify(e => e.SendEmailConfirmationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        email.Verify(e => e.SendPasswordResetAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ── ResetPassword ─────────────────────────────────────────────────────

    [Fact]
    public async Task ResetPassword_Get_NullCode_ReturnsErrorView()
    {
        var (ctrl, _, _, _) = Build();

        var result = await ctrl.ResetPassword(null, null);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Error", view.ViewName);
    }

    [Fact]
    public async Task ResetPassword_Post_ValidToken_RedirectsToConfirmation()
    {
        var (ctrl, um, _, _) = Build();
        var user = FakeUser();
        var rawToken     = "valid-reset-token";
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));

        um.Setup(u => u.FindByIdAsync("u1")).ReturnsAsync(user);
        um.Setup(u => u.ResetPasswordAsync(user, rawToken, "NewPass1!"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await ctrl.ResetPassword(new ResetPasswordViewModel
        {
            UserId          = "u1",
            Code            = encodedToken,
            Password        = "NewPass1!",
            ConfirmPassword = "NewPass1!"
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ResetPasswordConfirmation", redirect.ActionName);
    }

    [Fact]
    public async Task ResetPassword_Post_InvalidToken_ShowsError()
    {
        var (ctrl, um, _, _) = Build();
        var user = FakeUser();
        var rawToken     = "bad-token";
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));

        um.Setup(u => u.FindByIdAsync("u1")).ReturnsAsync(user);
        um.Setup(u => u.ResetPasswordAsync(user, rawToken, It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token" }));

        var result = await ctrl.ResetPassword(new ResetPasswordViewModel
        {
            UserId = "u1", Code = encodedToken,
            Password = "NewPass1!", ConfirmPassword = "NewPass1!"
        });

        var view = Assert.IsType<ViewResult>(result);
        Assert.False(ctrl.ModelState.IsValid);
    }

    // ── ExternalLogin ─────────────────────────────────────────────────────

    [Fact]
    public async Task ExternalLogin_Post_ProviderUnavailable_RedirectsWithError()
    {
        var (ctrl, _, sm, _) = Build();

        sm.Setup(s => s.GetExternalAuthenticationSchemesAsync())
            .ReturnsAsync(new List<AuthenticationScheme>());  // no providers

        var result = await ctrl.ExternalLogin("Google", null);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirect.ActionName);
        Assert.NotNull(ctrl.TempData["GoogleError"]);
    }

    [Fact]
    public async Task ExternalLogin_Post_ValidProvider_ReturnsChallenge()
    {
        var (ctrl, _, sm, _) = Build();

        var googleScheme = new AuthenticationScheme("Google", "Google", typeof(IAuthenticationHandler));
        sm.Setup(s => s.GetExternalAuthenticationSchemesAsync())
            .ReturnsAsync(new List<AuthenticationScheme> { googleScheme });
        sm.Setup(s => s.ConfigureExternalAuthenticationProperties("Google", It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new AuthenticationProperties());

        var result = await ctrl.ExternalLogin("Google", null);

        Assert.IsType<ChallengeResult>(result);
    }

    // ── ExternalLoginCallback ─────────────────────────────────────────────

    [Fact]
    public async Task ExternalLoginCallback_NullLoginInfo_RedirectsToLoginWithError()
    {
        var (ctrl, _, sm, _) = Build();

        sm.Setup(s => s.GetExternalLoginInfoAsync(It.IsAny<string>()))
            .ReturnsAsync((ExternalLoginInfo?)null);

        var result = await ctrl.ExternalLoginCallback();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirect.ActionName);
        Assert.NotNull(ctrl.TempData["GoogleError"]);
    }

    [Fact]
    public async Task ExternalLoginCallback_ExistingUser_SignsInAndRedirects()
    {
        var (ctrl, _, sm, _) = Build();

        var info = new ExternalLoginInfo(
            new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Email, "user@test.com")
            })),
            "Google", "google-key-123", "Google");

        sm.Setup(s => s.GetExternalLoginInfoAsync(It.IsAny<string>())).ReturnsAsync(info);
        sm.Setup(s => s.ExternalLoginSignInAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(IdentitySignInResult.Success);

        // Mock HttpContext.SignOutAsync
        var authServiceMock = new Mock<IAuthenticationService>();
        authServiceMock.Setup(a => a.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);
        ctrl.HttpContext.RequestServices = new MockServiceProvider(authServiceMock.Object);

        var result = await ctrl.ExternalLoginCallback();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home",  redirect.ControllerName);
    }

    [Fact]
    public async Task ExternalLoginCallback_NewUser_ShowsConfirmationView()
    {
        var (ctrl, _, sm, _) = Build();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email,      "new@test.com"),
            new Claim(ClaimTypes.GivenName,  "New"),
            new Claim(ClaimTypes.Surname,    "User")
        };
        var info = new ExternalLoginInfo(
            new ClaimsPrincipal(new ClaimsIdentity(claims)),
            "Google", "google-key-new", "Google");

        sm.Setup(s => s.GetExternalLoginInfoAsync(It.IsAny<string>())).ReturnsAsync(info);
        sm.Setup(s => s.ExternalLoginSignInAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(IdentitySignInResult.Failed);

        var result = await ctrl.ExternalLoginCallback();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("ExternalLoginConfirmation", view.ViewName);
        var model = Assert.IsType<ExternalLoginConfirmationViewModel>(view.Model);
        Assert.Equal("new@test.com", model.Email);
        Assert.Equal("Google",       model.LoginProvider);
        Assert.Equal("google-key-new", model.ProviderKey);
    }

    [Fact]
    public async Task ExternalLoginCallback_LockedOut_ReturnsLockoutView()
    {
        var (ctrl, _, sm, _) = Build();

        var info = new ExternalLoginInfo(
            new ClaimsPrincipal(new ClaimsIdentity()),
            "Google", "key", "Google");

        sm.Setup(s => s.GetExternalLoginInfoAsync(It.IsAny<string>())).ReturnsAsync(info);
        sm.Setup(s => s.ExternalLoginSignInAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(IdentitySignInResult.LockedOut);

        var result = await ctrl.ExternalLoginCallback();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Lockout", view.ViewName);
    }

    // ── ExternalLoginConfirmation POST ─────────────────────────────────────

    [Fact]
    public async Task ExternalLoginConfirmation_Post_NewUser_CreatesUserLinksLoginSignsIn()
    {
        var (ctrl, um, sm, _) = Build();

        // No external cookie (uses model hidden fields fallback)
        sm.Setup(s => s.GetExternalLoginInfoAsync(It.IsAny<string>()))
            .ReturnsAsync((ExternalLoginInfo?)null);

        um.Setup(u => u.FindByEmailAsync("new@test.com"))
            .ReturnsAsync((ApplicationUser?)null);
        um.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);
        um.Setup(u => u.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"))
            .ReturnsAsync(IdentityResult.Success);
        um.Setup(u => u.AddLoginAsync(It.IsAny<ApplicationUser>(), It.IsAny<UserLoginInfo>()))
            .ReturnsAsync(IdentityResult.Success);

        var authSvc = new Mock<IAuthenticationService>();
        authSvc.Setup(a => a.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);
        sm.Setup(s => s.SignInAsync(It.IsAny<ApplicationUser>(), false, It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        ctrl.HttpContext.RequestServices = new MockServiceProvider(authSvc.Object);

        var result = await ctrl.ExternalLoginConfirmationPost(new ExternalLoginConfirmationViewModel
        {
            Email               = "new@test.com",
            LoginProvider       = "Google",
            ProviderKey         = "google-key-999",
            ProviderDisplayName = "Google",
            ReturnUrl           = null
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        um.Verify(u => u.CreateAsync(It.IsAny<ApplicationUser>()), Times.Once);
        um.Verify(u => u.AddLoginAsync(It.IsAny<ApplicationUser>(), It.IsAny<UserLoginInfo>()), Times.Once);
    }

    [Fact]
    public async Task ExternalLoginConfirmation_Post_ExistingUser_LinksLoginAndSignsIn()
    {
        var (ctrl, um, sm, _) = Build();
        var existingUser = FakeUser("existing-1", "existing@test.com");

        sm.Setup(s => s.GetExternalLoginInfoAsync(It.IsAny<string>()))
            .ReturnsAsync((ExternalLoginInfo?)null);

        um.Setup(u => u.FindByEmailAsync("existing@test.com")).ReturnsAsync(existingUser);
        um.Setup(u => u.AddLoginAsync(existingUser, It.IsAny<UserLoginInfo>()))
            .ReturnsAsync(IdentityResult.Success);

        var authSvc = new Mock<IAuthenticationService>();
        authSvc.Setup(a => a.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);
        sm.Setup(s => s.SignInAsync(existingUser, false, It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        ctrl.HttpContext.RequestServices = new MockServiceProvider(authSvc.Object);

        var result = await ctrl.ExternalLoginConfirmationPost(new ExternalLoginConfirmationViewModel
        {
            Email = "existing@test.com", LoginProvider = "Google",
            ProviderKey = "g-key-existing", ProviderDisplayName = "Google"
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        um.Verify(u => u.AddLoginAsync(existingUser, It.IsAny<UserLoginInfo>()), Times.Once);
    }

    [Fact]
    public async Task ExternalLoginConfirmation_Post_LoginAlreadyAssociated_SignsInRealOwner()
    {
        var (ctrl, um, sm, _) = Build();
        var targetUser   = FakeUser("target-user", "target@test.com");
        var realOwner    = FakeUser("real-owner",  "realowner@test.com");

        sm.Setup(s => s.GetExternalLoginInfoAsync(It.IsAny<string>()))
            .ReturnsAsync((ExternalLoginInfo?)null);

        um.Setup(u => u.FindByEmailAsync("target@test.com")).ReturnsAsync(targetUser);
        um.Setup(u => u.AddLoginAsync(targetUser, It.IsAny<UserLoginInfo>()))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Code = "LoginAlreadyAssociated", Description = "Login already linked." }));
        um.Setup(u => u.FindByLoginAsync("Google", "shared-google-key"))
            .ReturnsAsync(realOwner);

        var authSvc = new Mock<IAuthenticationService>();
        authSvc.Setup(a => a.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);
        sm.Setup(s => s.SignInAsync(realOwner, false, It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        ctrl.HttpContext.RequestServices = new MockServiceProvider(authSvc.Object);

        var result = await ctrl.ExternalLoginConfirmationPost(new ExternalLoginConfirmationViewModel
        {
            Email = "target@test.com", LoginProvider = "Google",
            ProviderKey = "shared-google-key", ProviderDisplayName = "Google"
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        // Must sign in the REAL owner, not targetUser
        sm.Verify(s => s.SignInAsync(realOwner, false, It.IsAny<string>()), Times.Once);
        sm.Verify(s => s.SignInAsync(targetUser, false, It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExternalLoginConfirmation_Post_NullProviderKey_RedirectsToLoginWithError()
    {
        var (ctrl, _, sm, _) = Build();

        sm.Setup(s => s.GetExternalLoginInfoAsync(It.IsAny<string>()))
            .ReturnsAsync((ExternalLoginInfo?)null);

        // Model has no provider info — session fully expired
        var result = await ctrl.ExternalLoginConfirmationPost(new ExternalLoginConfirmationViewModel
        {
            Email             = "user@test.com",
            LoginProvider     = null,
            ProviderKey       = null
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirect.ActionName);
        Assert.NotNull(ctrl.TempData["GoogleError"]);
    }
}

// ── IAuthenticationHandler stub ───────────────────────────────────────────
// Needed to instantiate an AuthenticationScheme in tests.
file sealed class StubAuthHandler : IAuthenticationHandler
{
    public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context) => Task.CompletedTask;
    public Task<AuthenticateResult> AuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult());
    public Task ChallengeAsync(AuthenticationProperties? properties) => Task.CompletedTask;
    public Task ForbidAsync(AuthenticationProperties? properties) => Task.CompletedTask;
}

// ── Minimal IServiceProvider for HttpContext.RequestServices ──────────────
file sealed class MockServiceProvider : IServiceProvider
{
    private readonly IAuthenticationService _authSvc;
    public MockServiceProvider(IAuthenticationService authSvc) => _authSvc = authSvc;
    public object? GetService(Type serviceType) =>
        serviceType == typeof(IAuthenticationService) ? _authSvc : null;
}
