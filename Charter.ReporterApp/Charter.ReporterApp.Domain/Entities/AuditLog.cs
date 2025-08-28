namespace Charter.ReporterApp.Domain.Entities;

/// <summary>
/// Audit log entity for tracking user actions and security events
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public AuditEventType EventType { get; set; } = AuditEventType.Information;

    // Navigation properties
    public virtual User? User { get; set; }
}

/// <summary>
/// Audit event type enumeration
/// </summary>
public enum AuditEventType
{
    Information = 0,
    Warning = 1,
    Error = 2,
    SecurityEvent = 3,
    DataModification = 4,
    UserAction = 5
}