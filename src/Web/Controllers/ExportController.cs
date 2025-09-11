using Charter.Reporter.Domain.Policies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Microsoft.Extensions.Options;
using Charter.Reporter.Shared.Export;
using Charter.Reporter.Application.Services.Dashboard;
using Charter.Reporter.Infrastructure.Services.Export;

namespace Charter.Reporter.Web.Controllers;

[Authorize(Policy = AppPolicies.RequireAnyAdmin)]
public class ExportController : Controller
{
    private readonly IOptionsMonitor<ExportOptions> _options;
    private readonly IExportSafetyService _safety;
    private readonly IDashboardService _dashboardService;
    private readonly IExcelExportService _excelExportService;
    private const string CharterAdminRole = "CharterAdmin";
    private const string RebosaAdminRole = "RebosaAdmin";
    
    public ExportController(
        IOptionsMonitor<ExportOptions> options, 
        IExportSafetyService safety,
        IDashboardService dashboardService,
        IExcelExportService excelExportService)
    {
        _options = options;
        _safety = safety;
        _dashboardService = dashboardService;
        _excelExportService = excelExportService;
    }
    [HttpGet]
    public IActionResult CsvSample()
    {
        var requestedColumns = new[] { "column1", "column2" };
        var decision = _safety.Evaluate("baseline", User.IsInRole(CharterAdminRole) ? CharterAdminRole : "Other", 10, requestedColumns);
        if (!decision.Allowed) return BadRequest(decision.Reason);

        Response.Headers["Content-Disposition"] = "attachment; filename=sample.csv";
        Response.ContentType = "text/csv";
        var rowCap = _options.CurrentValue.RowCap;
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', decision.AllowedColumns));
        var maxRows = Math.Min(10, rowCap);
        for (int i = 0; i < maxRows; i++)
        {
            sb.AppendLine($"value1-{i},value2-{i}");
        }
        var buffer = Encoding.UTF8.GetBytes(sb.ToString());
        return File(buffer, "text/csv");
    }

    // BUG: Row count mismatch between dashboard table and Excel export
    // ROOT CAUSE: Excel export was only fetching page 1 of data (limited by RowCap), 
    //             while dashboard table shows total count across all pages
    // FIX: Fetch all matching rows up to RowCap for Excel export
    // VERIFIED: Export now includes all rows shown in table (respecting RowCap limit)
    [HttpPost]
    public async Task<IActionResult> ExcelMoodleReport(
        [FromForm] string[] selectedColumns,
        [FromForm] DateTime? fromUtc,
        [FromForm] DateTime? toUtc,
        [FromForm] long? courseCategoryId,
        [FromForm] string? search,
        CancellationToken cancellationToken)
    {
        try
        {
            // Only Charter or Rebosa Admins can export Excel reports
            if (!(User.IsInRole(CharterAdminRole) || User.IsInRole(RebosaAdminRole)))
            {
                return Forbid("Only Charter or Rebosa Admins are authorized to export Excel reports.");
            }
            
            // Security check with proper role
            var role = User.IsInRole(CharterAdminRole) ? CharterAdminRole : (User.IsInRole(RebosaAdminRole) ? RebosaAdminRole : "Other");
            
            // BUG FIX: Fetch ALL data for export, not just first page
            // First, get the total count to ensure we export all rows
            var totalCountData = await _dashboardService.GetMoodleReportAsync(
                fromUtc, toUtc, courseCategoryId, search, 
                sortColumn: "lastname", sortDesc: false, 
                page: 1, pageSize: 1, // Just to get total count
                cancellationToken);
            
            var totalRows = totalCountData.TotalCount;
            var rowsToFetch = Math.Min(totalRows, _options.CurrentValue.RowCap);
            
            // Now fetch all data up to RowCap
            var allData = await _dashboardService.GetMoodleReportAsync(
                fromUtc, toUtc, courseCategoryId, search, 
                sortColumn: "lastname", sortDesc: false, 
                page: 1, pageSize: rowsToFetch, 
                cancellationToken);

            var requestedColumns = selectedColumns?.ToList() ?? new List<string>();
            if (!requestedColumns.Any())
            {
                // Default to all columns if none selected
                requestedColumns = new List<string> { "LastName", "FirstName", "Email", "PhoneNumber", "PpraNo", "IdNo", "Province", "Agency", "CourseName", "Category", "EnrolmentDate", "CompletionDate", "FourthCompletionDate" };
            }

            var decision = _safety.Evaluate("moodle-report", role, allData.Items.Count, requestedColumns);
            if (!decision.Allowed)
            {
                return BadRequest(new { error = decision.Reason });
            }

            var fileName = $"Moodle_Report_{DateTime.Now:yyyyMMdd_HHmmss}";
            var excelBytes = await _excelExportService.GenerateExcelAsync(
                allData.Items, 
                decision.AllowedColumns, 
                fileName, 
                cancellationToken);

            Response.Headers["Content-Disposition"] = $"attachment; filename={fileName}.xlsx";
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Export failed: " + ex.Message });
        }
    }

    [HttpGet]
    [AllowAnonymous] // Column metadata is safe to expose without authentication
    public IActionResult GetAvailableColumns()
    {
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


}


