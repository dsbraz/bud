using Bud.BlazorWasm.Api;
using Bud.Shared.Contracts;
using Microsoft.AspNetCore.Components;

namespace Bud.BlazorWasm.Components.Common;

public partial class NotificationDropdown
{
    [Inject] private ApiClient Api { get; set; } = default!;

    [Parameter] public int UnreadCount { get; set; }
    [Parameter] public EventCallback<int> OnCountChanged { get; set; }

    private bool isOpen;
    private bool isLoading;
    private List<NotificationResponse> notifications = new();

    private async Task ToggleDropdown()
    {
        isOpen = !isOpen;
        if (isOpen && notifications.Count == 0)
        {
            await LoadNotifications();
        }
    }

    private async Task LoadNotifications()
    {
        isLoading = true;
        try
        {
            var result = await Api.GetNotificationsAsync(page: 1, pageSize: 20);
            if (result?.Items != null)
            {
                notifications = result.Items.ToList();
            }
        }
        catch
        {
            // Silently handle errors — user can retry by toggling
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task HandleNotificationClick(NotificationResponse notification)
    {
        if (!notification.IsRead)
        {
            try
            {
                await Api.MarkNotificationAsReadAsync(notification.Id);
                notification.IsRead = true;
                notification.ReadAtUtc = DateTime.UtcNow;
                var newCount = Math.Max(0, UnreadCount - 1);
                await OnCountChanged.InvokeAsync(newCount);
            }
            catch
            {
                // Silently handle errors
            }
        }
    }

    private async Task HandleMarkAllAsRead()
    {
        try
        {
            await Api.MarkAllNotificationsAsReadAsync();
            foreach (var n in notifications)
            {
                n.IsRead = true;
                n.ReadAtUtc = DateTime.UtcNow;
            }
            await OnCountChanged.InvokeAsync(0);
        }
        catch
        {
            // Silently handle errors
        }
    }

    private static string FormatRelativeTime(DateTime utcTime)
    {
        var diff = DateTime.UtcNow - utcTime;

        if (diff.TotalMinutes < 1)
        {
            return "agora";
        }

        if (diff.TotalMinutes < 60)
        {
            return $"há {(int)diff.TotalMinutes} min";
        }

        if (diff.TotalHours < 24)
        {
            return $"há {(int)diff.TotalHours}h";
        }

        if (diff.TotalDays < 30)
        {
            return $"há {(int)diff.TotalDays}d";
        }

        return utcTime.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
    }
}
