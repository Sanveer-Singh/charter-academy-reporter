using Microsoft.AspNetCore.Mvc;
using Charter.Reporter.Web.Models;
using Newtonsoft.Json;

namespace Charter.Reporter.Web.Extensions;

/// <summary>
/// Extension methods for adding user notifications via TempData
/// </summary>
public static class NotificationExtensions
{
    private const string NotificationsKey = "Notifications";

    /// <summary>
    /// Adds a success notification
    /// </summary>
    public static void AddSuccessNotification(this Controller controller, string message, string? title = null)
    {
        controller.AddNotification(NotificationType.Success, message, title);
    }

    /// <summary>
    /// Adds an error notification
    /// </summary>
    public static void AddErrorNotification(this Controller controller, string message, string? title = null)
    {
        controller.AddNotification(NotificationType.Error, message, title);
    }

    /// <summary>
    /// Adds a warning notification
    /// </summary>
    public static void AddWarningNotification(this Controller controller, string message, string? title = null)
    {
        controller.AddNotification(NotificationType.Warning, message, title);
    }

    /// <summary>
    /// Adds an info notification
    /// </summary>
    public static void AddInfoNotification(this Controller controller, string message, string? title = null)
    {
        controller.AddNotification(NotificationType.Info, message, title);
    }

    /// <summary>
    /// Adds a notification with the specified type
    /// </summary>
    private static void AddNotification(this Controller controller, NotificationType type, string message, string? title = null)
    {
        var notifications = GetNotifications(controller);
        
        notifications.Add(new NotificationMessage
        {
            Type = type,
            Message = message,
            Title = title ?? GetDefaultTitle(type),
            Dismissible = true,
            AutoHide = type == NotificationType.Success || type == NotificationType.Info,
            AutoHideDelay = type == NotificationType.Success ? 4000 : 6000
        });

        controller.TempData[NotificationsKey] = JsonConvert.SerializeObject(notifications);
    }

    /// <summary>
    /// Retrieves all notifications from TempData
    /// </summary>
    public static List<NotificationMessage> GetNotifications(this Controller controller)
    {
        var notificationsJson = controller.TempData[NotificationsKey] as string;
        if (string.IsNullOrEmpty(notificationsJson))
        {
            return new List<NotificationMessage>();
        }

        try
        {
            return JsonConvert.DeserializeObject<List<NotificationMessage>>(notificationsJson) ?? new List<NotificationMessage>();
        }
        catch (JsonException)
        {
            // If deserialization fails, return empty list and clear corrupted data
            controller.TempData.Remove(NotificationsKey);
            return new List<NotificationMessage>();
        }
    }

    /// <summary>
    /// Gets notifications for views (using ViewData to prevent consumption)
    /// </summary>
    public static List<NotificationMessage> GetNotificationsForView(this Controller controller)
    {
        var notifications = GetNotifications(controller);
        // Put them back so they persist through the request
        if (notifications.Any())
        {
            controller.TempData[NotificationsKey] = JsonConvert.SerializeObject(notifications);
        }
        return notifications;
    }

    /// <summary>
    /// Clears all notifications
    /// </summary>
    public static void ClearNotifications(this Controller controller)
    {
        controller.TempData.Remove(NotificationsKey);
    }

    private static string GetDefaultTitle(NotificationType type) => type switch
    {
        NotificationType.Success => "Success",
        NotificationType.Error => "Error", 
        NotificationType.Warning => "Warning",
        NotificationType.Info => "Information",
        _ => "Notification"
    };
}
