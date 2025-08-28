using Microsoft.AspNetCore.Identity;

namespace Charter.ReporterApp.Domain.Entities;

/// <summary>
/// Application user entity extending IdentityUser for authentication
/// </summary>
public class User : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }

    // Navigation properties
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}

/// <summary>
/// Application role entity extending IdentityRole
/// </summary>
public class Role : IdentityRole
{
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}