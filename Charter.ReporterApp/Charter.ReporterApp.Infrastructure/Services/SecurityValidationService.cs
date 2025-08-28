using Charter.ReporterApp.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Charter.ReporterApp.Infrastructure.Services;

/// <summary>
/// Security validation service implementation
/// </summary>
public class SecurityValidationService : ISecurityValidationService
{
    private readonly ILogger<SecurityValidationService> _logger;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;

    // Rate limiting configuration
    private readonly Dictionary<string, int> _rateLimits = new()
    {
        { "Login", 5 }, // 5 attempts per minute
        { "Register", 3 }, // 3 attempts per minute
        { "PasswordReset", 2 }, // 2 attempts per minute
        { "Default", 60 } // 60 requests per minute for other actions
    };

    public SecurityValidationService(
        ILogger<SecurityValidationService> logger,
        IMemoryCache cache,
        IConfiguration configuration)
    {
        _logger = logger;
        _cache = cache;
        _configuration = configuration;
    }

    public bool ValidateRequest(ActionExecutingContext context)
    {
        try
        {
            // Skip validation for certain controllers/actions
            var controller = context.Controller.GetType().Name;
            var action = context.ActionDescriptor.DisplayName;

            // Allow anonymous access to login, register, etc.
            if (IsAnonymousAction(controller, action))
                return true;

            // Validate authenticated requests
            if (context.HttpContext.User?.Identity?.IsAuthenticated == true)
            {
                return ValidateUserAccess(context.HttpContext.User, context);
            }

            // Redirect to login for unauthenticated requests
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating request");
            return false;
        }
    }

    public bool ValidateUserAccess(ClaimsPrincipal user, ActionExecutingContext context)
    {
        try
        {
            // Check if user is active
            if (!IsUserActive(user))
            {
                _logger.LogWarning("Access denied for inactive user: {UserId}", user.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return false;
            }

            // Check email confirmation
            if (!IsEmailConfirmed(user))
            {
                _logger.LogWarning("Access denied for unconfirmed user: {UserId}", user.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return false;
            }

            // Role-based access control
            var controller = context.Controller.GetType().Name;
            var area = context.RouteData.Values["area"]?.ToString();

            return ValidateRoleAccess(user, controller, area);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user access");
            return false;
        }
    }

    public bool ValidateInputData(object model)
    {
        if (model == null) return false;

        // Basic input validation (XSS, SQL injection patterns)
        var properties = model.GetType().GetProperties();
        foreach (var property in properties)
        {
            if (property.PropertyType == typeof(string))
            {
                var value = property.GetValue(model) as string;
                if (!string.IsNullOrEmpty(value) && ContainsSuspiciousContent(value))
                {
                    _logger.LogWarning("Suspicious input detected in property: {Property}", property.Name);
                    return false;
                }
            }
        }

        return true;
    }

    public bool IsValidRole(string role)
    {
        var validRoles = new[] { "Charter-Admin", "Rebosa-Admin", "PPRA-Admin" };
        return validRoles.Contains(role);
    }

    public bool CanAccessResource(ClaimsPrincipal user, string resourceType, string? resourceId = null)
    {
        var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
        
        return resourceType.ToLower() switch
        {
            "admin" => userRole == "Charter-Admin",
            "reports" => IsValidRole(userRole ?? ""),
            "dashboard" => IsValidRole(userRole ?? ""),
            "registration" => userRole == "Charter-Admin",
            _ => false
        };
    }

    public async Task<bool> ValidatePasswordAsync(string password)
    {
        if (string.IsNullOrEmpty(password)) return false;

        // Password requirements
        if (password.Length < 8) return false;
        if (!Regex.IsMatch(password, @"[A-Z]")) return false; // Upper case
        if (!Regex.IsMatch(password, @"[a-z]")) return false; // Lower case
        if (!Regex.IsMatch(password, @"\d")) return false; // Digit
        if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?\"":{}|<>]")) return false; // Special character

        // Check against common passwords (simplified)
        var commonPasswords = new[] { "password", "123456", "password123", "admin", "qwerty" };
        if (commonPasswords.Any(cp => password.ToLower().Contains(cp))) return false;

        return true;
    }

    public bool ValidateEmailFormat(string email)
    {
        if (string.IsNullOrEmpty(email)) return false;

        try
        {
            var emailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
            return emailRegex.IsMatch(email);
        }
        catch
        {
            return false;
        }
    }

    public bool ValidateIdNumber(string idNumber)
    {
        if (string.IsNullOrEmpty(idNumber) || idNumber.Length != 13) return false;
        
        // South African ID number validation
        if (!long.TryParse(idNumber, out _)) return false;

        // Basic date validation
        var year = int.Parse(idNumber.Substring(0, 2));
        var month = int.Parse(idNumber.Substring(2, 2));
        var day = int.Parse(idNumber.Substring(4, 2));

        if (month < 1 || month > 12 || day < 1 || day > 31) return false;

        // Checksum validation (Luhn algorithm for SA ID)
        return ValidateSouthAfricanIdChecksum(idNumber);
    }

    public async Task<bool> IsRateLimitExceededAsync(string identifier, string action)
    {
        try
        {
            var key = $"rate_limit_{identifier}_{action}";
            var limit = _rateLimits.GetValueOrDefault(action, _rateLimits["Default"]);
            
            if (_cache.TryGetValue(key, out int currentCount))
            {
                if (currentCount >= limit)
                {
                    _logger.LogWarning("Rate limit exceeded for {Identifier} on action {Action}", identifier, action);
                    return true;
                }
                
                _cache.Set(key, currentCount + 1, TimeSpan.FromMinutes(1));
            }
            else
            {
                _cache.Set(key, 1, TimeSpan.FromMinutes(1));
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit");
            return false; // Allow request if rate limiting fails
        }
    }

    private static bool IsAnonymousAction(string controller, string? action)
    {
        var anonymousActions = new[]
        {
            "AccountController.Login",
            "AccountController.Register",
            "AccountController.ForgotPassword",
            "AccountController.ConfirmEmail",
            "HomeController.Index",
            "HomeController.Privacy",
            "HomeController.Error"
        };

        return anonymousActions.Any(aa => action?.Contains(aa) == true);
    }

    private static bool IsUserActive(ClaimsPrincipal user)
    {
        var activeClaimValue = user.FindFirst("IsActive")?.Value;
        return activeClaimValue == "True" || activeClaimValue == "true";
    }

    private static bool IsEmailConfirmed(ClaimsPrincipal user)
    {
        var confirmedClaimValue = user.FindFirst("EmailConfirmed")?.Value;
        return confirmedClaimValue == "True" || confirmedClaimValue == "true";
    }

    private static bool ValidateRoleAccess(ClaimsPrincipal user, string controller, string? area)
    {
        var userRole = user.FindFirst(ClaimTypes.Role)?.Value;

        // Area-based access control
        if (!string.IsNullOrEmpty(area))
        {
            return area switch
            {
                "CharterAdmin" => userRole == "Charter-Admin",
                "RebosaAdmin" => userRole == "Rebosa-Admin",
                "PPRAAdmin" => userRole == "PPRA-Admin",
                _ => false
            };
        }

        // General access for admin controllers
        if (controller.Contains("Admin"))
        {
            return userRole == "Charter-Admin";
        }

        // Allow access to general controllers for all authenticated users
        return true;
    }

    private static bool ContainsSuspiciousContent(string input)
    {
        var suspiciousPatterns = new[]
        {
            @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>", // Script tags
            @"javascript:", // JavaScript protocol
            @"on\w+\s*=", // Event handlers
            @"expression\s*\(", // CSS expressions
            @"(?:'|\x22|;|\+|\s)(select|insert|update|delete|drop|create|alter|exec|union|script)\s", // SQL injection
            @"\.\.\/", // Directory traversal
            @"<iframe\b[^<]*(?:(?!<\/iframe>)<[^<]*)*<\/iframe>", // Iframe tags
        };

        return suspiciousPatterns.Any(pattern => 
            Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase));
    }

    private static bool ValidateSouthAfricanIdChecksum(string idNumber)
    {
        try
        {
            // Luhn algorithm for South African ID number
            int sum = 0;
            bool isEven = false;

            for (int i = idNumber.Length - 2; i >= 0; i--)
            {
                int digit = int.Parse(idNumber[i].ToString());

                if (isEven)
                {
                    digit *= 2;
                    if (digit > 9)
                        digit -= 9;
                }

                sum += digit;
                isEven = !isEven;
            }

            int checkDigit = (10 - (sum % 10)) % 10;
            return checkDigit == int.Parse(idNumber[12].ToString());
        }
        catch
        {
            return false;
        }
    }
}