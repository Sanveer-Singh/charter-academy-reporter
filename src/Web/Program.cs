using Microsoft.Extensions.FileProviders;
using System.IO;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Charter.Reporter.Infrastructure.Data;
using Charter.Reporter.Infrastructure.Identity;
using Charter.Reporter.Domain.Roles;
using Charter.Reporter.Domain.Policies;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Http;
using Charter.Reporter.Shared.Config;
using Charter.Reporter.Shared.Email;
using Charter.Reporter.Infrastructure.Email;
using Charter.Reporter.Application.Services.Dashboard;
using Charter.Reporter.Infrastructure.Services.Dashboard;
using Charter.Reporter.Shared.Export;
using Charter.Reporter.Infrastructure.Data.MariaDb;
using Charter.Reporter.Infrastructure.Services.Export;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Data.Sqlite;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddFluentValidationAutoValidation()
    .AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Charter.Reporter.Web.Validation.LoginVmValidator>();

// EF Core SQLite and Identity
var connectionString = builder.Configuration.GetConnectionString("AppDb") ?? "Data Source=app.db";
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedEmail = builder.Environment.IsProduction();
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.User.RequireUniqueEmail = true;
        // Security hardening: strict lockout policy
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 3;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromDays(30);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AppPolicies.RequireAnyAdmin, p => p.RequireRole(AppRoles.All));
    options.AddPolicy(AppPolicies.RequireCharterAdmin, p => p.RequireRole(AppRoles.CharterAdmin));
    options.AddPolicy(AppPolicies.RequireRebosaAdmin, p => p.RequireRole(AppRoles.RebosaAdmin));
    options.AddPolicy(AppPolicies.RequirePpraAdmin, p => p.RequireRole(AppRoles.PpraAdmin));
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    if (builder.Environment.IsDevelopment())
    {
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    }
    else
    {
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    }
});

// User Management Services
builder.Services.AddScoped<Charter.Reporter.Application.Services.Users.IUserManagementService, Charter.Reporter.Infrastructure.Services.Users.UserManagementService>();

// Email + Dashboard DI
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));

// Priority for email sending:
// 1. SendGrid (if API key is available) - preferred for all environments
// 2. SMTP (if properly configured)
// 3. DevNoop (only as last resort in development)
var sendGridKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
var smtpHost = builder.Configuration["Email:Host"];

if (!string.IsNullOrWhiteSpace(sendGridKey))
{
    // Use SendGrid if API key is available (in any environment)
    builder.Services.AddScoped<IEmailSender, SendGridEmailSender>();
    builder.Services.AddLogging(logging =>
    {
        logging.AddConsole();
    });
    Console.WriteLine($"Email Service: Using SendGrid (API key configured)");
}
else if (!string.IsNullOrWhiteSpace(smtpHost) && !string.Equals(smtpHost, "smtp.example.com", StringComparison.OrdinalIgnoreCase))
{
    // Use SMTP if properly configured
    builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
    Console.WriteLine($"Email Service: Using SMTP ({smtpHost})");
}
else
{
    if (builder.Environment.IsDevelopment())
    {
        // Only use DevNoop in development as last resort
        builder.Services.AddScoped<IEmailSender, DevNoopEmailSender>();
        Console.WriteLine("WARNING: Email Service: Using DevNoopEmailSender - emails will NOT be sent!");
        Console.WriteLine("To enable real email sending, set SENDGRID_API_KEY environment variable");
    }
    else
    {
        // In production, fail if no email service is configured
        throw new InvalidOperationException("No email service configured. Please set SENDGRID_API_KEY environment variable or configure SMTP settings.");
    }
}
// Replace stub with MariaDB-backed service
builder.Services.AddScoped<IDashboardService, MariaDbDashboardService>();
builder.Services.AddScoped<IWordPressReportService, WordPressReportService>();
builder.Services.AddScoped<IMergedReportService, MergedReportService>();
builder.Services.Configure<ExportOptions>(builder.Configuration.GetSection("Export"));
builder.Services.AddScoped<IExportSafetyService>(sp =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ExportOptions>>().Value;
    return new ExportSafetyService(options);
});
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
builder.Services.Configure<AutoApproveOptions>(builder.Configuration.GetSection("AutoApprove"));
// MariaDB connection factory with named options
builder.Services.Configure<MariaDbSettings>("Moodle", builder.Configuration.GetSection("MariaDb:Moodle"));
builder.Services.Configure<MariaDbSettings>("Woo", builder.Configuration.GetSection("MariaDb:Woo"));
builder.Services.AddScoped<IMariaDbConnectionFactory, MariaDbConnectionFactory>();

// Health checks
builder.Services.AddHealthChecks()
    .AddCheck("sqlite", () =>
    {
        try
        {
            using var c = new SqliteConnection(builder.Configuration.GetConnectionString("AppDb"));
            c.Open();
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, exception: ex);
        }
    })
    .AddCheck("moodle-mariadb", () => TryMySql(builder.Configuration.GetSection("MariaDb:Moodle")))
    .AddCheck("woo-mariadb", () => TryMySql(builder.Configuration.GetSection("MariaDb:Woo")));

static Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult TryMySql(IConfiguration section)
{
    try
    {
        var csb = new MySqlConnectionStringBuilder
        {
            Server = section["Host"],
            Port = (uint)(int.Parse(section["Port"] ?? "3306")),
            Database = section["Database"],
            UserID = section["Username"],
            Password = section["Password"],
            SslMode = MySqlSslMode.Preferred
        };
        using var conn = new MySqlConnection(csb.ConnectionString);
        conn.Open();
        using var cmd = new MySqlCommand("SELECT 1", conn);
        cmd.ExecuteScalar();
        return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy();
    }
    catch (Exception ex)
    {
        return new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, exception: ex);
    }
}

var app = builder.Build();

// Database migration and role seeding
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in AppRoles.All)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Seed default admin if configured
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var adminEmail = app.Configuration["Admin:Email"];
    var adminPassword = app.Configuration["Admin:Password"];
    if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
    {
        var existing = await userManager.FindByEmailAsync(adminEmail);
        if (existing == null)
        {
            var admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true, FirstName = "Admin", LastName = "User" };
            var created = await userManager.CreateAsync(admin, adminPassword);
            if (created.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, AppRoles.CharterAdmin);
            }
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
// SB Admin 2 assets are copied into wwwroot at build time via MSBuild target.

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

await app.RunAsync();
