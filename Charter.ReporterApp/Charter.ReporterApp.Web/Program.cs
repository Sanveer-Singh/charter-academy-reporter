using Charter.ReporterApp.Infrastructure.Data;
using Charter.ReporterApp.Application.Interfaces;
using Charter.ReporterApp.Infrastructure.Services;
using Charter.ReporterApp.Infrastructure.Repositories;
using Charter.ReporterApp.Domain.Interfaces;
using Charter.ReporterApp.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // Global filters can be added here if needed
    // options.Filters.Add<SecurityHeadersAttribute>();
    // options.Filters.Add<AuditActionFilter>();
});

// Database contexts
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));

// Conditional database contexts for external data sources
if (!string.IsNullOrEmpty(builder.Configuration.GetConnectionString("MoodleConnection")))
{
    builder.Services.AddDbContext<MoodleDbContext>(options =>
        options.UseMySql(
            builder.Configuration.GetConnectionString("MoodleConnection"),
            ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("MoodleConnection"))
        ));
}

if (!string.IsNullOrEmpty(builder.Configuration.GetConnectionString("WooCommerceConnection")))
{
    builder.Services.AddDbContext<WooCommerceDbContext>(options =>
        options.UseMySql(
            builder.Configuration.GetConnectionString("WooCommerceConnection"),
            ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("WooCommerceConnection"))
        ));
}

// Identity configuration
builder.Services.AddDefaultIdentity<User>(options =>
{
    // Password settings
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredUniqueChars = 6;
    
    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    
    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
    
    // Email confirmation
    options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
})
.AddRoles<Role>()
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Cookie authentication configuration
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CharterAdmin", policy => 
        policy.RequireRole("Charter-Admin"));
    
    options.AddPolicy("RebosaAdmin", policy => 
        policy.RequireRole("Rebosa-Admin"));
    
    options.AddPolicy("PPRAAdmin", policy => 
        policy.RequireRole("PPRA-Admin"));
    
    options.AddPolicy("AnyAdmin", policy => 
        policy.RequireRole("Charter-Admin", "Rebosa-Admin", "PPRA-Admin"));
    
    options.AddPolicy("RequireEmailConfirmed", policy =>
        policy.RequireClaim("EmailConfirmed", "true"));
});

// Application services
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ISecurityValidationService, SecurityValidationService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Repository services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();

// Security headers
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

// Data protection
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"./keys"))
    .SetApplicationName("CharterReporterApp");

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("AppDb");

// Memory cache for performance
builder.Services.AddMemoryCache();

// Background services for cleanup tasks
builder.Services.AddHostedService<CleanupService>();

// Logging configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configure application insights if available
if (!string.IsNullOrEmpty(builder.Configuration.GetConnectionString("ApplicationInsights")))
{
    builder.Services.AddApplicationInsightsTelemetry();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Security middleware
app.UseHttpsRedirection();
app.UseStaticFiles();

// Content Security Policy
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.Add("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' cdn.jsdelivr.net cdnjs.cloudflare.com; " +
        "style-src 'self' 'unsafe-inline' fonts.googleapis.com cdn.jsdelivr.net; " +
        "font-src 'self' fonts.gstatic.com cdn.jsdelivr.net; " +
        "img-src 'self' data: https:; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none'");
    await next();
});

app.UseRouting();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Health checks
app.MapHealthChecks("/health");

// Area routing
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Default routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<User>>();
        var roleManager = services.GetRequiredService<RoleManager<Role>>();
        
        await SeedData.InitializeAsync(context, userManager, roleManager, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database");
    }
}

app.Run();

/// <summary>
/// Background service for cleanup tasks
/// </summary>
public class CleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CleanupService> _logger;

    public CleanupService(IServiceProvider serviceProvider, ILogger<CleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                // Clean up expired registration requests (older than 30 days)
                var expiredRequests = await context.RegistrationRequests
                    .Where(r => r.Status == Charter.ReporterApp.Domain.Entities.RegistrationStatus.Pending 
                        && r.CreatedAt < DateTime.UtcNow.AddDays(-30))
                    .ToListAsync(stoppingToken);
                
                if (expiredRequests.Any())
                {
                    foreach (var request in expiredRequests)
                    {
                        request.Status = Charter.ReporterApp.Domain.Entities.RegistrationStatus.Expired;
                    }
                    
                    await context.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Cleaned up {Count} expired registration requests", expiredRequests.Count);
                }
                
                // Clean up old audit logs (older than 2 years)
                var oldAuditLogs = await context.AuditLogs
                    .Where(a => a.Timestamp < DateTime.UtcNow.AddYears(-2))
                    .ToListAsync(stoppingToken);
                
                if (oldAuditLogs.Any())
                {
                    context.AuditLogs.RemoveRange(oldAuditLogs);
                    await context.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Cleaned up {Count} old audit logs", oldAuditLogs.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during cleanup task");
            }
            
            // Run cleanup every 24 hours
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}