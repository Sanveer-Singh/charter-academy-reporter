namespace Charter.ReporterApp.Domain.Entities;

/// <summary>
/// Registration request entity for user approval workflow
/// </summary>
public class RegistrationRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;
    public string RequestedRole { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public RegistrationStatus Status { get; set; } = RegistrationStatus.Pending;
    public string? RejectionReason { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectedBy { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool EmailVerified { get; set; } = false;
    public DateTime? EmailVerifiedAt { get; set; }
}

/// <summary>
/// Registration status enumeration
/// </summary>
public enum RegistrationStatus
{
    Pending = 0,
    EmailVerificationRequired = 1,
    Approved = 2,
    Rejected = 3,
    Expired = 4
}