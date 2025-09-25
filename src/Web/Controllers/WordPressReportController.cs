using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Charter.Reporter.Application.Services.Dashboard;
using Charter.Reporter.Domain.Roles;
using Charter.Reporter.Domain.Policies;
using OfficeOpenXml;

namespace Charter.Reporter.Web.Controllers;

[Authorize(Policy = AppPolicies.RequireAnyAdmin)]
public class WordPressReportController : Controller
{
    private readonly IWordPressReportService _wordPressReportService;

    public WordPressReportController(IWordPressReportService wordPressReportService)
    {
        _wordPressReportService = wordPressReportService;
    }

    /// <summary>
    /// Get WordPress report data with server-side filtering, sorting, and searching
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetReport(
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
        try
        {
            // Only Charter or Rebosa Admins can access WordPress reports
            if (!(User.IsInRole(AppRoles.CharterAdmin) || User.IsInRole(AppRoles.RebosaAdmin)))
            {
                return Forbid("Only Charter or Rebosa Admins are authorized to access WordPress reports.");
            }

            ComputeDateRange(preset, ref from, ref to);
            
            var result = await _wordPressReportService.GetWordPressReportAsync(
                from, to, categoryId, search, sortColumn, sortDesc, 
                page, pageSize, showOnlyFourthCompletion, cancellationToken);
            
            return Json(new
            {
                items = result.Items,
                totalCount = result.TotalCount,
                page = result.Page,
                pageSize = result.PageSize,
                showOnlyFourthCompletion
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Failed to retrieve WordPress report: " + ex.Message });
        }
    }

    /// <summary>
    /// Get WordPress course categories for filtering
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        try
        {
            if (!User.IsInRole(AppRoles.CharterAdmin))
            {
                return Forbid("Only Charter Admins can access category information.");
            }

            var categories = await _wordPressReportService.GetWordPressCategoriesAsync(cancellationToken);
            return Json(categories);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Failed to retrieve categories: " + ex.Message });
        }
    }

    /// <summary>
    /// Get available columns for export functionality
    /// </summary>
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

    /// <summary>
    /// Export WordPress report to Excel
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ExportExcel(
        [FromForm] string[] selectedColumns,
        [FromForm] DateTime? fromUtc,
        [FromForm] DateTime? toUtc,
        [FromForm] long? courseCategoryId,
        [FromForm] string? search,
        [FromForm] bool showOnlyFourthCompletion = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Only Charter or Rebosa Admins can export Excel reports
            if (!(User.IsInRole(AppRoles.CharterAdmin) || User.IsInRole(AppRoles.RebosaAdmin)))
            {
                return Forbid("Only Charter or Rebosa Admins are authorized to export Excel reports.");
            }

            // First, get the total count to ensure we export all rows
            var totalCountData = await _wordPressReportService.GetWordPressReportAsync(
                fromUtc, toUtc, courseCategoryId, search, 
                sortColumn: "lastname", sortDesc: false, 
                page: 1, pageSize: 1, showOnlyFourthCompletion,
                cancellationToken);
            
            var totalRows = totalCountData.TotalCount;
            // Limit export to 50,000 rows for performance
            var rowsToFetch = Math.Min(totalRows, 50000);
            
            // Now fetch all data for export
            var allData = await _wordPressReportService.GetWordPressReportAsync(
                fromUtc, toUtc, courseCategoryId, search, 
                sortColumn: "lastname", sortDesc: false, 
                page: 1, pageSize: rowsToFetch, showOnlyFourthCompletion,
                cancellationToken);

            var requestedColumns = selectedColumns?.ToList() ?? new List<string>();
            if (!requestedColumns.Any())
            {
                // Default to all columns if none selected
                requestedColumns = new List<string> 
                { 
                    "LastName", "FirstName", "Email", "PhoneNumber", "PpraNo", "IdNo", 
                    "Province", "Agency", "CourseName", "Category", "EnrolmentDate", 
                    "CompletionDate", "FourthCompletionDate" 
                };
            }

            // Generate Excel using the existing service (we'll need to extend it for WordPress data)
            var fileName = $"WordPress_Report_{DateTime.Now:yyyyMMdd_HHmmss}";
            var excelBytes = await GenerateWordPressExcelAsync(allData.Items, requestedColumns);

            Response.Headers["Content-Disposition"] = $"attachment; filename={fileName}.xlsx";
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Export failed: " + ex.Message });
        }
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
    }

    private static async Task<byte[]> GenerateWordPressExcelAsync(IReadOnlyList<WordPressReportRow> data, IReadOnlyList<string> selectedColumns)
    {
        // Set EPPlus license context for non-commercial use
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        
        // For now, we'll implement a basic Excel generation
        // In production, you might want to extend the existing ExcelExportService to handle WordPressReportRow
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("WordPress Report");

        // Define column mappings
        var columnMappings = new Dictionary<string, (string DisplayName, Func<WordPressReportRow, object?> ValueSelector)>
        {
            ["LastName"] = ("Last Name", r => r.LastName),
            ["FirstName"] = ("First Name", r => r.FirstName),
            ["Email"] = ("Email", r => r.Email),
            ["PhoneNumber"] = ("Phone Number", r => r.PhoneNumber),
            ["PpraNo"] = ("PPRA No", r => r.PpraNo),
            ["IdNo"] = ("ID No", r => r.IdNo),
            ["Province"] = ("Province", r => r.Province),
            ["Agency"] = ("Agency", r => r.Agency),
            ["CourseName"] = ("Course Name", r => r.CourseName),
            ["Category"] = ("Category", r => r.Category),
            ["EnrolmentDate"] = ("Enrolment Date", r => r.EnrolmentDate),
            ["CompletionDate"] = ("Completion Date", r => r.CompletionDate),
            ["FourthCompletionDate"] = ("4th Completion Date", r => r.FourthCompletionDate)
        };

        // Filter to only selected columns that exist in our mappings
        var validColumns = selectedColumns
            .Where(c => columnMappings.ContainsKey(c))
            .ToList();

        if (!validColumns.Any())
        {
            validColumns = columnMappings.Keys.ToList();
        }

        // Add headers
        for (int i = 0; i < validColumns.Count; i++)
        {
            worksheet.Cells[1, i + 1].Value = columnMappings[validColumns[i]].DisplayName;
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
        }

        // Add data rows
        for (int row = 0; row < data.Count; row++)
        {
            var item = data[row];
            for (int col = 0; col < validColumns.Count; col++)
            {
                var column = validColumns[col];
                var value = columnMappings[column].ValueSelector(item);
                
                if (value is DateTime dt)
                {
                    worksheet.Cells[row + 2, col + 1].Value = dt;
                    worksheet.Cells[row + 2, col + 1].Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
                }
                else
                {
                    worksheet.Cells[row + 2, col + 1].Value = value?.ToString() ?? "";
                }
            }
        }

        worksheet.Cells.AutoFitColumns();
        return await package.GetAsByteArrayAsync();
    }
}
