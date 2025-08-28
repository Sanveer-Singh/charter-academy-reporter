# Charter Reporter App - Project Starter Template

## Quick Start Guide

This template provides the essential files to get started with the Charter Reporter App using SB Admin 2 with proper CSS hierarchy.

### 1. Project Setup Commands

```bash
# Create the solution and projects
dotnet new sln -n Charter.ReporterApp
dotnet new mvc -n Charter.ReporterApp.Web
dotnet new classlib -n Charter.ReporterApp.Domain
dotnet new classlib -n Charter.ReporterApp.Application
dotnet new classlib -n Charter.ReporterApp.Infrastructure

# Add projects to solution
dotnet sln add Charter.ReporterApp.Web/Charter.ReporterApp.Web.csproj
dotnet sln add Charter.ReporterApp.Domain/Charter.ReporterApp.Domain.csproj
dotnet sln add Charter.ReporterApp.Application/Charter.ReporterApp.Application.csproj
dotnet sln add Charter.ReporterApp.Infrastructure/Charter.ReporterApp.Infrastructure.csproj

# Add project references
cd Charter.ReporterApp.Web
dotnet add reference ../Charter.ReporterApp.Application/Charter.ReporterApp.Application.csproj

cd ../Charter.ReporterApp.Application
dotnet add reference ../Charter.ReporterApp.Domain/Charter.ReporterApp.Domain.csproj

cd ../Charter.ReporterApp.Infrastructure
dotnet add reference ../Charter.ReporterApp.Domain/Charter.ReporterApp.Domain.csproj
dotnet add reference ../Charter.ReporterApp.Application/Charter.ReporterApp.Application.csproj

# Install required packages
cd ../Charter.ReporterApp.Web
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design

cd ../Charter.ReporterApp.Infrastructure
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Pomelo.EntityFrameworkCore.MySql
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
```

### 2. _Layout.cshtml - Master Layout with CSS Hierarchy

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta http-equiv="X-UA-Compatible" content="IE=edge">
    <meta name="viewport" content="width=device-width, initial-scale=1, shrink-to-fit=no">
    <meta name="description" content="Charter Reporter App - Secure reporting tool for administrators">
    <title>@ViewData["Title"] - Charter Reporter</title>

    <!-- Custom fonts for SB Admin 2 -->
    <link href="~/vendor/fontawesome-free/css/all.min.css" rel="stylesheet" type="text/css">
    <link href="https://fonts.googleapis.com/css?family=Nunito:200,200i,300,300i,400,400i,600,600i,700,700i,800,800i,900,900i" rel="stylesheet">

    <!-- CSS Hierarchy Implementation -->
    <!-- 1. Base Layer: SB Admin 2 (DO NOT MODIFY) -->
    <link href="~/vendor/sb-admin-2/css/sb-admin-2.min.css" rel="stylesheet">

    <!-- 2. Site Layer: Global overrides and variables -->
    <link href="~/css/variables.css" rel="stylesheet">
    <link href="~/css/site.css" rel="stylesheet">

    <!-- 3. Module Layer: Page-specific styles -->
    @await RenderSectionAsync("Styles", required: false)
</head>

<body id="page-top">
    <!-- Skip to main content link for accessibility -->
    <a href="#main-content" class="skip-link">Skip to main content</a>

    <!-- Page Wrapper -->
    <div id="wrapper">
        <!-- Sidebar -->
        @await Html.PartialAsync("_Sidebar")

        <!-- Content Wrapper -->
        <div id="content-wrapper" class="d-flex flex-column">
            <!-- Main Content -->
            <div id="content">
                <!-- Topbar -->
                @await Html.PartialAsync("_Topbar")

                <!-- Begin Page Content -->
                <main id="main-content" class="container-fluid">
                    @RenderBody()
                </main>
                <!-- /.container-fluid -->
            </div>
            <!-- End of Main Content -->

            <!-- Footer -->
            @await Html.PartialAsync("_Footer")
        </div>
        <!-- End of Content Wrapper -->
    </div>
    <!-- End of Page Wrapper -->

    <!-- Scroll to Top Button-->
    <a class="scroll-to-top rounded" href="#page-top">
        <i class="fas fa-angle-up"></i>
    </a>

    <!-- Logout Modal-->
    @await Html.PartialAsync("_LogoutModal")

    <!-- JavaScript -->
    <!-- Bootstrap core JavaScript-->
    <script src="~/vendor/jquery/jquery.min.js"></script>
    <script src="~/vendor/bootstrap/js/bootstrap.bundle.min.js"></script>

    <!-- Core plugin JavaScript-->
    <script src="~/vendor/jquery-easing/jquery.easing.min.js"></script>

    <!-- SB Admin 2 JavaScript -->
    <script src="~/vendor/sb-admin-2/js/sb-admin-2.min.js"></script>

    <!-- Site-wide JavaScript -->
    <script src="~/js/site.js"></script>

    <!-- Anti-forgery token helper for AJAX -->
    <script>
      window.getCsrfToken = function() {
        var el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
      };
    </script>

    <!-- Page-specific scripts -->
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

### 3. variables.css - Theme Configuration

```css
/* Charter Reporter App - CSS Variables
   This file defines all theme variables used across the application
   Override SB Admin 2 defaults with Charter-specific values */

:root {
    /* Brand Colors */
    --charter-primary: #1e3a8a;        /* Deep blue - main brand color */
    --charter-primary-dark: #1e2f5c;   /* Darker variant for hover states */
    --charter-primary-light: #3b5998;  /* Lighter variant */
    --charter-secondary: #64748b;      /* Slate gray */
    --charter-accent: #3b82f6;         /* Bright blue accent */
    
    /* Status Colors */
    --charter-success: #10b981;        /* Green - approvals, success */
    --charter-danger: #ef4444;         /* Red - errors, rejections */
    --charter-warning: #f59e0b;        /* Amber - warnings, pending */
    --charter-info: #06b6d4;           /* Cyan - information */
    
    /* Neutral Colors */
    --charter-gray-50: #f9fafb;
    --charter-gray-100: #f3f4f6;
    --charter-gray-200: #e5e7eb;
    --charter-gray-300: #d1d5db;
    --charter-gray-400: #9ca3af;
    --charter-gray-500: #6b7280;
    --charter-gray-600: #4b5563;
    --charter-gray-700: #374151;
    --charter-gray-800: #1f2937;
    --charter-gray-900: #111827;
    
    /* Typography */
    --charter-font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
    --charter-font-size-xs: 0.75rem;   /* 12px */
    --charter-font-size-sm: 0.875rem;  /* 14px */
    --charter-font-size-base: 1rem;    /* 16px */
    --charter-font-size-lg: 1.125rem;  /* 18px */
    --charter-font-size-xl: 1.25rem;   /* 20px */
    --charter-font-size-2xl: 1.5rem;   /* 24px */
    --charter-font-size-3xl: 1.875rem; /* 30px */
    
    /* Spacing (8px rhythm) */
    --charter-spacing-xs: 0.25rem;     /* 4px */
    --charter-spacing-sm: 0.5rem;      /* 8px */
    --charter-spacing-md: 1rem;        /* 16px */
    --charter-spacing-lg: 1.5rem;      /* 24px */
    --charter-spacing-xl: 2rem;        /* 32px */
    --charter-spacing-2xl: 3rem;       /* 48px */
    --charter-spacing-3xl: 4rem;       /* 64px */
    
    /* Border Radius */
    --charter-radius-sm: 0.25rem;      /* 4px */
    --charter-radius-md: 0.375rem;     /* 6px */
    --charter-radius-lg: 0.5rem;       /* 8px */
    --charter-radius-xl: 0.75rem;      /* 12px */
    --charter-radius-full: 9999px;     /* Full round */
    
    /* Shadows */
    --charter-shadow-sm: 0 1px 2px 0 rgba(0, 0, 0, 0.05);
    --charter-shadow-md: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
    --charter-shadow-lg: 0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05);
    --charter-shadow-xl: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
    
    /* Transitions */
    --charter-transition-fast: 150ms ease-in-out;
    --charter-transition-base: 200ms ease-in-out;
    --charter-transition-slow: 300ms ease-in-out;
    
    /* Z-index Scale */
    --charter-z-dropdown: 1000;
    --charter-z-sticky: 1020;
    --charter-z-fixed: 1030;
    --charter-z-modal-backdrop: 1040;
    --charter-z-modal: 1050;
    --charter-z-popover: 1060;
    --charter-z-tooltip: 1070;
    
    /* Breakpoints (for reference in JS) */
    --charter-breakpoint-sm: 576px;
    --charter-breakpoint-md: 768px;
    --charter-breakpoint-lg: 1024px;
    --charter-breakpoint-xl: 1280px;
    --charter-breakpoint-2xl: 1536px;
}

/* Dark mode support */
@media (prefers-color-scheme: dark) {
    :root {
        --charter-primary: #3b82f6;
        --charter-primary-dark: #2563eb;
        --charter-primary-light: #60a5fa;
        
        /* Invert gray scale for dark mode */
        --charter-gray-50: #111827;
        --charter-gray-100: #1f2937;
        --charter-gray-200: #374151;
        --charter-gray-300: #4b5563;
        --charter-gray-400: #6b7280;
        --charter-gray-500: #9ca3af;
        --charter-gray-600: #d1d5db;
        --charter-gray-700: #e5e7eb;
        --charter-gray-800: #f3f4f6;
        --charter-gray-900: #f9fafb;
    }
}

/* High contrast mode */
@media (prefers-contrast: high) {
    :root {
        --charter-primary: #0d47a1;
        --charter-shadow-sm: 0 0 0 1px rgba(0, 0, 0, 0.1);
        --charter-shadow-md: 0 0 0 2px rgba(0, 0, 0, 0.1);
        --charter-shadow-lg: 0 0 0 3px rgba(0, 0, 0, 0.1);
    }
}
```

### 4. site.css - Global Site Overrides

```css
/* Charter Reporter App - Global Site Styles
   This file contains site-wide overrides to SB Admin 2 theme
   Using CSS variables defined in variables.css */

/* ========================================
   Global Resets and Base Styles
   ======================================== */

html {
    font-size: 16px;
    scroll-behavior: smooth;
}

body {
    font-family: var(--charter-font-family);
    color: var(--charter-gray-900);
    background-color: var(--charter-gray-50);
}

/* ========================================
   Override SB Admin 2 Primary Colors
   ======================================== */

.bg-primary {
    background-color: var(--charter-primary) !important;
}

.text-primary {
    color: var(--charter-primary) !important;
}

.btn-primary {
    background-color: var(--charter-primary);
    border-color: var(--charter-primary);
    transition: all var(--charter-transition-fast);
}

.btn-primary:hover,
.btn-primary:focus {
    background-color: var(--charter-primary-dark);
    border-color: var(--charter-primary-dark);
    transform: translateY(-1px);
    box-shadow: var(--charter-shadow-md);
}

.btn-primary:active {
    transform: translateY(0);
}

/* ========================================
   Typography Enhancements
   ======================================== */

h1, h2, h3, h4, h5, h6 {
    color: var(--charter-gray-900);
    font-weight: 600;
    line-height: 1.2;
}

.h1, h1 { font-size: var(--charter-font-size-3xl); }
.h2, h2 { font-size: var(--charter-font-size-2xl); }
.h3, h3 { font-size: var(--charter-font-size-xl); }
.h4, h4 { font-size: var(--charter-font-size-lg); }
.h5, h5 { font-size: var(--charter-font-size-base); }
.h6, h6 { font-size: var(--charter-font-size-sm); }

/* ========================================
   Card Enhancements
   ======================================== */

.card {
    border: none;
    box-shadow: var(--charter-shadow-sm);
    transition: box-shadow var(--charter-transition-base);
}

.card:hover {
    box-shadow: var(--charter-shadow-md);
}

.card-header {
    background-color: var(--charter-gray-50);
    border-bottom: 1px solid var(--charter-gray-200);
    font-weight: 600;
}

/* ========================================
   Form Enhancements
   ======================================== */

.form-control {
    border-color: var(--charter-gray-300);
    transition: all var(--charter-transition-fast);
}

.form-control:focus {
    border-color: var(--charter-primary);
    box-shadow: 0 0 0 0.2rem rgba(30, 58, 138, 0.25);
}

.form-control.is-invalid:focus {
    box-shadow: 0 0 0 0.2rem rgba(239, 68, 68, 0.25);
}

/* Required field indicator */
.required-indicator {
    color: var(--charter-danger);
    font-weight: 600;
    margin-left: 2px;
}

/* ========================================
   Table Enhancements
   ======================================== */

.table {
    color: var(--charter-gray-700);
}

.table thead th {
    background-color: var(--charter-gray-50);
    color: var(--charter-gray-900);
    font-weight: 600;
    text-transform: uppercase;
    font-size: var(--charter-font-size-sm);
    letter-spacing: 0.05em;
    border-bottom: 2px solid var(--charter-gray-200);
}

.table-hover tbody tr:hover {
    background-color: var(--charter-gray-50);
}

/* ========================================
   Accessibility Enhancements
   ======================================== */

/* Focus states */
:focus-visible {
    outline: 2px solid var(--charter-primary);
    outline-offset: 2px;
}

/* Skip to main content link */
.skip-link {
    position: absolute;
    top: -40px;
    left: 0;
    background: var(--charter-primary);
    color: white;
    padding: var(--charter-spacing-sm) var(--charter-spacing-md);
    text-decoration: none;
    z-index: var(--charter-z-tooltip);
    border-radius: var(--charter-radius-md);
}

.skip-link:focus {
    top: var(--charter-spacing-sm);
    left: var(--charter-spacing-sm);
}

/* Screen reader only text */
.sr-only {
    position: absolute;
    width: 1px;
    height: 1px;
    padding: 0;
    margin: -1px;
    overflow: hidden;
    clip: rect(0, 0, 0, 0);
    white-space: nowrap;
    border: 0;
}

/* ========================================
   Loading States
   ======================================== */

.loading {
    position: relative;
    opacity: 0.6;
    pointer-events: none;
}

.loading::after {
    content: "";
    position: absolute;
    top: 50%;
    left: 50%;
    width: 24px;
    height: 24px;
    margin: -12px 0 0 -12px;
    border: 3px solid var(--charter-gray-300);
    border-top-color: var(--charter-primary);
    border-radius: 50%;
    animation: spinner 0.75s linear infinite;
}

@keyframes spinner {
    to { transform: rotate(360deg); }
}

/* Loading button state */
.btn.is-loading {
    color: transparent;
    position: relative;
    pointer-events: none;
}

.btn.is-loading::after {
    content: "";
    position: absolute;
    width: 16px;
    height: 16px;
    top: 50%;
    left: 50%;
    margin: -8px 0 0 -8px;
    border: 2px solid currentColor;
    border-right-color: transparent;
    border-radius: 50%;
    animation: spinner 0.75s linear infinite;
}

/* ========================================
   Alert Enhancements
   ======================================== */

.alert {
    border: none;
    border-radius: var(--charter-radius-md);
    box-shadow: var(--charter-shadow-sm);
}

.alert-success {
    background-color: rgba(16, 185, 129, 0.1);
    color: var(--charter-success);
    border-left: 4px solid var(--charter-success);
}

.alert-danger {
    background-color: rgba(239, 68, 68, 0.1);
    color: var(--charter-danger);
    border-left: 4px solid var(--charter-danger);
}

.alert-warning {
    background-color: rgba(245, 158, 11, 0.1);
    color: var(--charter-warning);
    border-left: 4px solid var(--charter-warning);
}

.alert-info {
    background-color: rgba(6, 182, 212, 0.1);
    color: var(--charter-info);
    border-left: 4px solid var(--charter-info);
}

/* ========================================
   Badge Enhancements
   ======================================== */

.badge {
    font-weight: 500;
    padding: 0.25em 0.6em;
    border-radius: var(--charter-radius-sm);
}

.badge-primary {
    background-color: var(--charter-primary);
}

.badge-success {
    background-color: var(--charter-success);
}

.badge-danger {
    background-color: var(--charter-danger);
}

.badge-warning {
    background-color: var(--charter-warning);
    color: var(--charter-gray-900);
}

/* ========================================
   Utility Classes
   ======================================== */

/* Spacing utilities using Charter spacing scale */
.mt-charter-1 { margin-top: var(--charter-spacing-sm); }
.mt-charter-2 { margin-top: var(--charter-spacing-md); }
.mt-charter-3 { margin-top: var(--charter-spacing-lg); }
.mt-charter-4 { margin-top: var(--charter-spacing-xl); }
.mt-charter-5 { margin-top: var(--charter-spacing-2xl); }

/* Text utilities */
.text-muted {
    color: var(--charter-gray-500) !important;
}

/* Shadow utilities */
.shadow-charter-sm { box-shadow: var(--charter-shadow-sm) !important; }
.shadow-charter-md { box-shadow: var(--charter-shadow-md) !important; }
.shadow-charter-lg { box-shadow: var(--charter-shadow-lg) !important; }

/* ========================================
   Responsive Utilities
   ======================================== */

@media (max-width: 768px) {
    .hide-mobile {
        display: none !important;
    }
    
    /* Reduce font sizes on mobile */
    html {
        font-size: 14px;
    }
    
    /* Stack buttons on mobile */
    .btn-group-mobile-stack {
        display: flex;
        flex-direction: column;
    }
    
    .btn-group-mobile-stack .btn {
        margin-bottom: var(--charter-spacing-sm);
    }
}

/* ========================================
   Print Styles
   ======================================== */

@media print {
    .no-print,
    .sidebar,
    .topbar,
    .btn,
    .alert-dismissible .close {
        display: none !important;
    }
    
    .card {
        box-shadow: none !important;
        border: 1px solid #dee2e6 !important;
    }
}

/* ========================================
   Animation Classes
   ======================================== */

.fade-in {
    animation: fadeIn var(--charter-transition-slow);
}

@keyframes fadeIn {
    from { opacity: 0; transform: translateY(10px); }
    to { opacity: 1; transform: translateY(0); }
}

.slide-in-right {
    animation: slideInRight var(--charter-transition-slow);
}

@keyframes slideInRight {
    from { transform: translateX(100%); }
    to { transform: translateX(0); }
}
```

### 5. BaseController.cs - Security Foundation

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Charter.ReporterApp.Application.Interfaces;
using System.Security.Claims;

namespace Charter.ReporterApp.Web.Controllers
{
    public abstract class BaseController : Controller
    {
        protected readonly ILogger<BaseController> _logger;
        protected readonly IAuditService _auditService;
        protected readonly ISecurityValidationService _securityService;

        protected BaseController(
            ILogger<BaseController> logger,
            IAuditService auditService,
            ISecurityValidationService securityService)
        {
            _logger = logger;
            _auditService = auditService;
            _securityService = securityService;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Security validation
            if (!_securityService.ValidateRequest(context))
            {
                context.Result = new ForbidResult();
                _auditService.LogUnauthorizedAccess(
                    User?.Identity?.Name ?? "Anonymous",
                    context.ActionDescriptor.DisplayName
                );
                return;
            }

            // Input validation
            if (!ModelState.IsValid)
            {
                _auditService.LogInvalidInput(
                    User?.Identity?.Name ?? "Anonymous",
                    context.ActionDescriptor.DisplayName,
                    ModelState
                );
            }

            // Add security headers
            Response.Headers.Add("X-Content-Type-Options", "nosniff");
            Response.Headers.Add("X-Frame-Options", "DENY");
            Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

            base.OnActionExecuting(context);
        }

        protected string GetUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }

        protected string GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }

        protected void SetSuccessMessage(string message)
        {
            TempData["SuccessMessage"] = message;
        }

        protected void SetErrorMessage(string message)
        {
            TempData["ErrorMessage"] = message;
        }

        protected void SetWarningMessage(string message)
        {
            TempData["WarningMessage"] = message;
        }

        protected IActionResult JsonSuccess(object data = null, string message = null)
        {
            return Json(new
            {
                success = true,
                message = message,
                data = data
            });
        }

        protected IActionResult JsonError(string message, object data = null)
        {
            return Json(new
            {
                success = false,
                message = message,
                data = data
            });
        }
    }
}
```

### 6. Program.cs - Application Configuration

```csharp
using Charter.ReporterApp.Infrastructure.Data;
using Charter.ReporterApp.Application.Services;
using Charter.ReporterApp.Application.Interfaces;
using Charter.ReporterApp.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // Global filters
    options.Filters.Add<SecurityHeadersAttribute>();
    options.Filters.Add<AuditActionFilter>();
});

// Database contexts
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));

builder.Services.AddDbContext<MoodleDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("MoodleConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("MoodleConnection"))
    ));

builder.Services.AddDbContext<WooCommerceDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("WooCommerceConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("WooCommerceConnection"))
    ));

// Identity configuration
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    // Password settings
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    
    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    
    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Cookie authentication
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
app.UseRouting();

// Content Security Policy
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.Add("Content-Security-Policy",
        "default-src 'self'; script-src 'self' cdn.jsdelivr.net 'sha256-...'; style-src 'self' 'sha256-...' fonts.googleapis.com; font-src 'self' fonts.gstatic.com; img-src 'self' data:; frame-ancestors 'none'");
    await next();
});

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Custom middleware
app.UseSecurityHeaders();
app.UseAuditLogging();

// Health checks and monitoring
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("AppDb")
    .AddDbContextCheck<MoodleDbContext>("MoodleDb")
    .AddDbContextCheck<WooCommerceDbContext>("WooDb");
app.MapHealthChecks("/healthz");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed roles and admin user
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    
    await SeedData.InitializeAsync(roleManager, userManager);
}

app.Run();
```

### 7. Sample Module CSS - authentication.css

```css
/* modules/authentication.css - Authentication module specific styles */

/* ========================================
   Login & Registration Layout
   ======================================== */

.auth-container {
    min-height: 100vh;
    display: flex;
    align-items: center;
    justify-content: center;
    background: linear-gradient(135deg, var(--charter-primary) 0%, var(--charter-primary-dark) 100%);
}

.auth-card {
    width: 100%;
    max-width: 400px;
    margin: var(--charter-spacing-lg);
}

.auth-card .card-body {
    padding: var(--charter-spacing-xl);
}

/* ========================================
   Authentication Form Styling
   ======================================== */

.auth-form__logo {
    text-align: center;
    margin-bottom: var(--charter-spacing-xl);
}

.auth-form__logo img {
    max-width: 200px;
    height: auto;
}

.auth-form__title {
    text-align: center;
    color: var(--charter-gray-900);
    font-size: var(--charter-font-size-2xl);
    font-weight: 600;
    margin-bottom: var(--charter-spacing-lg);
}

.auth-form__subtitle {
    text-align: center;
    color: var(--charter-gray-600);
    font-size: var(--charter-font-size-base);
    margin-bottom: var(--charter-spacing-xl);
}

/* Form inputs */
.auth-form .form-group {
    margin-bottom: var(--charter-spacing-lg);
}

.auth-form .form-control {
    padding: var(--charter-spacing-sm) var(--charter-spacing-md);
    font-size: var(--charter-font-size-base);
    border-radius: var(--charter-radius-md);
}

.auth-form .form-control:focus {
    border-color: var(--charter-primary);
    box-shadow: 0 0 0 0.2rem rgba(30, 58, 138, 0.25);
}

/* Remember me checkbox */
.auth-form__remember {
    display: flex;
    align-items: center;
    margin-bottom: var(--charter-spacing-lg);
}

.auth-form__remember input[type="checkbox"] {
    margin-right: var(--charter-spacing-sm);
}

/* Submit button */
.auth-form__submit {
    width: 100%;
    padding: var(--charter-spacing-sm) var(--charter-spacing-lg);
    font-size: var(--charter-font-size-base);
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    border-radius: var(--charter-radius-md);
    transition: all var(--charter-transition-base);
}

.auth-form__submit:hover {
    transform: translateY(-2px);
    box-shadow: var(--charter-shadow-lg);
}

/* Links */
.auth-form__links {
    text-align: center;
    margin-top: var(--charter-spacing-lg);
}

.auth-form__link {
    color: var(--charter-primary);
    text-decoration: none;
    font-size: var(--charter-font-size-sm);
    transition: color var(--charter-transition-fast);
}

.auth-form__link:hover {
    color: var(--charter-primary-dark);
    text-decoration: underline;
}

/* ========================================
   Registration Specific
   ======================================== */

.registration-form__section {
    margin-bottom: var(--charter-spacing-xl);
    padding-bottom: var(--charter-spacing-xl);
    border-bottom: 1px solid var(--charter-gray-200);
}

.registration-form__section:last-child {
    border-bottom: none;
    margin-bottom: 0;
    padding-bottom: 0;
}

.registration-form__section-title {
    font-size: var(--charter-font-size-lg);
    font-weight: 600;
    color: var(--charter-gray-900);
    margin-bottom: var(--charter-spacing-md);
}

/* Role selection */
.role-select {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
    gap: var(--charter-spacing-md);
    margin-top: var(--charter-spacing-md);
}

.role-option {
    position: relative;
    padding: var(--charter-spacing-md);
    border: 2px solid var(--charter-gray-300);
    border-radius: var(--charter-radius-md);
    cursor: pointer;
    transition: all var(--charter-transition-fast);
}

.role-option:hover {
    border-color: var(--charter-primary);
    background-color: var(--charter-gray-50);
}

.role-option input[type="radio"] {
    position: absolute;
    opacity: 0;
}

.role-option input[type="radio"]:checked + .role-option__content {
    border-color: var(--charter-primary);
    background-color: rgba(30, 58, 138, 0.05);
}

.role-option__content {
    display: block;
    text-align: center;
}

.role-option__icon {
    font-size: var(--charter-font-size-2xl);
    color: var(--charter-primary);
    margin-bottom: var(--charter-spacing-sm);
}

.role-option__name {
    font-weight: 600;
    color: var(--charter-gray-900);
}

/* ========================================
   Password Strength Indicator
   ======================================== */

.password-strength {
    margin-top: var(--charter-spacing-sm);
}

.password-strength__bar {
    height: 4px;
    background-color: var(--charter-gray-200);
    border-radius: var(--charter-radius-full);
    overflow: hidden;
}

.password-strength__fill {
    height: 100%;
    transition: width var(--charter-transition-base), background-color var(--charter-transition-base);
}

.password-strength__fill--weak {
    width: 33%;
    background-color: var(--charter-danger);
}

.password-strength__fill--medium {
    width: 66%;
    background-color: var(--charter-warning);
}

.password-strength__fill--strong {
    width: 100%;
    background-color: var(--charter-success);
}

.password-strength__text {
    font-size: var(--charter-font-size-sm);
    margin-top: var(--charter-spacing-xs);
}

/* ========================================
   Two-Factor Authentication
   ======================================== */

.tfa-code-inputs {
    display: flex;
    gap: var(--charter-spacing-sm);
    justify-content: center;
    margin: var(--charter-spacing-xl) 0;
}

.tfa-code-inputs input {
    width: 50px;
    height: 50px;
    text-align: center;
    font-size: var(--charter-font-size-xl);
    font-weight: 600;
    border: 2px solid var(--charter-gray-300);
    border-radius: var(--charter-radius-md);
    transition: all var(--charter-transition-fast);
}

.tfa-code-inputs input:focus {
    border-color: var(--charter-primary);
    box-shadow: 0 0 0 0.2rem rgba(30, 58, 138, 0.25);
}

/* ========================================
   Mobile Responsiveness
   ======================================== */

@media (max-width: 768px) {
    .auth-container {
        padding: var(--charter-spacing-md);
    }
    
    .auth-card {
        margin: 0;
    }
    
    .auth-card .card-body {
        padding: var(--charter-spacing-lg);
    }
    
    .role-select {
        grid-template-columns: 1fr;
    }
}

/* ========================================
   Loading States
   ======================================== */

.auth-form.is-submitting .auth-form__submit {
    position: relative;
    color: transparent;
}

.auth-form.is-submitting .auth-form__submit::after {
    content: "";
    position: absolute;
    width: 20px;
    height: 20px;
    top: 50%;
    left: 50%;
    margin: -10px 0 0 -10px;
    border: 2px solid #fff;
    border-right-color: transparent;
    border-radius: 50%;
    animation: spinner 0.75s linear infinite;
}
```

### 8. Directory Structure Creation Script

```powershell
# Create-ProjectStructure.ps1
# Run this in the Charter.ReporterApp.Web directory

# Create directories
$directories = @(
    "wwwroot/css/modules",
    "wwwroot/js/modules",
    "wwwroot/vendor/sb-admin-2/css",
    "wwwroot/vendor/sb-admin-2/js",
    "Views/Shared/Components",
    "Areas/CharterAdmin/Controllers",
    "Areas/CharterAdmin/Views",
    "Areas/RebosaAdmin/Controllers",
    "Areas/RebosaAdmin/Views",
    "Areas/PPRAAdmin/Controllers",
    "Areas/PPRAAdmin/Views"
)

foreach ($dir in $directories) {
    New-Item -ItemType Directory -Force -Path $dir
    Write-Host "Created directory: $dir" -ForegroundColor Green
}

# Create placeholder files
$files = @{
    "wwwroot/css/variables.css" = "/* CSS Variables */"
    "wwwroot/css/site.css" = "/* Site CSS */"
    "wwwroot/js/site.js" = "// Site JavaScript"
    "wwwroot/css/modules/authentication.css" = "/* Authentication Module CSS */"
    "wwwroot/css/modules/dashboard.css" = "/* Dashboard Module CSS */"
    "wwwroot/css/modules/reporting.css" = "/* Reporting Module CSS */"
    "wwwroot/css/modules/admin-approval.css" = "/* Admin Approval Module CSS */"
}

foreach ($file in $files.GetEnumerator()) {
    Set-Content -Path $file.Key -Value $file.Value
    Write-Host "Created file: $($file.Key)" -ForegroundColor Green
}

Write-Host "`nProject structure created successfully!" -ForegroundColor Yellow
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Copy SB Admin 2 files to wwwroot/vendor/sb-admin-2/" -ForegroundColor Cyan
Write-Host "2. Update appsettings.json with database connection strings" -ForegroundColor Cyan
Write-Host "3. Run 'dotnet ef migrations add InitialCreate'" -ForegroundColor Cyan
Write-Host "4. Run 'dotnet ef database update'" -ForegroundColor Cyan
```

## Summary

This starter template provides:

1. **Proper CSS Hierarchy**: Clear separation between base theme, site overrides, and module styles
2. **Security Foundation**: BaseController with built-in security validations
3. **Clean Architecture**: Proper project structure following SOLID principles
4. **Module Examples**: Authentication module with complete CSS implementation
5. **Configuration**: Complete Program.cs setup with all required services

To get started:
1. Run the project setup commands
2. Copy the provided files
3. Run the PowerShell script to create directories
4. Copy SB Admin 2 assets to the vendor folder
5. Configure your database connections
6. Run migrations
7. Start building your modules following the established patterns

This ensures a bulletproof implementation that adheres to all requirements while maintaining flexibility for future enhancements.
