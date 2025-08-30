using Charter.Reporter.Domain.Policies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Microsoft.Extensions.Options;
using Charter.Reporter.Shared.Export;

namespace Charter.Reporter.Web.Controllers;

[Authorize(Policy = AppPolicies.RequireAnyAdmin)]
public class ExportController : Controller
{
    private readonly IOptionsMonitor<ExportOptions> _options;
    private readonly IExportSafetyService _safety;
    public ExportController(IOptionsMonitor<ExportOptions> options, IExportSafetyService safety)
    {
        _options = options;
        _safety = safety;
    }
    [HttpGet]
    public IActionResult CsvSample()
    {
        var requestedColumns = new[] { "column1", "column2" };
        var decision = _safety.Evaluate("baseline", User.IsInRole("CharterAdmin") ? "CharterAdmin" : "Other", 10, requestedColumns);
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
}


