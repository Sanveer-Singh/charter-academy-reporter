using Charter.Reporter.Domain.Policies;
using Charter.Reporter.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Charter.Reporter.Domain.Approvals;

namespace Charter.Reporter.Web.Controllers;

[Authorize(Policy = AppPolicies.RequireCharterAdmin)]
public class ApprovalsController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<Charter.Reporter.Infrastructure.Identity.ApplicationUser> _userManager;

    public ApprovalsController(AppDbContext db, UserManager<Charter.Reporter.Infrastructure.Identity.ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var pending = _db.ApprovalRequests.Where(a => a.Status == ApprovalStatus.Pending)
            .OrderBy(a => a.CreatedUtc)
            .ToList();
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
}


