namespace Charter.Reporter.Web.Models;

/// <summary>
/// Represents different types of notifications that can be displayed to users
/// </summary>
public enum NotificationType
{
    Success,
    Error,
    Warning,
    Info
}

/// <summary>
/// Model for displaying notifications to users
/// </summary>
public class NotificationMessage
{
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool Dismissible { get; set; } = true;
    public bool AutoHide { get; set; } = false;
    public int AutoHideDelay { get; set; } = 5000; // milliseconds

    public string CssClass => Type switch
    {
        NotificationType.Success => "alert-success",
        NotificationType.Error => "alert-danger", 
        NotificationType.Warning => "alert-warning",
        NotificationType.Info => "alert-info",
        _ => "alert-info"
    };

    public string IconClass => Type switch
    {
        NotificationType.Success => "fas fa-check-circle",
        NotificationType.Error => "fas fa-exclamation-triangle",
        NotificationType.Warning => "fas fa-exclamation-triangle", 
        NotificationType.Info => "fas fa-info-circle",
        _ => "fas fa-info-circle"
    };
}
