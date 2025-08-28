using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Charter.ReporterApp.Application.Interfaces;

/// <summary>
/// Security validation service interface for request validation
/// </summary>
public interface ISecurityValidationService
{
    bool ValidateRequest(ActionExecutingContext context);
    bool ValidateUserAccess(ClaimsPrincipal user, ActionExecutingContext context);
    bool ValidateInputData(object model);
    bool IsValidRole(string role);
    bool CanAccessResource(ClaimsPrincipal user, string resourceType, string? resourceId = null);
    Task<bool> ValidatePasswordAsync(string password);
    bool ValidateEmailFormat(string email);
    bool ValidateIdNumber(string idNumber);
    Task<bool> IsRateLimitExceededAsync(string identifier, string action);
}

/// <summary>
/// Audit service interface for tracking user actions
/// </summary>
public interface IAuditService
{
    Task LogEventAsync(string userId, string action, object? details = null, string? entityType = null, string? entityId = null);
    Task LogEventAsync(ClaimsPrincipal user, string action, object? details = null, string? entityType = null, string? entityId = null);
    Task LogUnauthorizedAccessAsync(string userId, string attemptedAction, string? ipAddress = null);
    Task LogInvalidInputAsync(string userId, string action, object? invalidData = null);
    Task LogRegistrationApprovalAsync(object registrationRequest, string approvedBy);
    Task LogRegistrationRejectionAsync(object registrationRequest, string rejectedBy, string reason);
    Task LogPasswordChangeAsync(string userId);
    Task LogLoginAsync(string userId, bool successful, string? ipAddress = null);
    Task LogLogoutAsync(string userId);
}

/// <summary>
/// Email service interface for sending notifications
/// </summary>
public interface IEmailService
{
    Task<bool> SendVerificationEmailAsync(string email, string verificationToken);
    Task<bool> SendApprovalNotificationAsync(string email, string userName);
    Task<bool> SendRejectionNotificationAsync(string email, string userName, string reason);
    Task<bool> SendPasswordResetEmailAsync(string email, string resetToken);
    Task<bool> SendWelcomeEmailAsync(string email, string userName, string temporaryPassword);
}

/// <summary>
/// Authentication service interface
/// </summary>
public interface IAuthenticationService
{
    Task<AuthenticationResult> AuthenticateAsync(string email, string password);
    Task<AuthenticationResult> ConfirmEmailAsync(string email, string token);
    Task<string> GenerateEmailConfirmationTokenAsync(string email);
    Task<AuthenticationResult> ResetPasswordAsync(string email, string token, string newPassword);
    Task<string> GeneratePasswordResetTokenAsync(string email);
    Task<bool> SignOutAsync();
}

/// <summary>
/// Authentication result model
/// </summary>
public class AuthenticationResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
    public string? UserId { get; set; }
    public IEnumerable<string> Roles { get; set; } = new List<string>();

    public static AuthenticationResult Success(string userId, IEnumerable<string> roles)
        => new() { Succeeded = true, UserId = userId, Roles = roles };

    public static AuthenticationResult Failed(string errorMessage)
        => new() { Succeeded = false, ErrorMessage = errorMessage };
}