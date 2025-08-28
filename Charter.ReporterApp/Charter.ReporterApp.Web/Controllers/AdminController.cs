using Charter.ReporterApp.Application.DTOs;
using Charter.ReporterApp.Application.Interfaces;
using Charter.ReporterApp.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Charter.ReporterApp.Web.Controllers;

/// <summary>
/// Admin controller for user approval and management
/// </summary>
[Authorize(Policy = "CharterAdmin")]
public class AdminController : BaseController
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;

    public AdminController(
        ILogger<AdminController> logger,
        IAuditService auditService,
        ISecurityValidationService securityService,
        IUserRepository userRepository,
        IEmailService emailService)
        : base(logger, auditService, securityService)
    {
        _userRepository = userRepository;
        _emailService = emailService;
    }

    [HttpGet]
    public async Task<IActionResult> Approvals()
    {
        try
        {
            var pendingRequests = await _userRepository.GetPendingRegistrationsAsync();
            var model = new ApprovalQueueViewModel
            {
                PendingRequests = pendingRequests.Select(r => new RegistrationRequestDto
                {
                    Id = r.Id,
                    FullName = r.FullName,
                    Email = r.Email,
                    Organization = r.Organization,
                    RequestedRole = r.RequestedRole,
                    IdNumber = r.IdNumber,
                    PhoneNumber = r.PhoneNumber,
                    Address = r.Address,
                    Status = r.Status.ToString(),
                    CreatedAt = r.CreatedAt,
                    RejectionReason = r.RejectionReason
                }).ToList(),
                PendingCount = pendingRequests.Count(),
                ApprovedTodayCount = await GetApprovedTodayCountAsync()
            };

            return View(model);
        }
        catch (Exception ex)
        {
            return await HandleExceptionAsync(ex, "Approvals");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveRequest(Guid id)
    {
        try
        {
            var request = await _userRepository.GetRegistrationByIdAsync(id);
            if (request == null)
            {
                return JsonError("Registration request not found.");
            }

            var success = await _userRepository.ApproveRegistrationAsync(id, GetUserName());
            if (success)
            {
                // Send approval email
                await _emailService.SendApprovalNotificationAsync(request.Email, request.FullName);
                
                await LogUserActionAsync("RegistrationApproved", new { 
                    RequestId = id,
                    UserEmail = request.Email,
                    RequestedRole = request.RequestedRole 
                });

                return JsonSuccess(new { id, userName = request.FullName }, "Registration approved successfully");
            }

            return JsonError("Failed to approve registration. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving registration {RequestId}", id);
            return JsonError("An error occurred while approving the registration.");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectRequest(Guid id, [FromBody] RejectRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            return JsonError("Rejection reason is required.");
        }

        try
        {
            var request = await _userRepository.GetRegistrationByIdAsync(id);
            if (request == null)
            {
                return JsonError("Registration request not found.");
            }

            var success = await _userRepository.RejectRegistrationAsync(id, GetUserName(), dto.Reason);
            if (success)
            {
                // Send rejection email
                await _emailService.SendRejectionNotificationAsync(request.Email, request.FullName, dto.Reason);
                
                await LogUserActionAsync("RegistrationRejected", new { 
                    RequestId = id,
                    UserEmail = request.Email,
                    RequestedRole = request.RequestedRole,
                    Reason = dto.Reason 
                });

                return JsonSuccess(new { id, userName = request.FullName }, "Registration rejected successfully");
            }

            return JsonError("Failed to reject registration. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting registration {RequestId}", id);
            return JsonError("An error occurred while rejecting the registration.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> RegistrationDetails(Guid id)
    {
        try
        {
            var request = await _userRepository.GetRegistrationByIdAsync(id);
            if (request == null)
            {
                SetErrorMessage("Registration request not found.");
                return RedirectToAction("Approvals");
            }

            var model = new RegistrationRequestDto
            {
                Id = request.Id,
                FullName = request.FullName,
                Email = request.Email,
                Organization = request.Organization,
                RequestedRole = request.RequestedRole,
                IdNumber = request.IdNumber,
                PhoneNumber = request.PhoneNumber,
                Address = request.Address,
                Status = request.Status.ToString(),
                CreatedAt = request.CreatedAt,
                RejectionReason = request.RejectionReason
            };

            return View(model);
        }
        catch (Exception ex)
        {
            return await HandleExceptionAsync(ex, "RegistrationDetails");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetPendingCount()
    {
        try
        {
            var count = await _userRepository.GetPendingRegistrationCountAsync();
            return Json(new { count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending registration count");
            return Json(new { count = 0 });
        }
    }

    [HttpGet]
    public async Task<IActionResult> UserManagement()
    {
        try
        {
            var users = await _userRepository.GetAllAsync();
            var model = users.Select(u => new UserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Organization = u.Organization,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            }).ToList();

            return View(model);
        }
        catch (Exception ex)
        {
            return await HandleExceptionAsync(ex, "UserManagement");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserStatus(string userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return JsonError("User not found.");
            }

            bool success;
            string action;
            
            if (user.IsActive)
            {
                success = await _userRepository.DeactivateAsync(userId, GetUserName());
                action = "deactivated";
            }
            else
            {
                success = await _userRepository.ActivateAsync(userId, GetUserName());
                action = "activated";
            }

            if (success)
            {
                await LogUserActionAsync($"User{(user.IsActive ? "Deactivated" : "Activated")}", new { 
                    UserId = userId,
                    UserEmail = user.Email 
                });

                return JsonSuccess(new { userId, newStatus = !user.IsActive }, $"User {action} successfully");
            }

            return JsonError($"Failed to {action.ToLower()} user. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling user status for {UserId}", userId);
            return JsonError("An error occurred while updating user status.");
        }
    }

    private async Task<int> GetApprovedTodayCountAsync()
    {
        try
        {
            // This would need to be implemented in the repository
            // For now, return 0
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting approved today count");
            return 0;
        }
    }
}

/// <summary>
/// Approval queue view model
/// </summary>
public class ApprovalQueueViewModel
{
    public IEnumerable<RegistrationRequestDto> PendingRequests { get; set; } = new List<RegistrationRequestDto>();
    public int PendingCount { get; set; }
    public int ApprovedTodayCount { get; set; }
}

/// <summary>
/// Reject request DTO
/// </summary>
public class RejectRequestDto
{
    public string Reason { get; set; } = string.Empty;
}