using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using ThriftHub.Data;
using ThriftHub.Hubs;
using ThriftHub.Models;
using ThriftHub.Services;

var builder = WebApplication.CreateBuilder(args);


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


// ------------------------------------------------------------
// Notifications
// ------------------------------------------------------------

builder.Services.AddScoped<NotificationService>();

builder.Services.AddScoped<SellerSubscriptionService>();


// ============================================================
// RENDER / REVERSE PROXY
// ============================================================
// Ensures password reset links in emails use https on Render.

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;
});


// ============================================================
// BUILD APPLICATION
// ============================================================

var app = builder.Build();


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

    var storage =
        app.Services.GetRequiredService<AppStorageService>();

    var persistence =
        app.Services.GetRequiredService<DatabasePersistenceService>();

    persistence.LogPersistenceWarnings();

    storage.SeedPersistentDatabaseIfNeeded();

    if (persistence.UsesPersistentStorage)
    {
        startupLogger.LogInformation(
            "Persistent storage enabled at {DataPath}.",
            persistence.DataRoot);
    }
}


app.UseForwardedHeaders();


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