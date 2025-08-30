using Charter.Reporter.Domain.Approvals;

namespace Charter.Reporter.Infrastructure.Data.Entities;

public class ApprovalRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string RequestedRole { get; set; } = string.Empty;
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public string? DecisionReason { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedUtc { get; set; }
}


