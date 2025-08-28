using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Charter.ReporterApp.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ILogger<DashboardController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var userRole = User.FindFirst("Role")?.Value ?? "Charter-Admin";
            
            ViewData["UserRole"] = userRole;
            ViewData["UserName"] = User.FindFirst("FullName")?.Value ?? User.Identity?.Name ?? "User";
            
            // Return role-specific view
            return userRole switch
            {
                "Charter-Admin" => View("CharterAdminDashboard"),
                "Rebosa-Admin" => View("RebosaAdminDashboard"),
                "PPRA-Admin" => View("PPRAAdminDashboard"),
                _ => View("CharterAdminDashboard")
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Filter([FromBody] DashboardFilterModel filter)
        {
            // TODO: Implement actual filtering logic
            return Json(new
            {
                success = true,
                metrics = new
                {
                    enrollments = new { value = 12456, change = 15.3 },
                    sales = new { value = 1245678, change = 8.7 },
                    completions = new { value = 8923, change = 22.4 },
                    completionRate = new { value = 71.6, change = -2.1 }
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Export([FromBody] ExportRequestModel request)
        {
            // TODO: Implement actual export logic
            return Json(new
            {
                success = true,
                downloadUrl = $"/api/download/dashboard-report.{request.Format}"
            });
        }
    }

    public class DashboardFilterModel
    {
        public string? DateRange { get; set; }
        public string? Category { get; set; }
        public string? Status { get; set; }
    }

    public class ExportRequestModel
    {
        public string Format { get; set; } = "csv";
        public DashboardFilterModel? Filters { get; set; }
        public Dictionary<string, bool>? Include { get; set; }
    }
}