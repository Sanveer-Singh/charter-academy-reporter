using Charter.ReporterApp.Application.Interfaces;
using Charter.ReporterApp.Domain.Entities;
using Charter.ReporterApp.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;

namespace Charter.ReporterApp.Infrastructure.Services;

/// <summary>
/// Audit service implementation for tracking user actions
/// </summary>
public class AuditService : IAuditService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuditService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(
        AppDbContext context,
        ILogger<AuditService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogEventAsync(string userId, string action, object? details = null, string? entityType = null, string? entityId = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                UserName = GetUserNameFromContext() ?? "Unknown",
                Action = action,
                EntityType = entityType ?? "Unknown",
                EntityId = entityId,
                Details = details != null ? JsonSerializer.Serialize(details) : null,
                IpAddress = GetClientIpAddress(),
                UserAgent = GetUserAgent(),
                EventType = DetermineEventType(action),
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit event: {Action} for user {UserId}", action, userId);
        }
    }

    public async Task LogEventAsync(ClaimsPrincipal user, string action, object? details = null, string? entityType = null, string? entityId = null)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
        var userName = user.FindFirst(ClaimTypes.Name)?.Value ?? user.Identity?.Name ?? "Unknown";

        try
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                UserName = userName,
                Action = action,
                EntityType = entityType ?? "Unknown",
                EntityId = entityId,
                Details = details != null ? JsonSerializer.Serialize(details) : null,
                IpAddress = GetClientIpAddress(),
                UserAgent = GetUserAgent(),
                EventType = DetermineEventType(action),
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit event: {Action} for user {UserName}", action, userName);
        }
    }

    public async Task LogUnauthorizedAccessAsync(string userId, string attemptedAction, string? ipAddress = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                UserName = GetUserNameFromContext() ?? "Unknown",
                Action = "UnauthorizedAccess",
                EntityType = "Security",
                Details = JsonSerializer.Serialize(new { AttemptedAction = attemptedAction }),
                IpAddress = ipAddress ?? GetClientIpAddress(),
                UserAgent = GetUserAgent(),
                EventType = AuditEventType.SecurityEvent,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Unauthorized access attempt: {AttemptedAction} by user {UserId} from IP {IpAddress}", 
                attemptedAction, userId, auditLog.IpAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log unauthorized access for user {UserId}", userId);
        }
    }

    public async Task LogInvalidInputAsync(string userId, string action, object? invalidData = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                UserName = GetUserNameFromContext() ?? "Unknown",
                Action = "InvalidInput",
                EntityType = "Validation",
                Details = JsonSerializer.Serialize(new { 
                    OriginalAction = action,
                    InvalidData = invalidData?.ToString() ?? "Not specified"
                }),
                IpAddress = GetClientIpAddress(),
                UserAgent = GetUserAgent(),
                EventType = AuditEventType.Warning,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log invalid input for user {UserId}", userId);
        }
    }

    public async Task LogRegistrationApprovalAsync(object registrationRequest, string approvedBy)
    {
        try
        {
            var auditLog = new AuditLog
            {
                UserId = approvedBy,
                UserName = approvedBy,
                Action = "RegistrationApproved",
                EntityType = "RegistrationRequest",
                Details = JsonSerializer.Serialize(registrationRequest),
                IpAddress = GetClientIpAddress(),
                UserAgent = GetUserAgent(),
                EventType = AuditEventType.UserAction,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Registration approved by {ApprovedBy}", approvedBy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log registration approval");
        }
    }

    public async Task LogRegistrationRejectionAsync(object registrationRequest, string rejectedBy, string reason)
    {
        try
        {
            var auditLog = new AuditLog
            {
                UserId = rejectedBy,
                UserName = rejectedBy,
                Action = "RegistrationRejected",
                EntityType = "RegistrationRequest",
                Details = JsonSerializer.Serialize(new { 
                    RegistrationRequest = registrationRequest,
                    RejectionReason = reason
                }),
                IpAddress = GetClientIpAddress(),
                UserAgent = GetUserAgent(),
                EventType = AuditEventType.UserAction,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Registration rejected by {RejectedBy} with reason: {Reason}", rejectedBy, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log registration rejection");
        }
    }

    public async Task LogPasswordChangeAsync(string userId)
    {
        try
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                UserName = GetUserNameFromContext() ?? "Unknown",
                Action = "PasswordChanged",
                EntityType = "User",
                EntityId = userId,
                IpAddress = GetClientIpAddress(),
                UserAgent = GetUserAgent(),
                EventType = AuditEventType.SecurityEvent,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log password change for user {UserId}", userId);
        }
    }

    public async Task LogLoginAsync(string userId, bool successful, string? ipAddress = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                UserName = GetUserNameFromContext() ?? "Unknown",
                Action = successful ? "LoginSuccessful" : "LoginFailed",
                EntityType = "Authentication",
                EntityId = userId,
                IpAddress = ipAddress ?? GetClientIpAddress(),
                UserAgent = GetUserAgent(),
                EventType = successful ? AuditEventType.UserAction : AuditEventType.SecurityEvent,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            if (!successful)
            {
                _logger.LogWarning("Failed login attempt for user {UserId} from IP {IpAddress}", userId, auditLog.IpAddress);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log login event for user {UserId}", userId);
        }
    }

    public async Task LogLogoutAsync(string userId)
    {
        try
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                UserName = GetUserNameFromContext() ?? "Unknown",
                Action = "Logout",
                EntityType = "Authentication",
                EntityId = userId,
                IpAddress = GetClientIpAddress(),
                UserAgent = GetUserAgent(),
                EventType = AuditEventType.UserAction,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log logout for user {UserId}", userId);
        }
    }

    private string GetClientIpAddress()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return "Unknown";

        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();

        // Check for forwarded IP (when behind proxy/load balancer)
        if (httpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            ipAddress = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
        }
        else if (httpContext.Request.Headers.ContainsKey("X-Real-IP"))
        {
            ipAddress = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        }

        return ipAddress ?? "Unknown";
    }

    private string GetUserAgent()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        return httpContext?.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";
    }

    private string? GetUserNameFromContext()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        return httpContext?.User?.Identity?.Name;
    }

    private static AuditEventType DetermineEventType(string action)
    {
        return action.ToLower() switch
        {
            var a when a.Contains("login") || a.Contains("logout") || a.Contains("password") => AuditEventType.SecurityEvent,
            var a when a.Contains("unauthorized") || a.Contains("failed") => AuditEventType.SecurityEvent,
            var a when a.Contains("create") || a.Contains("update") || a.Contains("delete") => AuditEventType.DataModification,
            var a when a.Contains("error") || a.Contains("exception") => AuditEventType.Error,
            var a when a.Contains("warning") || a.Contains("invalid") => AuditEventType.Warning,
            _ => AuditEventType.UserAction
        };
    }
}