using System.ComponentModel.DataAnnotations;

namespace Charter.Reporter.Application.Services.Users;

public class UserDetailsVm
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public string Cell { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public bool IsLockedOut { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? TempPassword { get; set; } // Only populated when creating/resetting
}

public class UserCreateVm
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Organization is required")]
    [StringLength(200, ErrorMessage = "Organization cannot exceed 200 characters")]
    public string Organization { get; set; } = string.Empty;

    [Required(ErrorMessage = "ID number is required")]
    [StringLength(50, ErrorMessage = "ID number cannot exceed 50 characters")]
    public string IdNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Cell phone is required")]
    [Phone(ErrorMessage = "Invalid phone number")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
    public string Cell { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required")]
    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required")]
    public string Role { get; set; } = string.Empty;
}

public class UserEditVm
{
    [Required]
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Organization is required")]
    [StringLength(200, ErrorMessage = "Organization cannot exceed 200 characters")]
    public string Organization { get; set; } = string.Empty;

    [Required(ErrorMessage = "ID number is required")]
    [StringLength(50, ErrorMessage = "ID number cannot exceed 50 characters")]
    public string IdNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Cell phone is required")]
    [Phone(ErrorMessage = "Invalid phone number")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
    public string Cell { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required")]
    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
    public string Address { get; set; } = string.Empty;

    public string? Role { get; set; }
}

public class UserListVm
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsLockedOut { get; set; }
    public bool EmailConfirmed { get; set; }
}

public class PasswordResetResultVm
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string TempPassword { get; set; } = string.Empty;
}
