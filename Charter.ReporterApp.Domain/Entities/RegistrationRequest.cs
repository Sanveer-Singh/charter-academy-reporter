using System;

namespace Charter.ReporterApp.Domain.Entities
{
    public class RegistrationRequest
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Organization { get; set; } = string.Empty;
        public string RequestedRole { get; set; } = string.Empty;
        public string IdNumber { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public RegistrationStatus Status { get; set; }
        public string? EmailVerificationToken { get; set; }
        public DateTime? EmailVerifiedAt { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? RejectedBy { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
    
    public enum RegistrationStatus
    {
        Pending,
        EmailVerified,
        Approved,
        Rejected
    }
}