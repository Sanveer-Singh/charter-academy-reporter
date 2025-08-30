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

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var to = DateTime.UtcNow;
        var from = to.AddDays(-90);
        var summary = await _dashboardService.GetBaselineSummaryAsync(from, to, cancellationToken);
        var isCharterAdmin = User.IsInRole(AppRoles.CharterAdmin);
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
            IsCharterAdmin = isCharterAdmin
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Summary([FromQuery] string? preset, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? categoryId, CancellationToken cancellationToken)
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
        [FromQuery] int? categoryId,
        [FromQuery] string? search,
        [FromQuery] string? sortColumn,
        [FromQuery] bool sortDesc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        ComputeDateRange(preset, ref from, ref to);
        var result = await _dashboardService.GetMoodleReportAsync(from, to, categoryId, search, sortColumn, sortDesc, page, pageSize, cancellationToken);
        return Json(new
        {
            items = result.Items,
            totalCount = result.TotalCount,
            page = result.Page,
            pageSize = result.PageSize
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


