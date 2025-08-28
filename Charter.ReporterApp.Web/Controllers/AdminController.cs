using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Charter.ReporterApp.Web.Controllers
{
    [Authorize(Policy = "CharterAdmin")]
    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;

        public AdminController(ILogger<AdminController> logger)
        {
            _logger = logger;
        }

        public IActionResult Approvals()
        {
            // TODO: Get actual pending registrations from database
            var pendingRequests = new List<object>
            {
                new
                {
                    Id = "req-001",
                    FullName = "John Doe",
                    Email = "john.doe@example.com",
                    Organization = "ABC Corporation",
                    RequestedRole = "Charter-Admin",
                    IdNumber = "9001015800084",
                    PhoneNumber = "+27 11 123 4567",
                    CreatedAt = DateTime.Now.AddHours(-2)
                }
            };

            ViewData["PendingCount"] = pendingRequests.Count;
            ViewData["ApprovedTodayCount"] = 12;
            
            return View(pendingRequests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveRequest(string id)
        {
            try
            {
                // TODO: Implement actual approval logic
                _logger.LogInformation("Registration {RequestId} approved by {User}", 
                    id, User.Identity?.Name);
                
                return Json(new { success = true, userName = "John Doe" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving registration {RequestId}", id);
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectRequest(string id, [FromBody] RejectRequestModel model)
        {
            try
            {
                // TODO: Implement actual rejection logic
                _logger.LogInformation("Registration {RequestId} rejected by {User} for reason: {Reason}", 
                    id, User.Identity?.Name, model.Reason);
                
                return Json(new { success = true, userName = "John Doe" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting registration {RequestId}", id);
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        [HttpGet]
        public IActionResult GetPendingCount()
        {
            // TODO: Get actual count from database
            return Json(new { count = 3 });
        }
    }

    public class RejectRequestModel
    {
        public string Reason { get; set; } = string.Empty;
    }
}