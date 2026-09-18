using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TaxiBlitz.Domain.Identity;

namespace TaxiBlitz.Web.Filters
{
    public class ProfileCompleteFilter : IAsyncActionFilter
    {
        private const string SessionKey = "ProfileComplete";
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileCompleteFilter(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpContext = context.HttpContext;
            var user = httpContext.User;

            if (!user.Identity.IsAuthenticated)
            {
                await next();
                return;
            }

            var controllerName = (context.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor)
                                 ?.ControllerName ?? string.Empty;
            var actionName = (context.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor)
                             ?.ActionName ?? string.Empty;

            if (controllerName == "Account")
            {
                await next();
                return;
            }

            if (controllerName == "Manage" &&
                (actionName == "CompleteProfile" || actionName == "EditProfile" || actionName == "UploadProfilePicture"))
            {
                await next();
                return;
            }

            if (controllerName == "Home" && actionName == "Error")
            {
                await next();
                return;
            }

            if (httpContext.Session.GetString(SessionKey) == "true")
            {
                await next();
                return;
            }

            var userId = _userManager.GetUserId(user);
            var appUser = await _userManager.FindByIdAsync(userId);

            if (appUser != null &&
                !string.IsNullOrWhiteSpace(appUser.FirstName) &&
                !string.IsNullOrWhiteSpace(appUser.LastName) &&
                !string.IsNullOrWhiteSpace(appUser.PhoneNumber))
            {
                httpContext.Session.SetString(SessionKey, "true");
                await next();
                return;
            }

            bool isXhr = httpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            if (isXhr)
            {
                context.Result = new JsonResult(new { error = "Profile incomplete" }) { StatusCode = 403 };
                return;
            }

            var returnUrl = httpContext.Request.Path + httpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("CompleteProfile", "Manage", new { returnUrl });
        }
    }
}
