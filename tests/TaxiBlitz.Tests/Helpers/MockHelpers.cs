using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TaxiBlitz.Application.Interfaces;
using TaxiBlitz.Domain.Identity;
using TaxiBlitz.Web.Controllers;

namespace TaxiBlitz.Tests.Helpers;

/// <summary>
/// Factory helpers for mocking ASP.NET Core Identity and MVC types in unit tests.
/// </summary>
public static class MockHelpers
{
    public static Mock<UserManager<ApplicationUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        // Implement all the store interfaces that UserManager uses
        store.As<IUserEmailStore<ApplicationUser>>();
        store.As<IUserPasswordStore<ApplicationUser>>();
        store.As<IUserRoleStore<ApplicationUser>>();
        store.As<IUserLoginStore<ApplicationUser>>();
        store.As<IUserSecurityStampStore<ApplicationUser>>();
        store.As<IUserTwoFactorStore<ApplicationUser>>();
        store.As<IUserLockoutStore<ApplicationUser>>();
        store.As<IUserPhoneNumberStore<ApplicationUser>>();

        var um = new Mock<UserManager<ApplicationUser>>(
            store.Object,
            /* optionsAccessor */   null,
            /* passwordHasher */    null,
            /* userValidators */    new List<IUserValidator<ApplicationUser>>(),
            /* passwordValidators */new List<IPasswordValidator<ApplicationUser>>(),
            /* keyNormalizer */     null,
            /* errors */            null,
            /* services */          null,
            /* logger */            Mock.Of<ILogger<UserManager<ApplicationUser>>>());

        return um;
    }

    public static Mock<SignInManager<ApplicationUser>> CreateMockSignInManager(
        Mock<UserManager<ApplicationUser>> userManager)
    {
        var contextAccessor = new Mock<IHttpContextAccessor>();
        contextAccessor.Setup(a => a.HttpContext).Returns(new DefaultHttpContext());

        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        claimsFactory.Setup(f => f.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new ClaimsPrincipal(new ClaimsIdentity()));

        var identityOptions = new IdentityOptions();
        var optionsAccessor = new Mock<IOptions<IdentityOptions>>();
        optionsAccessor.Setup(o => o.Value).Returns(identityOptions);

        var sm = new Mock<SignInManager<ApplicationUser>>(
            userManager.Object,
            contextAccessor.Object,
            claimsFactory.Object,
            optionsAccessor.Object,
            /* logger */     Mock.Of<ILogger<SignInManager<ApplicationUser>>>(),
            /* schemes */    Mock.Of<IAuthenticationSchemeProvider>(),
            /* confirmation*/Mock.Of<IUserConfirmation<ApplicationUser>>());

        return sm;
    }

    public static AccountController CreateController(
        Mock<UserManager<ApplicationUser>> userManager,
        Mock<SignInManager<ApplicationUser>> signInManager,
        Mock<IAuthEmailService> authEmailService)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host   = new HostString("localhost");

        // Provide a session so TempData works (cookie-based by default in unit tests may not; use ITempDataProvider)
        httpContext.Session = new TestSession();

        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(u => u.Action(It.IsAny<UrlActionContext>()))
            .Returns("https://localhost/test-action");
        urlHelper.Setup(u => u.IsLocalUrl(It.IsAny<string>()))
            .Returns<string>(url => !string.IsNullOrEmpty(url) && url.StartsWith('/'));

        var controller = new AccountController(
            userManager.Object,
            signInManager.Object,
            authEmailService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            },
            Url = urlHelper.Object
        };

        // Wire up TempData with an in-memory provider
        controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            httpContext,
            Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

        return controller;
    }

    // ── Additional helpers for controller / filter tests ─────────────

    public static ControllerContext CreateControllerContext(ClaimsPrincipal? user = null)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Session = new TestSession();
        if (user != null)
            httpContext.User = user;
        return new ControllerContext { HttpContext = httpContext };
    }

    public static DefaultHttpContext CreateAjaxHttpContext(ClaimsPrincipal? user = null)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["X-Requested-With"] = "XMLHttpRequest";
        ctx.Session = new TestSession();
        if (user != null)
            ctx.User = user;
        return ctx;
    }

    public static ClaimsPrincipal CreateAdminUser(string userId = "admin1", string email = "admin@test.com")
        => CreateUserPrincipal(userId, email, "Administrator");

    public static ClaimsPrincipal CreateRegularUser(string userId = "user1", string email = "user@test.com")
        => CreateUserPrincipal(userId, email, "User");

    public static ClaimsPrincipal CreateDriverUser(string userId = "driver1", string email = "driver@test.com")
        => CreateUserPrincipal(userId, email, "Driver");

    private static ClaimsPrincipal CreateUserPrincipal(string userId, string email, string role)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, email),
            new Claim(ClaimTypes.Role, role)
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    // Minimal ISession implementation for unit tests
    public sealed class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();
        public bool IsAvailable => true;
        public string Id        => "test-session";
        public IEnumerable<string> Keys => _store.Keys;
        public void Clear()    => _store.Clear();
        public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken ct = default)   => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, out byte[] value)   => _store.TryGetValue(key, out value!);
    }
}
