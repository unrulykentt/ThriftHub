using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ThriftHub.Data;
using ThriftHub.Hubs;
using ThriftHub.Models;
using ThriftHub.Services;

var builder = WebApplication.CreateBuilder(args);


// ============================================================
// DATABASE
// ============================================================
// ThriftHub is currently using SQLite.
// appsettings.json:
// "DefaultConnection": "Data Source=thrifthub.db"
// ============================================================

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "DefaultConnection was not found in appsettings.json."
    );
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(connectionString);
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
// Email
// ------------------------------------------------------------

builder.Services.AddScoped<IEmailSender, EmailSender>();


// ------------------------------------------------------------
// Notifications
// ------------------------------------------------------------

builder.Services.AddScoped<NotificationService>();


// ============================================================
// BUILD APPLICATION
// ============================================================

var app = builder.Build();


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