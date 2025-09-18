using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Charter.Reporter.Application.Services.Dashboard;
using Charter.Reporter.Web.Models;
using Charter.Reporter.Domain.Roles;

namespace Charter.Reporter.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IWordPressReportService _wordPressReportService;
    private readonly IMergedReportService _mergedReportService;

    public DashboardController(
        IDashboardService dashboardService, 
        IWordPressReportService wordPressReportService,
        IMergedReportService mergedReportService)
    {
        _dashboardService = dashboardService;
        _wordPressReportService = wordPressReportService;
        _mergedReportService = mergedReportService;
    }
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var to = DateTime.UtcNow;
        var from = to.AddDays(-90);
        var summary = await _dashboardService.GetBaselineSummaryAsync(from, to, cancellationToken);
        var isCharterAdmin = User.IsInRole(AppRoles.CharterAdmin);
        var isRebosaAdmin = User.IsInRole(AppRoles.RebosaAdmin);
        var canExport = isCharterAdmin || isRebosaAdmin;
        var categories = isCharterAdmin ? await _dashboardService.GetCourseCategoriesAsync(cancellationToken) : Array.Empty<CourseCategory>();
        var vm = new DashboardVm
        {
            SalesTotal = summary.SalesTotal,
            EnrollmentCount = summary.EnrollmentCount,
            CompletionCount = summary.CompletionCount,
            FromUtc = from,
            ToUtc = to,
            SelectedPreset = "last-3-months",
            Categories = categories,
            IsCharterAdmin = isCharterAdmin,
            CanExport = canExport,
            ShowWordPressToggle = isCharterAdmin
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Summary([FromQuery] string? preset, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] long? categoryId, CancellationToken cancellationToken)
    {
        ComputeDateRange(preset, ref from, ref to);
        var summary = await _dashboardService.GetSummaryAsync(from, to, categoryId, cancellationToken);
        return Json(new
        {
            salesTotal = summary.SalesTotal,
            enrollmentCount = summary.EnrollmentCount,
            completionCount = summary.CompletionCount
        });
    }

    [HttpGet]
    public async Task<IActionResult> MoodleReport(
        [FromQuery] string? preset,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] long? categoryId,
        [FromQuery] string? search,
        [FromQuery] string? sortColumn,
        [FromQuery] bool sortDesc,
        [FromQuery] string? reportMode,
        [FromQuery] bool showOnlyFourthCompletion = false,
        [FromQuery] bool perUser = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        ComputeDateRange(preset, ref from, ref to);
        
        // Route to appropriate service based on report mode
        if (User.IsInRole(AppRoles.CharterAdmin))
        {
            if (string.Equals(reportMode, "wordpress", StringComparison.OrdinalIgnoreCase))
            {
                var wpResult = await _wordPressReportService.GetWordPressReportAsync(from, to, categoryId, search, sortColumn, sortDesc, page, pageSize, showOnlyFourthCompletion, cancellationToken);
                return Json(new
                {
                    items = wpResult.Items,
                    totalCount = wpResult.TotalCount,
                    page = wpResult.Page,
                    pageSize = wpResult.PageSize,
                    showOnlyFourthCompletion,
                    reportMode = "wordpress"
                });
            }
            else if (string.Equals(reportMode, "both", StringComparison.OrdinalIgnoreCase))
            {
                var mergedResult = await _mergedReportService.GetMergedReportAsync(from, to, categoryId, search, sortColumn, sortDesc, perUser, page, pageSize, cancellationToken);
                return Json(new
                {
                    items = mergedResult.Items,
                    totalCount = mergedResult.TotalCount,
                    page = mergedResult.Page,
                    pageSize = mergedResult.PageSize,
                    reportMode = "both"
                });
            }
        }
        
        // Default to Moodle report
        var result = await _dashboardService.GetMoodleReportAsync(from, to, categoryId, search, sortColumn, sortDesc, perUser, page, pageSize, cancellationToken);
        return Json(new
        {
            items = result.Items,
            totalCount = result.TotalCount,
            page = result.Page,
            pageSize = result.PageSize,
            reportMode = "moodle"
        });
    }

    [HttpGet]
    public async Task<IActionResult> Categories(CancellationToken cancellationToken)
    {
        if (!User.IsInRole(AppRoles.CharterAdmin))
        {
            return Forbid();
        }
        var cats = await _dashboardService.GetCourseCategoriesAsync(cancellationToken);
        return Json(cats);
    }

    [HttpGet]
    public IActionResult GetAvailableColumns()
    {
        // Only Charter or Rebosa Admins can access column information for export
        if (!(User.IsInRole(AppRoles.CharterAdmin) || User.IsInRole(AppRoles.RebosaAdmin)))
        {
            return Forbid("Only Charter or Rebosa Admins are authorized to access export column information.");
        }
        
        var columns = new[]
        {
            new { value = "LastName", label = "Last Name" },
            new { value = "FirstName", label = "First Name" },
            new { value = "Email", label = "Email" },
            new { value = "PhoneNumber", label = "Phone Number" },
            new { value = "PpraNo", label = "PPRA No" },
            new { value = "IdNo", label = "ID No" },
            new { value = "Province", label = "Province" },
            new { value = "Agency", label = "Agency" },
            new { value = "CourseName", label = "Course Name" },
            new { value = "Category", label = "Category" },
            new { value = "EnrolmentDate", label = "Enrolment Date" },
            new { value = "CompletionDate", label = "Completion Date" },
            new { value = "FourthCompletionDate", label = "4th Completion Date" }
        };
        
        return Json(columns);
    }

    [HttpGet]
    public async Task<IActionResult> WordPressReport(
        [FromQuery] string? preset,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] long? categoryId,
        [FromQuery] string? search,
        [FromQuery] string? sortColumn,
        [FromQuery] bool sortDesc,
        [FromQuery] bool showOnlyFourthCompletion = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        // Only Charter Admins can access WordPress reports
        if (!User.IsInRole(AppRoles.CharterAdmin))
        {
            return Forbid("Only Charter Admins are authorized to access WordPress reports.");
        }

        ComputeDateRange(preset, ref from, ref to);
        var result = await _wordPressReportService.GetWordPressReportAsync(from, to, categoryId, search, sortColumn, sortDesc, page, pageSize, showOnlyFourthCompletion, cancellationToken);
        return Json(new
        {
            items = result.Items,
            totalCount = result.TotalCount,
            page = result.Page,
            pageSize = result.PageSize,
            showOnlyFourthCompletion
        });
    }

    [HttpGet]
    public async Task<IActionResult> WordPressCategories(CancellationToken cancellationToken)
    {
        if (!User.IsInRole(AppRoles.CharterAdmin))
        {
            return Forbid("Only Charter Admins can access WordPress category information.");
        }
        var categories = await _wordPressReportService.GetWordPressCategoriesAsync(cancellationToken);
        return Json(categories);
    }


    private static void ComputeDateRange(string? preset, ref DateTime? from, ref DateTime? to)
    {
        if (string.Equals(preset, "all-time", StringComparison.OrdinalIgnoreCase))
        {
            from = null;
            to = null;
            return;
        }
        var now = DateTime.UtcNow;
        if (string.Equals(preset, "last-month", StringComparison.OrdinalIgnoreCase))
        {
            var startOfThisMonthUtc = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var startOfLastMonthUtc = startOfThisMonthUtc.AddMonths(-1);
            var endOfLastMonthUtc = startOfThisMonthUtc.AddTicks(-1);
            from = startOfLastMonthUtc;
            to = endOfLastMonthUtc;
            return;
        }
        if (string.Equals(preset, "last-3-months", StringComparison.OrdinalIgnoreCase))
        {
            from = now.AddMonths(-3);
            to = now;
            return;
        }
        if (string.Equals(preset, "last-6-months", StringComparison.OrdinalIgnoreCase))
        {
            from = now.AddMonths(-6);
            to = now;
            return;
        }
        if (string.Equals(preset, "1-year", StringComparison.OrdinalIgnoreCase) || string.Equals(preset, "last-year", StringComparison.OrdinalIgnoreCase))
        {
            from = now.AddYears(-1);
            to = now;
        }
        // If preset is not provided, use custom from/to if present; otherwise leave as is.
    }
}


