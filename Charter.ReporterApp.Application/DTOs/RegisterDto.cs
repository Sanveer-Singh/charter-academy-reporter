using System.ComponentModel.DataAnnotations;

namespace Charter.ReporterApp.Application.DTOs
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, MinimumLength = 2)]
        [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "Invalid name format")]
        public string FullName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Organization is required")]
        [StringLength(200)]
        public string Organization { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Role selection is required")]
        public string RequestedRole { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "ID number is required")]
        [RegularExpression(@"^\d{13}$", ErrorMessage = "ID must be 13 digits")]
        public string IdNumber { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Address is required")]
        [StringLength(500)]
        public string Address { get; set; } = string.Empty;
    }
}