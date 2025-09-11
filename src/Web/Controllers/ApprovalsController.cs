using Charter.Reporter.Domain.Policies;
using Charter.Reporter.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Charter.Reporter.Domain.Approvals;
using Charter.Reporter.Application.Services.Users;
using Charter.Reporter.Infrastructure.Identity;
using Charter.Reporter.Web.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Charter.Reporter.Web.Controllers;

[Authorize(Policy = AppPolicies.RequireCharterAdmin)]
public class ApprovalsController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserManagementService _userManagementService;

    public ApprovalsController(
        AppDbContext db, 
        UserManager<ApplicationUser> userManager,
        IUserManagementService userManagementService)
    {
        _db = db;
        _userManager = userManager;
        _userManagementService = userManagementService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var pending = await _db.ApprovalRequests
            .Where(a => a.Status == ApprovalStatus.Pending)
            .OrderBy(a => a.CreatedUtc)
            .ToListAsync();
        
        // Also get all users for management
        var usersResult = await _userManagementService.GetAllUsersAsync();
        ViewBag.AllUsers = usersResult.IsSuccess ? usersResult.Value : new List<UserListVm>();
        ViewBag.AvailableRoles = Domain.Roles.AppRoles.All;
        
        return View(pending);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id)
    {
        var req = await _db.ApprovalRequests.FindAsync(id);
        if (req == null) return NotFound();
        req.Status = ApprovalStatus.Approved;
        req.DecidedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        // Assign role if user exists
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user != null && !await _userManager.IsInRoleAsync(user, req.RequestedRole))
        {
            await _userManager.AddToRoleAsync(user, req.RequestedRole);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid id, string reason)
    {
        var req = await _db.ApprovalRequests.FindAsync(id);
        if (req == null) return NotFound();
        req.Status = ApprovalStatus.Rejected;
        req.DecisionReason = reason;
        req.DecidedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // User Management AJAX Endpoints
    
    [HttpGet]
    public async Task<IActionResult> GetUserDetails(string id)
    {
        var result = await _userManagementService.GetUserDetailsAsync(id);
        if (result.IsSuccess)
        {
            return Json(new { success = true, user = result.Value });
        }
        return Json(new { success = false, message = result.ErrorMessage });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser([FromBody] UserCreateVm model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { success = false, message = string.Join(", ", errors) });
        }

        var result = await _userManagementService.CreateUserAsync(model);
        if (result.IsSuccess)
        {
            this.AddSuccessNotification($"User {result.Value?.Email} created successfully", "User Created");
            return Json(new { success = true, user = result.Value });
        }
        
        return Json(new { success = false, message = result.ErrorMessage });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUser([FromBody] UserEditVm model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { success = false, message = string.Join(", ", errors) });
        }

        var result = await _userManagementService.UpdateUserAsync(model);
        if (result.IsSuccess)
        {
            this.AddSuccessNotification($"User {result.Value?.Email} updated successfully", "User Updated");
            return Json(new { success = true, user = result.Value });
        }
        
        return Json(new { success = false, message = result.ErrorMessage });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var result = await _userManagementService.ResetPasswordAsync(id);
        if (result.IsSuccess)
        {
            this.AddSuccessNotification($"Password reset for {result.Value?.Email}", "Password Reset");
            return Json(new { 
                success = true, 
                email = result.Value?.Email,
                tempPassword = result.Value?.TempPassword,
                userName = result.Value?.UserName
            });
        }
        
        return Json(new { success = false, message = result.ErrorMessage });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLockout(string id, bool locked)
    {
        var result = await _userManagementService.SetLockoutAsync(id, locked);
        if (result.IsSuccess)
        {
            var action = locked ? "locked" : "unlocked";
            this.AddSuccessNotification($"User account {action} successfully", "Account Status Updated");
            return Json(new { success = true, locked = locked });
        }
        
        return Json(new { success = false, message = result.ErrorMessage });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveWithEdit([FromBody] ApproveWithEditVm model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Invalid data provided" });
        }

        var req = await _db.ApprovalRequests.FindAsync(model.ApprovalId);
        if (req == null) 
            return Json(new { success = false, message = "Approval request not found" });

        // Find the user
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user == null)
        {
            return Json(new { success = false, message = "User account not found for this approval. Ask the user to register first." });
        }

        // Update user details if provided
        if (model.UserEdit != null)
        {
            // Ensure we update the correct user id
            model.UserEdit.Id = user.Id;
            // If email is omitted in the inline edit, keep existing email
            model.UserEdit.Email = string.IsNullOrWhiteSpace(model.UserEdit.Email) ? (user.Email ?? string.Empty) : model.UserEdit.Email;

            var updateResult = await _userManagementService.UpdateUserAsync(model.UserEdit);
            if (!updateResult.IsSuccess)
            {
                return Json(new { success = false, message = updateResult.ErrorMessage });
            }
        }

        // Assign role
        if (!await _userManager.IsInRoleAsync(user, req.RequestedRole))
        {
            await _userManager.AddToRoleAsync(user, req.RequestedRole);
        }

        // Approve the request
        req.Status = ApprovalStatus.Approved;
        req.DecidedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        this.AddSuccessNotification($"User {req.Email} approved successfully", "User Approved");
        return Json(new { success = true });
    }

    public class ApproveWithEditVm
    {
        public Guid ApprovalId { get; set; }
        public UserEditVm? UserEdit { get; set; }
    }
}


