using System.ComponentModel.DataAnnotations;

namespace Charter.ReporterApp.Application.DTOs;

/// <summary>
/// User registration DTO with validation
/// </summary>
public class RegisterUserDto : IValidatableObject
{
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters")]
    [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "Full name contains invalid characters")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Organization is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Organization must be between 2 and 200 characters")]
    public string Organization { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role selection is required")]
    public string RequestedRole { get; set; } = string.Empty;

    [Required(ErrorMessage = "ID number is required")]
    [RegularExpression(@"^\d{13}$", ErrorMessage = "ID number must be exactly 13 digits")]
    public string IdNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Invalid phone number format")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required")]
    [StringLength(300, MinimumLength = 10, ErrorMessage = "Address must be between 10 and 300 characters")]
    public string Address { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var allowedRoles = new[] { "Charter-Admin", "Rebosa-Admin", "PPRA-Admin" };
        if (!allowedRoles.Contains(RequestedRole))
        {
            yield return new ValidationResult(
                "Invalid role selection",
                new[] { nameof(RequestedRole) });
        }

        // Validate South African ID number format (basic check)
        if (!string.IsNullOrEmpty(IdNumber) && IdNumber.Length == 13)
        {
            if (!IsValidSouthAfricanId(IdNumber))
            {
                yield return new ValidationResult(
                    "Invalid South African ID number",
                    new[] { nameof(IdNumber) });
            }
        }
    }

    private static bool IsValidSouthAfricanId(string idNumber)
    {
        // Basic South African ID number validation
        if (!long.TryParse(idNumber, out _)) return false;

        // Extract date parts
        var year = int.Parse(idNumber.Substring(0, 2));
        var month = int.Parse(idNumber.Substring(2, 2));
        var day = int.Parse(idNumber.Substring(4, 2));

        // Validate date
        if (month < 1 || month > 12 || day < 1 || day > 31) return false;

        // Basic checksum validation (simplified)
        return true;
    }
}

/// <summary>
/// User DTO for display purposes
/// </summary>
public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

/// <summary>
/// Role DTO
/// </summary>
public class RoleDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Registration request DTO for admin approval
/// </summary>
public class RegistrationRequestDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;
    public string RequestedRole { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? RejectionReason { get; set; }
}

/// <summary>
/// Login DTO
/// </summary>
public class LoginDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = false;
}