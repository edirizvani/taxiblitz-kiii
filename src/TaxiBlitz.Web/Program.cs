using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using TaxiBlitz.Application.Interfaces;
using TaxiBlitz.Application.Services;
using TaxiBlitz.Application.Services.Interfaces;
using TaxiBlitz.Domain.Identity;
using TaxiBlitz.Infrastructure.Email;
using TaxiBlitz.Infrastructure.Maps;
using TaxiBlitz.Infrastructure.Receipts;
using TaxiBlitz.Infrastructure.Seo;
using TaxiBlitz.Infrastructure.Weather;
using TaxiBlitz.Persistence;
using TaxiBlitz.Persistence.Repositories;
using TaxiBlitz.Web.Filters;

var builder = WebApplication.CreateBuilder(args);

// ── Response Compression ─────────────────────────────────────────
builder.Services.AddResponseCompression(opts => { opts.EnableForHttps = true; });

// ── MVC ──────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews(options =>
{
    // Skip the profile-complete filter in Testing environment (tested separately)
    if (!builder.Environment.IsEnvironment("Testing"))
        options.Filters.AddService<ProfileCompleteFilter>();
});

// ── EF Core ──────────────────────────────────────────────────────
// In "Testing" environment, the WebApplicationFactory registers InMemory instead.
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null)));
}

// ── Identity ─────────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength         = 6;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireDigit           = true;
    options.Password.RequireLowercase       = true;
    options.Password.RequireUppercase       = true;
    options.User.RequireUniqueEmail         = true;
    options.User.AllowedUserNameCharacters  = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.SignIn.RequireConfirmedAccount  = true;
    options.Lockout.MaxFailedAccessAttempts = 15;
    options.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(5);
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    if (builder.Environment.IsProduction())
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite     = SameSiteMode.Lax;
        options.Cookie.Name         = "__Host-TaxiBlitz";
    }
    else
    {
        // Development: SameAsRequest + Lax so the cookie works on plain HTTP localhost
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite     = SameSiteMode.Lax;
        options.Cookie.Name         = ".TaxiBlitz";
    }
    options.LoginPath        = "/Account/Login";
    options.LogoutPath       = "/Account/LogOff";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan   = TimeSpan.FromDays(14);
});

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.FromMinutes(5));

// ── Google OAuth (wires up only if keys are configured) ──────────
var googleClientId     = builder.Configuration["GoogleClientId"];
var googleClientSecret = builder.Configuration["GoogleClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId     = googleClientId;
            options.ClientSecret = googleClientSecret;

            // Fix "Correlation failed.": SameSite=Lax allows the correlation cookie on
            // top-level GET redirects from accounts.google.com (OAuth callbacks are exactly this).
            // Lax works on HTTP and HTTPS without requiring Secure, unlike SameSite=None.
            options.CorrelationCookie.SameSite    = SameSiteMode.Lax;
            options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.CorrelationCookie.HttpOnly     = true;
        });
}

// ── Data protection — persist keys so OAuth cookies survive restarts ─
// Azure App Service: /home is persistent across deployments; ContentRootPath resets on deploy
var dpKeysPath = builder.Environment.IsProduction()
    ? "/home/data/DataProtection-Keys"
    : Path.Combine(builder.Environment.ContentRootPath, "DataProtection-Keys");

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dpKeysPath))
    .SetApplicationName("TaxiBlitzOhrid");

// ── Session ───────────────────────────────────────────────────────
builder.Services.AddSession(options =>
{
    options.IdleTimeout        = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly    = true;
    options.Cookie.IsEssential = true;
});

// ── Rate limiting — protect auth endpoints from brute force ──────
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Per-IP partitioned so each client gets its own independent bucket.
    // A global (non-partitioned) limiter would block everyone after one IP exhausts the quota.
    options.AddPolicy("auth-login", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit          = 10,
                Window               = TimeSpan.FromMinutes(10),
                SegmentsPerWindow    = 5,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0
            }));

    options.AddPolicy("auth-register", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit          = 5,
                Window               = TimeSpan.FromMinutes(60),
                SegmentsPerWindow    = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0
            }));

    options.AddPolicy("auth-forgot", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit          = 5,
                Window               = TimeSpan.FromMinutes(60),
                SegmentsPerWindow    = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0
            }));

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        context.HttpContext.Response.Headers.RetryAfter = "60";
        context.HttpContext.Response.ContentType = "text/html; charset=utf-8";
        await context.HttpContext.Response.WriteAsync("""
            <!DOCTYPE html>
            <html lang="en">
            <head><meta charset="utf-8"><title>Too many attempts — TaxiBlitz Ohrid</title>
            <style>
              body{font-family:Arial,sans-serif;background:#f4f4f4;display:flex;align-items:center;
                   justify-content:center;min-height:100vh;margin:0;}
              .card{background:#fff;border-radius:8px;padding:48px 40px;max-width:420px;
                    text-align:center;box-shadow:0 2px 16px rgba(0,0,0,.08);}
              h2{color:#1a1a2e;margin-top:0;}
              p{color:#555;line-height:1.6;}
              a{display:inline-block;margin-top:24px;background:#e63946;color:#fff;
                padding:12px 28px;border-radius:6px;text-decoration:none;font-weight:bold;}
            </style></head>
            <body>
              <div class="card">
                <svg viewBox="0 0 24 24" fill="none" stroke="#e63946" stroke-width="1.5"
                     stroke-linecap="round" stroke-linejoin="round" style="width:52px;height:52px;margin-bottom:16px;">
                  <circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/>
                  <line x1="12" y1="16" x2="12.01" y2="16"/>
                </svg>
                <h2>Too many attempts</h2>
                <p>You have made too many requests in a short time.<br>
                   Please wait a few minutes before trying again.</p>
                <a href="/Account/Login">Back to login</a>
              </div>
            </body></html>
            """, token);
    };
});

// ── Application services ─────────────────────────────────────────
builder.Services.AddScoped<ProfileCompleteFilter>();
builder.Services.AddScoped<TourRouteService>();
builder.Services.AddHttpClient<LocationGeocodingService>();
builder.Services.AddScoped<LocationGeocodingService>();
builder.Services.AddSingleton<IReceiptService, ReceiptService>();
builder.Services.AddHttpClient<WeatherService>();
builder.Services.AddScoped<IWeatherService, WeatherService>();

// Repositories
builder.Services.AddScoped<ITourRepository,      TourRepository>();
builder.Services.AddScoped<IDriverRepository,    DriverRepository>();
builder.Services.AddScoped<IBookingRepository,   BookingRepository>();
builder.Services.AddScoped<IReviewRepository,    ReviewRepository>();
builder.Services.AddScoped<ITourPostRepository,  TourPostRepository>();
builder.Services.AddScoped<IFavouriteRepository, FavouriteRepository>();
builder.Services.AddScoped<IReferralRepository,  ReferralRepository>();

// Services
builder.Services.AddScoped<IEmailService,      EmailNotificationService>();
builder.Services.AddScoped<IAuthEmailService,  AuthEmailService>();
builder.Services.AddScoped<ITourService,        TourService>();
builder.Services.AddScoped<IDriverService,      DriverService>();
builder.Services.AddScoped<IBookingService,     BookingService>();
builder.Services.AddScoped<IReviewService,      ReviewService>();
builder.Services.AddScoped<ITourStoryService,   TourStoryService>();
builder.Services.AddScoped<IFavouriteService,   FavouriteService>();
builder.Services.AddScoped<IReferralService,    ReferralService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (!app.Environment.IsEnvironment("Testing"))
    app.UseHttpsRedirection();

app.UseResponseCompression();

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    await next();
});

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=604800");
    }
});
app.UseRouting();
app.UseSession();
app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");
// Ensure correlation cookies (SameSite=Lax) are not downgraded by the policy
app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.Lax
});
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Apply pending EF Core migrations and seed roles on startup
using (var scope = app.Services.CreateScope())
{
    var db          = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    try
    {
        var providerName = db.Database.ProviderName ?? "";
        if (providerName.Contains("InMemory") || providerName.Contains("Sqlite"))
            db.Database.EnsureCreated();
        else
            db.Database.Migrate();
    }
    catch (InvalidOperationException)
    {
        // In Testing environment, InMemory provider — ensure schema is created
        db.Database.EnsureCreated();
    }
    foreach (var role in new[] { "Administrator", "User", "Driver", "Receptionist" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
    // Ensure admin account exists, is confirmed, has a known password, and has the Administrator role
    try
    {
        var userManager  = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var adminEmail   = app.Configuration["AdminEmail"] ?? "admin@taxiblitz.local";
        var seedPassword = app.Configuration["AdminSeedPassword"];
        var adminUser    = await userManager.FindByEmailAsync(adminEmail);

        // Create admin account if it doesn't exist and AdminSeedPassword is configured
        if (adminUser == null && !string.IsNullOrWhiteSpace(seedPassword))
        {
            adminUser = new ApplicationUser
            {
                UserName       = adminEmail,
                Email          = adminEmail,
                EmailConfirmed = true
            };
            var createResult = await userManager.CreateAsync(adminUser, seedPassword);
            if (createResult.Succeeded)
            {
                Console.WriteLine($"Admin account created for {adminEmail}");
                adminUser = await userManager.FindByEmailAsync(adminEmail);
            }
            else
            {
                Console.WriteLine($"Admin creation failed: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }
        }

        if (adminUser != null)
        {
            if (!adminUser.EmailConfirmed)
            {
                adminUser.EmailConfirmed = true;
                await userManager.UpdateAsync(adminUser);
                Console.WriteLine($"Confirmed email for {adminEmail}");
            }

            // Sync password to AdminSeedPassword — if password doesn't match, reset it.
            // In production: set AdminSeedPassword as an environment variable, not in appsettings.json.
            if (!string.IsNullOrWhiteSpace(seedPassword))
            {
                bool matches = await userManager.CheckPasswordAsync(adminUser, seedPassword);
                if (!matches)
                {
                    var tok = await userManager.GeneratePasswordResetTokenAsync(adminUser);
                    var res = await userManager.ResetPasswordAsync(adminUser, tok, seedPassword);
                    Console.WriteLine(res.Succeeded
                        ? "Admin password set from AdminSeedPassword config"
                        : $"Admin password set failed: {string.Join(", ", res.Errors.Select(e => e.Description))}");
                }
            }

            if (!await userManager.IsInRoleAsync(adminUser, "Administrator"))
            {
                await userManager.AddToRoleAsync(adminUser, "Administrator");
                Console.WriteLine($"Granted Administrator role to {adminEmail}");
            }
            else
            {
                Console.WriteLine($"User {adminEmail} is already an Administrator");
            }

        }
        else
        {
            Console.WriteLine($"Admin {adminEmail} not found — set AdminSeedPassword in config to auto-create.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Admin seeder error: " + ex.Message);
    }
}

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program { }
