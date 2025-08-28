# Charter Reporter App - Implementation Plan with SB Admin 2

## Executive Summary
This implementation plan provides a bulletproof approach to building the Charter reporter app using SB Admin 2 theme with a proper CSS hierarchy. The plan ensures strict adherence to user stories, security requirements (OWASP), UI/UX standards (WCAG 2.1 AA), and architectural best practices.

## Table of Contents
1. [Project Structure](#project-structure)
2. [CSS Architecture & Hierarchy](#css-architecture--hierarchy)
3. [Module Implementation Plan](#module-implementation-plan)
4. [Security Implementation](#security-implementation)
5. [Database Architecture](#database-architecture)
6. [Phase-wise Implementation](#phase-wise-implementation)
7. [Quality Assurance](#quality-assurance)

---

## 1. Project Structure

### Solution Architecture
```
Charter.ReporterApp/
├── Charter.ReporterApp.Domain/           # Core business logic (no dependencies)
│   ├── Entities/
│   │   ├── User.cs
│   │   ├── Role.cs
│   │   ├── RegistrationRequest.cs
│   │   └── AuditLog.cs
│   ├── ValueObjects/
│   │   ├── PPRANumber.cs
│   │   └── DateRange.cs
│   └── Interfaces/
│       ├── IUserRepository.cs
│       └── IReportRepository.cs
│
├── Charter.ReporterApp.Application/      # Business services
│   ├── Services/
│   │   ├── AuthenticationService.cs
│   │   ├── RegistrationService.cs
│   │   ├── DashboardService.cs
│   │   └── ReportingService.cs
│   ├── DTOs/
│   │   ├── UserDto.cs
│   │   └── ReportDto.cs
│   └── Interfaces/
│       └── ISecurityValidationService.cs
│
├── Charter.ReporterApp.Infrastructure/   # External integrations
│   ├── Data/
│   │   ├── AppDbContext.cs
│   │   ├── MoodleDbContext.cs
│   │   └── WooCommerceDbContext.cs
│   ├── Repositories/
│   │   ├── UserRepository.cs
│   │   └── ReportRepository.cs
│   └── Services/
│       ├── EmailService.cs
│       └── AuditService.cs
│
└── Charter.ReporterApp.Web/             # Presentation layer
    ├── wwwroot/
    │   ├── css/
    │   │   ├── sb-admin-2.css          # Base theme (DO NOT MODIFY)
    │   │   ├── sb-admin-2.min.css      # Minified base
    │   │   ├── site.css                # Global site overrides
    │   │   ├── variables.css           # Theme variables
    │   │   └── modules/                # Module-specific styles
    │   │       ├── authentication.css
    │   │       ├── dashboard.css
    │   │       ├── reporting.css
    │   │       └── admin-approval.css
    │   ├── js/
    │   │   ├── sb-admin-2.js           # Base JS (DO NOT MODIFY)
    │   │   ├── site.js                 # Global site JS
    │   │   └── modules/                # Module-specific JS
    │   │       ├── dashboard.js
    │   │       └── reporting.js
    │   └── lib/                        # Vendor libraries
    │       └── sb-admin-2/             # SB Admin 2 assets
    │
    ├── Controllers/
    │   ├── BaseController.cs            # Security validation base
    │   ├── AccountController.cs
    │   ├── DashboardController.cs
    │   ├── ReportController.cs
    │   └── AdminController.cs
    │
    ├── Views/
    │   ├── Shared/
    │   │   ├── _Layout.cshtml
    │   │   ├── _LoginLayout.cshtml
    │   │   └── Components/
    │   │       ├── _Sidebar.cshtml
    │   │       ├── _Topbar.cshtml
    │   │       └── _LoadingState.cshtml
    │   ├── Account/
    │   │   ├── Login.cshtml
    │   │   ├── Register.cshtml
    │   │   └── ForgotPassword.cshtml
    │   ├── Dashboard/
    │   │   ├── Index.cshtml
    │   │   └── Components/
    │   │       ├── _ChartCard.cshtml
    │   │       └── _MetricCard.cshtml
    │   ├── Report/
    │   │   └── Index.cshtml
    │   └── Admin/
    │       └── Approvals.cshtml
    │
    └── Areas/                           # Role-specific areas
        ├── CharterAdmin/
        ├── RebosaAdmin/
        └── PPRAAdmin/
```

## 2. CSS Architecture & Hierarchy

### CSS Loading Order (CRITICAL)
```html
<!-- _Layout.cshtml -->
<!DOCTYPE html>
<html lang="en">
<head>
    <!-- 1. Base UI tokens from SB Admin 2 (NEVER MODIFY) -->
    <link href="~/lib/sb-admin-2/css/sb-admin-2.min.css" rel="stylesheet">
    
    <!-- 2. Site-wide overrides and theme variables -->
    <link href="~/css/variables.css" rel="stylesheet">
    <link href="~/css/site.css" rel="stylesheet">
    
    <!-- 3. Module-specific styles (loaded conditionally) -->
    @RenderSection("Styles", required: false)
</head>
```

### variables.css - Theme Configuration
```css
/* variables.css - Central theme configuration */
:root {
    /* Override SB Admin 2 colors for Charter branding */
    --primary: #1e3a8a;        /* Charter blue */
    --secondary: #64748b;      /* Slate gray */
    --success: #10b981;        /* Green */
    --danger: #ef4444;         /* Red */
    --warning: #f59e0b;        /* Amber */
    --info: #3b82f6;          /* Blue */
    
    /* Spacing system (8px rhythm) */
    --spacing-xs: 0.25rem;     /* 4px */
    --spacing-sm: 0.5rem;      /* 8px */
    --spacing-md: 1rem;        /* 16px */
    --spacing-lg: 1.5rem;      /* 24px */
    --spacing-xl: 2rem;        /* 32px */
    --spacing-2xl: 3rem;       /* 48px */
    
    /* Typography */
    --font-family-base: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
    --font-size-base: 1rem;
    --line-height-base: 1.6;
    
    /* Borders and shadows */
    --border-radius: 0.35rem;
    --border-radius-lg: 0.5rem;
    --box-shadow: 0 0.15rem 1.75rem 0 rgba(58, 59, 69, 0.15);
    
    /* Breakpoints */
    --breakpoint-sm: 576px;
    --breakpoint-md: 768px;
    --breakpoint-lg: 1024px;
    --breakpoint-xl: 1200px;
}
```

### site.css - Global Site Overrides
```css
/* site.css - Site-wide overrides to SB Admin 2 */

/* Global typography adjustments */
body {
    font-family: var(--font-family-base);
    line-height: var(--line-height-base);
}

/* Override SB Admin 2 primary button */
.btn-primary {
    background-color: var(--primary);
    border-color: var(--primary);
}

.btn-primary:hover {
    background-color: color-mix(in srgb, var(--primary) 85%, black);
    border-color: color-mix(in srgb, var(--primary) 85%, black);
}

/* Accessibility enhancements */
:focus-visible {
    outline: 2px solid var(--primary);
    outline-offset: 2px;
}

/* Loading states */
.loading-spinner {
    display: inline-block;
    width: 1rem;
    height: 1rem;
    border: 2px solid rgba(0, 0, 0, 0.1);
    border-left-color: var(--primary);
    border-radius: 50%;
    animation: spin 1s linear infinite;
}

@keyframes spin {
    to { transform: rotate(360deg); }
}

/* Responsive utilities */
@media (max-width: 768px) {
    .hide-mobile { display: none !important; }
}

/* WCAG compliance - High contrast mode support */
@media (prefers-contrast: high) {
    :root {
        --primary: #0d47a1;
        --text-color: #000;
        --bg-color: #fff;
    }
}
```

### Module CSS Templates

#### authentication.css
```css
/* modules/authentication.css - Authentication module overrides */

/* Login/Register form customization */
.auth-form {
    max-width: 400px;
    margin: 0 auto;
}

.auth-form__header {
    text-align: center;
    margin-bottom: var(--spacing-xl);
}

.auth-form__title {
    color: var(--primary);
    font-size: 1.75rem;
    font-weight: 600;
}

/* Form field enhancements */
.auth-form .form-control {
    padding: var(--spacing-sm) var(--spacing-md);
    border-radius: var(--border-radius);
}

.auth-form .form-control:focus {
    border-color: var(--primary);
    box-shadow: 0 0 0 0.2rem rgba(30, 58, 138, 0.25);
}

/* Registration specific */
.registration-form__role-select {
    background-color: var(--bg-light);
}

/* Required field indicator */
.required-indicator {
    color: var(--danger);
    font-weight: 600;
}

/* Mobile responsiveness */
@media (max-width: 768px) {
    .auth-form {
        padding: var(--spacing-md);
    }
}
```

#### dashboard.css
```css
/* modules/dashboard.css - Dashboard module overrides */

/* Dashboard grid layout */
.dashboard-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
    gap: var(--spacing-lg);
}

/* Override SB Admin 2 card for dashboard metrics */
.metric-card {
    background: white;
    border-left: 4px solid var(--primary);
    transition: transform 0.2s;
}

.metric-card:hover {
    transform: translateY(-2px);
    box-shadow: var(--box-shadow);
}

.metric-card__value {
    font-size: 2rem;
    font-weight: 700;
    color: var(--primary);
}

/* Chart containers */
.chart-container {
    position: relative;
    height: 400px;
    padding: var(--spacing-md);
}

/* Filter panel */
.filter-panel {
    background-color: var(--bg-light);
    border-radius: var(--border-radius);
    padding: var(--spacing-md);
    margin-bottom: var(--spacing-lg);
}

/* Export button customization */
.btn-export {
    background-color: var(--success);
    border-color: var(--success);
    color: white;
}

.btn-export:hover {
    background-color: color-mix(in srgb, var(--success) 85%, black);
}

/* Responsive adjustments */
@media (max-width: 768px) {
    .dashboard-grid {
        grid-template-columns: 1fr;
    }
    
    .chart-container {
        height: 300px;
    }
}
```

## 3. Module Implementation Plan

### Authentication Module

#### User Stories Coverage
- User registration with required fields ✓
- Email verification workflow ✓
- Charter Admin approval queue ✓
- Secure login with session management ✓

#### Implementation Details
```csharp
// AccountController.cs
[AllowAnonymous]
public class AccountController : BaseController
{
    private readonly IAuthenticationService _authService;
    private readonly IRegistrationService _registrationService;
    
    [HttpGet]
    public IActionResult Register()
    {
        var model = new RegisterViewModel
        {
            Roles = _registrationService.GetAvailableRoles()
        };
        return View(model);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);
            
        var result = await _registrationService.RegisterUserAsync(model);
        if (result.Succeeded)
        {
            TempData["Success"] = "Registration submitted. Please check your email.";
            return RedirectToAction("Login");
        }
        
        ModelState.AddModelError("", result.ErrorMessage);
        return View(model);
    }
}
```

#### Email Verification and Duplicate Email Handling
```csharp
// AccountController.cs (additional endpoints)
[AllowAnonymous]
[HttpGet]
public async Task<IActionResult> ConfirmEmail(string userId, string token)
{
    var result = await _authService.ConfirmEmailAsync(userId, token);
    if (result.Succeeded)
    {
        SetSuccessMessage("Email confirmed. You can now sign in.");
        _auditService.LogEvent(User, "EmailConfirmed", new { userId });
        return RedirectToAction("Login");
    }
    SetErrorMessage(result.ErrorMessage ?? "Email confirmation failed.");
    return RedirectToAction("Login");
}

// RegistrationService.cs (called from POST Register)
public async Task<OperationResult> RegisterUserAsync(RegisterViewModel model)
{
    var existing = await _userRepository.GetByEmailAsync(model.Email);
    if (existing != null)
        return OperationResult.Failed("Email already registered");

    var created = await _userRepository.CreateRegistrationRequestAsync(new RegistrationRequest { /* map fields incl. Address */ });
    if (!created) return OperationResult.Failed("Registration could not be created");

    await _emailService.SendVerificationEmailAsync(model.Email, await _authService.GenerateEmailTokenAsync(model.Email));
    _auditService.LogEvent(null, "RegistrationSubmitted", new { model.Email, model.RequestedRole });
    return OperationResult.Success();
}
```

### Admin Approval Workflow
```csharp
// AdminController.cs
[Authorize(Roles = "Charter-Admin")]
public class AdminController : BaseController
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveRequest(Guid id)
    {
        var ok = await _userRepository.ApproveRegistrationAsync(id, User.Identity?.Name ?? "");
        if (ok)
        {
            await _emailService.SendApprovalAsync(id);
            _auditService.LogEvent(User, "RegistrationApproved", new { id });
            return JsonSuccess(new { id }, "Approved");
        }
        return JsonError("Approval failed");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectRequest(Guid id, [FromBody] RejectDto dto)
    {
        var ok = await _userRepository.RejectRegistrationAsync(id, User.Identity?.Name ?? "", dto.Reason);
        if (ok)
        {
            await _emailService.SendRejectionAsync(id, dto.Reason);
            _auditService.LogEvent(User, "RegistrationRejected", new { id });
            return JsonSuccess(new { id }, "Rejected");
        }
        return JsonError("Rejection failed");
    }

    public sealed class RejectDto { public string Reason { get; set; } }
}
```

### Monitoring & Operations
```csharp
// Program.cs additions
builder.Services.AddApplicationInsightsTelemetry();
// or Serilog
// Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
// builder.Host.UseSerilog();

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("AppDb")
    .AddDbContextCheck<MoodleDbContext>("MoodleDb")
    .AddDbContextCheck<WooCommerceDbContext>("WooDb");

app.MapHealthChecks("/healthz");
```

### Error Handling and UX
- Use SB Admin 2 alerts with `role="alert"`; never display stack traces to users.
- Provide friendly retry guidance for transient DB failures.

### Accessibility for Charts and Controls
- Provide text summaries for charts and use `aria-describedby` on `<canvas>` elements.
- Do not rely on color alone; include patterns/labels. Ensure keyboard navigation for filters and exports.

### Performance and Caching
```csharp
// Static files caching
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=31536000";
    }
});

// Cache common aggregates (example)
public async Task<DashboardData> GetDashboardDataAsync(string role, DateRange range, string category = null, int? ppraCycle = null)
{
    var cacheKey = $"dash:{role}:{range}:{category}:{ppraCycle}";
    return await _cache.GetOrCreateAsync(cacheKey, async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
        // ... compute and return ...
    });
}
```

### Process Compliance (Feature Analysis Docs)
- `FeatureAnalysis_YYYYMMDD_AuthAndRegistration.md`
- `FeatureAnalysis_YYYYMMDD_AdminApproval.md`
- `FeatureAnalysis_YYYYMMDD_DashboardsReporting.md`

#### View Implementation
```html
<!-- Register.cshtml -->
@section Styles {
    <link href="~/css/modules/authentication.css" rel="stylesheet" />
}

<div class="container">
    <div class="row justify-content-center">
        <div class="col-xl-10 col-lg-12 col-md-9">
            <div class="card o-hidden border-0 shadow-lg my-5">
                <div class="card-body p-0">
                    <div class="row">
                        <div class="col-lg-6 d-none d-lg-block bg-register-image"></div>
                        <div class="col-lg-6">
                            <div class="p-5">
                                <div class="auth-form__header">
                                    <h1 class="auth-form__title">Create an Account</h1>
                                </div>
                                <form class="auth-form" asp-action="Register" method="post">
                                    <div asp-validation-summary="All" class="text-danger"></div>
                                    
                                    <div class="form-group">
                                        <label asp-for="FullName">
                                            Full Name <span class="required-indicator">*</span>
                                        </label>
                                        <input asp-for="FullName" class="form-control" 
                                               placeholder="Enter your full name" required />
                                        <span asp-validation-for="FullName" class="text-danger"></span>
                                    </div>
                                    
                                    <div class="form-group">
                                        <label asp-for="Email">
                                            Email Address <span class="required-indicator">*</span>
                                        </label>
                                        <input asp-for="Email" class="form-control" 
                                               placeholder="Enter email address" required />
                                        <span asp-validation-for="Email" class="text-danger"></span>
                                    </div>
                                    
                                    <div class="form-group">
                                        <label asp-for="Address">
                                            Address <span class="required-indicator">*</span>
                                        </label>
                                        <input asp-for="Address" class="form-control" 
                                               placeholder="Enter your address" required />
                                        <span asp-validation-for="Address" class="text-danger"></span>
                                    </div>
                                    
                                    <div class="form-group">
                                        <label asp-for="RequestedRole">
                                            Role Request <span class="required-indicator">*</span>
                                        </label>
                                        <select asp-for="RequestedRole" 
                                                asp-items="Model.Roles" 
                                                class="form-control registration-form__role-select" required>
                                            <option value="">Select a role</option>
                                        </select>
                                        <span asp-validation-for="RequestedRole" class="text-danger"></span>
                                    </div>
                                    
                                    @* Additional fields: Organization, ID Number, Phone, Address *@
                                    
                                    <button type="submit" class="btn btn-primary btn-block">
                                        Register Account
                                    </button>
                                </form>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>
```

### Dashboard Module

#### Implementation Architecture
```csharp
// DashboardController.cs
[Authorize]
public class DashboardController : BaseController
{
    private readonly IDashboardService _dashboardService;
    
    public async Task<IActionResult> Index(DashboardFilterModel filter)
    {
        var userRole = User.GetRole();
        var dashboardData = await _dashboardService.GetDashboardDataAsync(userRole, filter);
        
        var viewModel = new DashboardViewModel
        {
            Metrics = dashboardData.Metrics,
            Charts = dashboardData.Charts,
            Filter = filter,
            UserRole = userRole
        };
        
        return View($"~/Views/Dashboard/{userRole}Dashboard.cshtml", viewModel);
    }
}
```

#### Dashboard View Structure
```html
<!-- CharterAdminDashboard.cshtml -->
@section Styles {
    <link href="~/css/modules/dashboard.css" rel="stylesheet" />
}

<div class="container-fluid">
    <!-- Page Heading -->
    <div class="d-sm-flex align-items-center justify-content-between mb-4">
        <h1 class="h3 mb-0 text-gray-800">Charter Admin Dashboard</h1>
        <button class="btn btn-export" onclick="exportDashboard()">
            <i class="fas fa-download fa-sm text-white-50"></i> Export Report
        </button>
    </div>
    
    <!-- Filter Panel -->
    <div class="filter-panel">
        <form asp-action="Index" method="get" class="form-inline">
            <div class="form-group mr-3">
                <label for="dateRange" class="sr-only">Date Range</label>
                <select name="dateRange" class="form-control">
                    <option value="7">Last 7 Days</option>
                    <option value="30">Last 30 Days</option>
                    <option value="90">Last 90 Days</option>
                    <option value="365">Last Year</option>
                    <option value="custom">Custom Range</option>
                </select>
            </div>
            
            <div class="form-group mr-3">
                <label for="category" class="sr-only">Course Category</label>
                <select name="category" class="form-control">
                    <option value="">All Categories</option>
                    @foreach (var cat in Model.Categories)
                    {
                        <option value="@cat.Id">@cat.Name</option>
                    }
                </select>
            </div>
            
            @if (Model.UserRole == "PPRA-Admin")
            {
                <div class="form-group mr-3">
                    <label for="ppraCycle" class="sr-only">PPRA Cycle</label>
                    <select name="ppraCycle" class="form-control">
                        @foreach (var yr in Model.AvailableCycles)
                        {
                            <option value="@yr">@yr</option>
                        }
                    </select>
                </div>
            }
            
            <button type="submit" class="btn btn-primary">
                <i class="fas fa-filter"></i> Apply Filters
            </button>
        </form>
    </div>
    
    <!-- Metrics Row -->
    <div class="dashboard-grid">
        @foreach (var metric in Model.Metrics)
        {
            <div class="card metric-card">
                <div class="card-body">
                    <div class="row no-gutters align-items-center">
                        <div class="col mr-2">
                            <div class="text-xs font-weight-bold text-primary text-uppercase mb-1">
                                @metric.Label
                            </div>
                            <div class="metric-card__value">@metric.Value</div>
                        </div>
                        <div class="col-auto">
                            <i class="@metric.Icon fa-2x text-gray-300"></i>
                        </div>
                    </div>
                </div>
            </div>
        }
    </div>
    
    <!-- Charts Row -->
    <div class="row mt-4">
        <div class="col-xl-8 col-lg-7">
            <div class="card shadow mb-4">
                <div class="card-header py-3">
                    <h6 class="m-0 font-weight-bold text-primary">Enrollment Trends</h6>
                </div>
                <div class="card-body">
                    <div class="chart-container">
                        <canvas id="enrollmentChart"></canvas>
                    </div>
                </div>
            </div>
        </div>
        
        <div class="col-xl-4 col-lg-5">
            <div class="card shadow mb-4">
                <div class="card-header py-3">
                    <h6 class="m-0 font-weight-bold text-primary">Course Distribution</h6>
                </div>
                <div class="card-body">
                    <div class="chart-container">
                        <canvas id="distributionChart"></canvas>
                    </div>
                </div>
            </div>
        </div>
    </div>
    
    <!-- Funnel Analysis -->
    <div class="row">
        <div class="col-12">
            <div class="card shadow mb-4">
                <div class="card-header py-3">
                    <h6 class="m-0 font-weight-bold text-primary">Conversion Funnel</h6>
                </div>
                <div class="card-body">
                    @await Html.PartialAsync("_ConversionFunnel", Model.FunnelData)
                </div>
            </div>
        </div>
    </div>
</div>

@section Scripts {
    <script src="~/js/modules/dashboard.js"></script>
    <script>
        // Initialize charts with data
        initializeCharts(@Html.Raw(Json.Serialize(Model.ChartData)));
    </script>
}
```

#### Export Endpoints
```csharp
// DashboardController.cs (export actions)
[Authorize]
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ExportCsv(DashboardFilterModel filter)
{
    var bytes = await _reportingService.ExportCsvAsync(User, filter);
    _auditService.LogEvent(User, "ExportCSV", new { filter });
    return File(bytes, "text/csv", "report.csv");
}

[Authorize]
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ExportXlsx(DashboardFilterModel filter)
{
    var bytes = await _reportingService.ExportXlsxAsync(User, filter);
    _auditService.LogEvent(User, "ExportXLSX", new { filter });
    return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "report.xlsx");
}
```

#### Filter Model Update (PPRA cycle)
```csharp
// DashboardFilterModel.cs
public class DashboardFilterModel
{
    public string DateRange { get; set; }
    public string Category { get; set; }
    public int? PpraCycle { get; set; } // Only applied for PPRA role
}

// ReportRepository.cs usage (apply when role == PPRA-Admin)
if (role == "PPRA-Admin" && filter.PpraCycle.HasValue)
{
    enrollmentQuery = enrollmentQuery.Where(e => e.Year == filter.PpraCycle.Value);
}
```

## 4. Security Implementation

### BaseController Security Pattern
```csharp
public abstract class BaseController : Controller
{
    protected readonly ISecurityValidationService _securityService;
    protected readonly IAuditService _auditService;
    
    protected BaseController(
        ISecurityValidationService securityService,
        IAuditService auditService)
    {
        _securityService = securityService;
        _auditService = auditService;
    }
    
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // Validate user permissions
        if (!_securityService.ValidateUserAccess(User, context))
        {
            context.Result = new ForbidResult();
            _auditService.LogUnauthorizedAccess(User, context);
            return;
        }
        
        // Validate input data
        if (!ModelState.IsValid)
        {
            _auditService.LogInvalidInput(User, context);
        }
        
        base.OnActionExecuting(context);
    }
}
```

### Security Middleware Configuration
```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Security headers
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

// Authentication & Authorization
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CharterAdmin", policy => 
        policy.RequireRole("Charter-Admin"));
    options.AddPolicy("RebosaAdmin", policy => 
        policy.RequireRole("Rebosa-Admin"));
    options.AddPolicy("PPRAAdmin", policy => 
        policy.RequireRole("PPRA-Admin"));
});

// Data Protection
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"./keys"))
    .SetApplicationName("CharterReporterApp");

var app = builder.Build();

// Security middleware pipeline
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.Add("Content-Security-Policy",
        "default-src 'self'; script-src 'self' cdn.jsdelivr.net 'sha256-...'; style-src 'self' 'sha256-...' fonts.googleapis.com; font-src 'self' fonts.gstatic.com; img-src 'self' data:; frame-ancestors 'none'");
    await next();
});
app.UseSecurityHeaders();
app.UseHsts();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseGlobalExceptionHandler();
app.UseAuditLogging();
```

### Input Validation Pattern
```csharp
// RegisterViewModel.cs
public class RegisterViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(100, MinimumLength = 2)]
    [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "Invalid name format")]
    public string FullName { get; set; }
    
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(255)]
    public string Email { get; set; }
    
    [Required(ErrorMessage = "Organization is required")]
    [StringLength(200)]
    public string Organization { get; set; }
    
    [Required(ErrorMessage = "Role selection is required")]
    public string RequestedRole { get; set; }
    
    [Required(ErrorMessage = "ID number is required")]
    [RegularExpression(@"^\d{13}$", ErrorMessage = "ID must be 13 digits")]
    public string IdNumber { get; set; }
    
    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Invalid phone number")]
    public string PhoneNumber { get; set; }
    
    [Required, StringLength(300)]
    public string Address { get; set; }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Custom validation logic
        var allowedRoles = new[] { "Charter-Admin", "Rebosa-Admin", "PPRA-Admin" };
        if (!allowedRoles.Contains(RequestedRole))
        {
            yield return new ValidationResult(
                "Invalid role selection",
                new[] { nameof(RequestedRole) });
        }
    }
}
```

## 5. Database Architecture

### Repository Pattern Implementation
```csharp
// IUserRepository.cs
public interface IUserRepository
{
    Task<User> GetByIdAsync(Guid id);
    Task<User> GetByEmailAsync(string email);
    Task<IEnumerable<RegistrationRequest>> GetPendingRegistrationsAsync();
    Task<bool> CreateRegistrationRequestAsync(RegistrationRequest request);
    Task<bool> ApproveRegistrationAsync(Guid requestId, string approvedBy);
    Task<bool> RejectRegistrationAsync(Guid requestId, string rejectedBy, string reason);
}

// UserRepository.cs
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;
    
    public async Task<IEnumerable<RegistrationRequest>> GetPendingRegistrationsAsync()
    {
        return await _context.RegistrationRequests
            .Where(r => r.Status == RegistrationStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }
    
    public async Task<bool> ApproveRegistrationAsync(Guid requestId, string approvedBy)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var request = await _context.RegistrationRequests
                .FirstOrDefaultAsync(r => r.Id == requestId);
                
            if (request == null || request.Status != RegistrationStatus.Pending)
                return false;
                
            // Create user
            var user = new User
            {
                Email = request.Email,
                FullName = request.FullName,
                Role = request.RequestedRole,
                IsActive = true
            };
            
            _context.Users.Add(user);
            
            // Update request
            request.Status = RegistrationStatus.Approved;
            request.ApprovedBy = approvedBy;
            request.ApprovedAt = DateTime.UtcNow;
            
            // Audit log
            await _auditService.LogRegistrationApprovalAsync(request, approvedBy);
            
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
```

### Data Access for Reporting
```csharp
// ReportRepository.cs
public class ReportRepository : IReportRepository
{
    private readonly MoodleDbContext _moodleContext;
    private readonly WooCommerceDbContext _wooContext;
    
    public async Task<DashboardData> GetDashboardDataAsync(
        string role, 
        DateRange dateRange, 
        string category = null)
    {
        var dashboardData = new DashboardData();
        
        // Get enrollment data from Moodle
        var enrollmentQuery = _moodleContext.Enrollments
            .Where(e => e.TimeCreated >= dateRange.StartDate && 
                       e.TimeCreated <= dateRange.EndDate);
                       
        if (!string.IsNullOrEmpty(category))
        {
            enrollmentQuery = enrollmentQuery
                .Where(e => e.Course.Category.Name == category);
        }
        
        // Role-based filtering
        if (role == "PPRA-Admin")
        {
            enrollmentQuery = enrollmentQuery
                .Where(e => e.Course.Category.Name.Contains("CPD"));
        }
        
        dashboardData.TotalEnrollments = await enrollmentQuery.CountAsync();
        
        // Get completion data
        dashboardData.TotalCompletions = await _moodleContext.Completions
            .Where(c => c.TimeCompleted >= dateRange.StartDate &&
                       c.TimeCompleted <= dateRange.EndDate)
            .CountAsync();
            
        // Get sales data from WooCommerce (Charter-Admin only)
        if (role == "Charter-Admin")
        {
            dashboardData.TotalSales = await _wooContext.Orders
                .Where(o => o.Status == "completed" &&
                           o.DateCreated >= dateRange.StartDate &&
                           o.DateCreated <= dateRange.EndDate)
                .SumAsync(o => o.Total);
        }
        
        return dashboardData;
    }
    
    public async Task<FunnelData> GetConversionFunnelAsync(DateRange dateRange)
    {
        // Implement funnel analysis: Site visits → Product views → Cart adds → Purchases → Enrollments → Completions
        // Recommend using a read-optimized SQL View or materialized table joined across WooCommerce and Moodle for performance.
        var funnelData = new FunnelData();
        // ... populate funnelData from optimized view (e.g., vw_FunnelDailyAggregates) ...
        return funnelData;
    }
}
```

## 6. Phase-wise Implementation

### Phase 1: Foundation (Week 1-2)
1. **Project Setup**
   - Create solution structure
   - Configure SB Admin 2 integration
   - Set up CSS hierarchy
   - Configure security middleware

2. **Authentication Module**
   - User registration form
   - Email verification
   - Login/logout functionality
   - Password recovery

3. **Base Infrastructure**
   - BaseController implementation
   - Security validation service
   - Audit logging service
   - Global exception handling

### Phase 2: Core Features (Week 3-4)
1. **Admin Approval Workflow**
   - Approval queue interface
   - Email notifications
   - Audit trail implementation

2. **Dashboard Foundation**
   - Role-based dashboard routing
   - Basic metric cards
   - Chart integration setup

3. **Database Integration**
   - Repository pattern implementation
   - Moodle database connection
   - WooCommerce database connection

### Phase 3: Advanced Features (Week 5-6)
1. **Advanced Dashboards**
   - Interactive charts
   - Date range filtering
   - Category filtering
   - Export functionality

2. **Reporting Module**
   - Report generation
   - XLSX/CSV export
   - Role-based data filtering

3. **Funnel Analysis**
   - Conversion funnel implementation
   - Drop-off rate calculations
   - Visual funnel representation

### Phase 4: Polish & Security (Week 7-8)
1. **Security Hardening**
   - OWASP compliance audit
   - Penetration testing
   - Performance optimization

2. **UI/UX Refinement**
   - WCAG 2.1 AA compliance
   - Mobile responsiveness testing
   - Cross-browser compatibility

3. **Documentation & Deployment**
   - User documentation
   - Deployment guides
   - Admin training materials

## 7. Quality Assurance

### Testing Strategy
```csharp
// Unit Test Example
[TestClass]
public class RegistrationServiceTests
{
    [TestMethod]
    public async Task RegisterUser_ValidData_CreatesRegistrationRequest()
    {
        // Arrange
        var service = new RegistrationService(_mockRepo.Object, _mockEmail.Object);
        var model = new RegisterViewModel
        {
            FullName = "Test User",
            Email = "test@example.com",
            RequestedRole = "PPRA-Admin"
        };
        
        // Act
        var result = await service.RegisterUserAsync(model);
        
        // Assert
        Assert.IsTrue(result.Succeeded);
        _mockRepo.Verify(r => r.CreateRegistrationRequestAsync(It.IsAny<RegistrationRequest>()), Times.Once);
        _mockEmail.Verify(e => e.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
}
```

### Security Checklist
- [ ] All inputs validated server-side
- [ ] Parameterized queries used exclusively
- [ ] XSS prevention through output encoding
- [ ] CSRF tokens on all forms
- [ ] Secure session management
- [ ] Role-based access control enforced
- [ ] Audit logging implemented
- [ ] Security headers configured
- [ ] HTTPS enforced
- [ ] Password policy enforced

### Performance Benchmarks
- Page load time < 2 seconds
- Dashboard refresh < 3 seconds
- Export generation < 5 seconds
- 100 concurrent users supported
- Query optimization verified
- Caching strategy implemented

### Accessibility Compliance
- [ ] WCAG 2.1 AA compliant
- [ ] Keyboard navigation tested
- [ ] Screen reader compatible
- [ ] 4.5:1 contrast ratios
- [ ] Focus indicators visible
- [ ] Form labels present
- [ ] Error messages clear
- [ ] Alternative text provided

## Conclusion

This implementation plan provides a bulletproof approach to building the Charter reporter app with:

1. **Proper CSS Hierarchy**: SB Admin 2 → Site overrides → Module-specific styles
2. **Security First**: OWASP compliance, role-based access, comprehensive audit trails
3. **Clean Architecture**: SOLID principles, repository pattern, service layer
4. **User Story Adherence**: All acceptance criteria addressed
5. **Accessibility**: WCAG 2.1 AA compliant, mobile-first design
6. **Performance**: Optimized queries, caching, async operations

The modular structure ensures maintainability while the phased approach allows for iterative development with regular quality checkpoints.
