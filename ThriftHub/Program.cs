using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using AspNet.Security.OAuth.Apple;
using ThriftHub.Data;
using Microsoft.AspNetCore.SignalR;
using ThriftHub.Hubs;
using ThriftHub.Models;
using ThriftHub.Services;

var builder = WebApplication.CreateBuilder(args);


static string MapExternalLoginFailureCode(
    Exception? failure)
{
    var message =
        failure?.Message
        ?? string.Empty;

    if (
        message.Contains(
            "Correlation",
            StringComparison.OrdinalIgnoreCase) ||
        message.Contains(
            "state was missing or invalid",
            StringComparison.OrdinalIgnoreCase))
    {
        return "correlation";
    }

    if (
        message.Contains(
            "token endpoint",
            StringComparison.OrdinalIgnoreCase) ||
        message.Contains(
            "invalid_client",
            StringComparison.OrdinalIgnoreCase) ||
        message.Contains(
            "Unauthorized",
            StringComparison.OrdinalIgnoreCase))
    {
        return "invalid_client";
    }

    if (
        message.Contains(
            "redirect_uri",
            StringComparison.OrdinalIgnoreCase))
    {
        return "redirect_uri";
    }

    return "signin_failed";
}


static void ConfigureRemoteAuthOptions(
    RemoteAuthenticationOptions options)
{
    options.SignInScheme =
        IdentityConstants.ExternalScheme;

    options.SaveTokens = true;

    options.CorrelationCookie.SameSite =
        SameSiteMode.Lax;

    options.CorrelationCookie.SecurePolicy =
        CookieSecurePolicy.Always;

    options.CorrelationCookie.IsEssential =
        true;

    options.CorrelationCookie.Path =
        "/";

    options.Events.OnRemoteFailure =
        context =>
        {
            var logger =
                context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();

            var failureCode =
                MapExternalLoginFailureCode(
                    context.Failure);

            logger.LogWarning(
                context.Failure,
                "External login remote failure for {CallbackPath}. Code={FailureCode}.",
                options.CallbackPath,
                failureCode);

            context.Response.Redirect(
                $"/Account/ExternalLoginCallback?remoteError={failureCode}");

            context.HandleResponse();

            return Task.CompletedTask;
        };
}


// ============================================================
// PERSISTENT STORAGE (RENDER DISK)
// ============================================================

var connectionString =
    DatabasePersistenceService.BuildConnectionString(
        builder.Configuration,
        builder.Environment);

var dataProtectionPath =
    DatabasePersistenceService.GetDataProtectionPath(
        builder.Configuration,
        builder.Environment);

Directory.CreateDirectory(dataProtectionPath);


// ============================================================
// DATABASE
// ============================================================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(connectionString)
           .ConfigureWarnings(warnings => warnings.Ignore(
               Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.NonTransactionalMigrationOperationWarning));
});


// ============================================================
// IDENTITY
// ============================================================

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // --------------------------------------------------------
        // PASSWORD SETTINGS
        // --------------------------------------------------------

        options.Password.RequireDigit = true;

        options.Password.RequireLowercase = true;

        options.Password.RequireUppercase = true;

        options.Password.RequireNonAlphanumeric = false;

        options.Password.RequiredLength = 6;


        // --------------------------------------------------------
        // USER SETTINGS
        // --------------------------------------------------------

        options.User.RequireUniqueEmail = true;


        // --------------------------------------------------------
        // SIGN-IN SETTINGS
        // --------------------------------------------------------

        options.SignIn.RequireConfirmedEmail = false;

        options.SignIn.RequireConfirmedAccount = false;


        // --------------------------------------------------------
        // LOCKOUT SETTINGS
        // --------------------------------------------------------

        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(10);

        options.Lockout.MaxFailedAccessAttempts = 5;

        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();


// ============================================================
// GOOGLE / APPLE SIGN-IN
// ============================================================

var externalAuth =
    builder.Services.AddAuthentication();

var appleClientId =
    builder.Configuration["Authentication:Apple:ClientId"];

var appleTeamId =
    builder.Configuration["Authentication:Apple:TeamId"];

var appleKeyId =
    builder.Configuration["Authentication:Apple:KeyId"];

var applePrivateKey =
    builder.Configuration["Authentication:Apple:PrivateKey"];

if (
    !string.IsNullOrWhiteSpace(appleClientId) &&
    !string.IsNullOrWhiteSpace(appleTeamId) &&
    !string.IsNullOrWhiteSpace(appleKeyId) &&
    !string.IsNullOrWhiteSpace(applePrivateKey))
{
    externalAuth.AddApple(
        AppleAuthenticationDefaults.AuthenticationScheme,
        options =>
        {
            options.ClientId =
                appleClientId;

            options.TeamId =
                appleTeamId;

            options.KeyId =
                appleKeyId;

            options.PrivateKey =
                (_, _) =>
                    Task.FromResult<ReadOnlyMemory<char>>(
                        applePrivateKey
                            .Replace("\\n", "\n")
                            .AsMemory());

            ConfigureRemoteAuthOptions(options);
        });
}


builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(
        new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("ThriftHub");


// ============================================================
// COOKIE SETTINGS
// ============================================================

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";

    options.AccessDeniedPath = "/Account/AccessDenied";

    options.ExpireTimeSpan =
        TimeSpan.FromDays(14);

    options.SlidingExpiration = true;

    options.Cookie.SameSite =
        SameSiteMode.Lax;

    options.Cookie.SecurePolicy =
        CookieSecurePolicy.Always;
});

builder.Services.ConfigureExternalCookie(options =>
{
    options.Cookie.SameSite =
        SameSiteMode.Lax;

    options.Cookie.SecurePolicy =
        CookieSecurePolicy.Always;

    options.Cookie.IsEssential =
        true;

    options.Cookie.Path =
        "/";

    options.ExpireTimeSpan =
        TimeSpan.FromMinutes(15);
});

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy =
        SameSiteMode.Unspecified;

    options.Secure =
        CookieSecurePolicy.Always;
});


// ============================================================
// MVC
// ============================================================

builder.Services.AddControllersWithViews();


// ============================================================
// RAZOR PAGES
// ============================================================

builder.Services.AddRazorPages();


// ============================================================
// SESSION
// ============================================================

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout =
        TimeSpan.FromHours(2);

    options.Cookie.HttpOnly = true;

    options.Cookie.IsEssential = true;
});


// ============================================================
// SIGNALR
// ============================================================
// This fixes the error:
// Unable to resolve service for type
// Microsoft.AspNetCore.SignalR.IHubContext<ChatHub>
// ============================================================

builder.Services.AddSignalR();

builder.Services.AddSingleton<
    IUserIdProvider,
    ThriftHubUserIdProvider>();


// ============================================================
// THRIFTHUB SERVICES
// ============================================================

// ------------------------------------------------------------
// Paystack
// ------------------------------------------------------------
// IMPORTANT:
// AddHttpClient is required because PaystackService uses
// HttpClient.
// ------------------------------------------------------------

builder.Services.AddHttpClient<PaystackService>();


// ------------------------------------------------------------
// Email - Resend
// ------------------------------------------------------------
// Required by the new EmailSender which sends emails through
// the Resend API.
// ------------------------------------------------------------

builder.Services.AddHttpClient("Resend");

builder.Services.AddScoped<IEmailSender, EmailSender>();


builder.Services.AddSingleton<DatabasePersistenceService>();

builder.Services.AddSingleton<AppStorageService>();

builder.Services.AddScoped<IdentityDocumentArchiveService>();

builder.Services.AddScoped<AccountDeletionService>();

builder.Services.AddSingleton<QrCodeService>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<SiteSeoService>();


// ------------------------------------------------------------
// Notifications
// ------------------------------------------------------------

builder.Services.AddScoped<NotificationService>();

builder.Services.AddScoped<ProductViewService>();

builder.Services.AddScoped<ProductImageService>();

builder.Services.AddScoped<SellerSubscriptionService>();


// ============================================================
// RENDER / REVERSE PROXY
// ============================================================
// Render terminates HTTPS and forwards X-Forwarded-* headers.
// ASP.NET Core 8.0.17+ ignores those headers unless the proxy
// is trusted — without that, OAuth sees http:// and sign-in fails.

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;

    options.ForwardLimit = null;

    options.KnownNetworks.Add(
        new Microsoft.AspNetCore.HttpOverrides.IPNetwork(
            IPAddress.Any,
            0));

    options.KnownNetworks.Add(
        new Microsoft.AspNetCore.HttpOverrides.IPNetwork(
            IPAddress.IPv6Any,
            0));
});


// ============================================================
// BUILD APPLICATION
// ============================================================

var app = builder.Build();


// ============================================================
// WRITABLE PERSISTENT STORAGE (RENDER DISK)
// ============================================================

{
    var persistence =
        app.Services.GetRequiredService<DatabasePersistenceService>();

    persistence.PrepareWritableStorage();

    Directory.CreateDirectory(dataProtectionPath);
}


// ============================================================
// EMAIL CONFIGURATION CHECK (RENDER LOGS)
// ============================================================

{
    var startupLogger =
        app.Services.GetRequiredService<ILogger<Program>>();

    var resendApiKey =
        app.Configuration["Resend:ApiKey"]
        ?? Environment.GetEnvironmentVariable("RESEND_API_KEY");

    var resendFromEmail =
        app.Configuration["Resend:FromEmail"];

    var smtpSenderEmail =
        app.Configuration["EmailSettings:SenderEmail"];

    if (string.IsNullOrWhiteSpace(resendApiKey))
    {
        startupLogger.LogWarning(
            "Resend API key is missing. " +
            "Set Resend__ApiKey on Render before registration emails will work."
        );
    }
    else
    {
        startupLogger.LogInformation(
            "Resend API key is configured."
        );
    }

    if (string.IsNullOrWhiteSpace(resendFromEmail))
    {
        startupLogger.LogWarning(
            "Resend:FromEmail is missing. " +
            "Verify thrifthubgh.com on Resend and set Resend__FromEmail " +
            "to noreply@thrifthubgh.com on Render."
        );
    }
    else if (resendFromEmail.Contains(
            "resend.dev",
            StringComparison.OrdinalIgnoreCase))
    {
        startupLogger.LogWarning(
            "Resend:FromEmail uses resend.dev. " +
            "Production registration emails require a verified domain sender."
        );
    }
    else
    {
        startupLogger.LogInformation(
            "Resend sender email is configured as {SenderEmail}.",
            resendFromEmail
        );
    }

    if (string.IsNullOrWhiteSpace(smtpSenderEmail))
    {
        startupLogger.LogWarning(
            "EmailSettings:SenderEmail is missing. " +
            "Set EmailSettings__SenderEmail and EmailSettings__SenderPassword on Render " +
            "so password reset emails can use SMTP when Resend fails."
        );
    }
    else
    {
        startupLogger.LogInformation(
            "SMTP fallback sender is configured as {SenderEmail}.",
            smtpSenderEmail
        );
    }

    var storage =
        app.Services.GetRequiredService<AppStorageService>();

    var persistence =
        app.Services.GetRequiredService<DatabasePersistenceService>();

    persistence.LogPersistenceWarnings();

    storage.SeedPersistentDatabaseIfNeeded();

    storage.MigrateLegacyUploadsToPersistentStorage();

    if (persistence.UsesPersistentStorage)
    {
        startupLogger.LogInformation(
            "Persistent storage enabled at {DataPath}.",
            persistence.DataRoot);
    }
}


app.UseForwardedHeaders();


if (!app.Environment.IsDevelopment())
{
    app.Use((context, next) =>
    {
        context.Request.Scheme = "https";

        return next();
    });
}


app.UseCookiePolicy();


app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger =
            context.RequestServices.GetRequiredService<ILogger<Program>>();

        logger.LogError(
            ex,
            "Unhandled request failure for {Method} {Path}.",
            context.Request.Method,
            context.Request.Path);

        throw;
    }
});


// ============================================================
// AUTOMATIC DATABASE MIGRATIONS
// ============================================================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var context =
            services.GetRequiredService<ApplicationDbContext>();

        var persistence =
            services.GetRequiredService<DatabasePersistenceService>();

        await persistence
            .EnsureBestDatabaseAvailableAsync(
                context);
    }
    catch (Exception ex)
    {
        var logger =
            services.GetRequiredService<ILogger<Program>>();

        logger.LogCritical(
            ex,
            "Database startup failed. The app cannot run safely until this is fixed."
        );

        throw;
    }
}


// ============================================================
// ERROR HANDLING
// ============================================================

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
}


// ============================================================
// HTTPS
// ============================================================

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}


// ============================================================
// STATIC FILES
// ============================================================

{
    var storage =
        app.Services.GetRequiredService<AppStorageService>();

    if (storage.UsesPersistentStorage)
    {
        app.UseStaticFiles(
            new StaticFileOptions
            {
                FileProvider =
                    new PhysicalFileProvider(
                        storage.GetUploadsRoot()),
                RequestPath = "/uploads"
            });
    }
}

app.UseStaticFiles();


// ============================================================
// ROUTING
// ============================================================

app.UseRouting();


// ============================================================
// SESSION
// ============================================================

app.UseSession();


// ============================================================
// AUTHENTICATION
// ============================================================

app.UseAuthentication();


// ============================================================
// AUTHORIZATION
// ============================================================

app.UseAuthorization();


// ============================================================
// SIGNALR CHAT HUB
// ============================================================
// Your Messages/Chat system can now resolve ChatHub.
// ============================================================

app.MapHub<ChatHub>("/chatHub");


// ============================================================
// DEFAULT MVC ROUTE
// ============================================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);


// ============================================================
// RAZOR PAGES
// ============================================================

app.MapRazorPages();


// ============================================================
// RUN APPLICATION
// ============================================================

app.Run();