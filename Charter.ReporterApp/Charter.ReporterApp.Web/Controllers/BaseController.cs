using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Charter.ReporterApp.Application.Interfaces;
using System.Security.Claims;

namespace Charter.ReporterApp.Web.Controllers;

/// <summary>
/// Base controller with security validation and audit logging
/// </summary>
public abstract class BaseController : Controller
{
    protected readonly ILogger<BaseController> _logger;
    protected readonly IAuditService _auditService;
    protected readonly ISecurityValidationService _securityService;

    protected BaseController(
        ILogger<BaseController> logger,
        IAuditService auditService,
        ISecurityValidationService securityService)
    {
        _logger = logger;
        _auditService = auditService;
        _securityService = securityService;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        try
        {
            // Security validation
            if (!_securityService.ValidateRequest(context))
            {
                context.Result = new ForbidResult();
                _ = _auditService.LogUnauthorizedAccessAsync(
                    User?.Identity?.Name ?? "Anonymous",
                    context.ActionDescriptor.DisplayName ?? "Unknown Action",
                    GetClientIpAddress()
                );
                return;
            }

            // Input validation
            if (!ModelState.IsValid)
            {
                _ = _auditService.LogInvalidInputAsync(
                    User?.Identity?.Name ?? "Anonymous",
                    context.ActionDescriptor.DisplayName ?? "Unknown Action",
                    ModelState
                );
            }

            // Add security headers
            AddSecurityHeaders();

            // Rate limiting check
            var userId = GetUserId();
            var action = context.ActionDescriptor.DisplayName ?? "Unknown";
            
            if (!string.IsNullOrEmpty(userId))
            {
                _ = Task.Run(async () =>
                {
                    if (await _securityService.IsRateLimitExceededAsync(userId, action))
                    {
                        _logger.LogWarning("Rate limit exceeded for user {UserId} on action {Action}", userId, action);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in BaseController.OnActionExecuting");
        }

        base.OnActionExecuting(context);
    }

    private void AddSecurityHeaders()
    {
        if (!Response.Headers.ContainsKey("X-Content-Type-Options"))
            Response.Headers.Add("X-Content-Type-Options", "nosniff");
        
        if (!Response.Headers.ContainsKey("X-Frame-Options"))
            Response.Headers.Add("X-Frame-Options", "DENY");
        
        if (!Response.Headers.ContainsKey("X-XSS-Protection"))
            Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        
        if (!Response.Headers.ContainsKey("Referrer-Policy"))
            Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    }

    protected string GetUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
    }

    protected string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

    protected string GetUserName()
    {
        return User.FindFirst(ClaimTypes.Name)?.Value ?? User.Identity?.Name ?? string.Empty;
    }

    protected string GetClientIpAddress()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        
        // Check for forwarded IP (when behind proxy/load balancer)
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            ipAddress = Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
        }
        else if (Request.Headers.ContainsKey("X-Real-IP"))
        {
            ipAddress = Request.Headers["X-Real-IP"].FirstOrDefault();
        }

        return ipAddress ?? "Unknown";
    }

    protected void SetSuccessMessage(string message)
    {
        TempData["SuccessMessage"] = message;
    }

    protected void SetErrorMessage(string message)
    {
        TempData["ErrorMessage"] = message;
    }

    protected void SetWarningMessage(string message)
    {
        TempData["WarningMessage"] = message;
    }

    protected void SetInfoMessage(string message)
    {
        TempData["InfoMessage"] = message;
    }

    protected IActionResult JsonSuccess(object? data = null, string? message = null)
    {
        return Json(new
        {
            success = true,
            message = message,
            data = data
        });
    }

    protected IActionResult JsonError(string message, object? data = null)
    {
        return Json(new
        {
            success = false,
            message = message,
            data = data
        });
    }

    protected async Task LogUserActionAsync(string action, object? details = null, string? entityType = null, string? entityId = null)
    {
        try
        {
            await _auditService.LogEventAsync(User, action, details, entityType, entityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log user action: {Action}", action);
        }
    }

    protected bool IsUserInRole(string role)
    {
        return User.IsInRole(role);
    }

    protected bool IsCharterAdmin()
    {
        return IsUserInRole("Charter-Admin");
    }

    protected bool IsRebosaAdmin()
    {
        return IsUserInRole("Rebosa-Admin");
    }

    protected bool IsPPRAAdmin()
    {
        return IsUserInRole("PPRA-Admin");
    }

    protected bool IsAnyAdmin()
    {
        return IsCharterAdmin() || IsRebosaAdmin() || IsPPRAAdmin();
    }

    protected IActionResult RedirectToRoleDashboard()
    {
        var role = GetUserRole();
        return role switch
        {
            "Charter-Admin" => RedirectToAction("Index", "Dashboard", new { area = "CharterAdmin" }),
            "Rebosa-Admin" => RedirectToAction("Index", "Dashboard", new { area = "RebosaAdmin" }),
            "PPRA-Admin" => RedirectToAction("Index", "Dashboard", new { area = "PPRAAdmin" }),
            _ => RedirectToAction("Index", "Dashboard")
        };
    }

    protected async Task<IActionResult> HandleExceptionAsync(Exception ex, string action = "Unknown")
    {
        _logger.LogError(ex, "Unhandled exception in {Action}", action);
        
        await LogUserActionAsync($"Error_{action}", new { 
            Error = ex.Message,
            StackTrace = ex.StackTrace 
        });

        SetErrorMessage("An unexpected error occurred. Please try again or contact support if the problem persists.");
        
        return RedirectToAction("Index", "Home");
    }
}