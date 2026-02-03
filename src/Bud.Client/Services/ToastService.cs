namespace Bud.Client.Services;

public class ToastService
{
    public event Action<ToastMessage>? OnToastAdded;

    public void ShowSuccess(string title, string? message = null, int durationMs = 5000)
    {
        Show(title, message, ToastType.Success, durationMs);
    }

    public void ShowError(string title, string? message = null, int durationMs = 5000)
    {
        Show(title, message, ToastType.Error, durationMs);
    }

    public void ShowWarning(string title, string? message = null, int durationMs = 5000)
    {
        Show(title, message, ToastType.Warning, durationMs);
    }

    public void ShowInfo(string title, string? message = null, int durationMs = 5000)
    {
        Show(title, message, ToastType.Info, durationMs);
    }

    public void Show(string title, string? message, ToastType type, int durationMs = 5000, bool showCloseButton = true)
    {
        var toast = new ToastMessage
        {
            Title = title,
            Message = message,
            Type = type,
            DurationMs = durationMs,
            ShowCloseButton = showCloseButton
        };

        OnToastAdded?.Invoke(toast);
    }
}

public class ToastMessage
{
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public ToastType Type { get; set; }
    public int DurationMs { get; set; } = 5000;
    public bool ShowCloseButton { get; set; } = true;
}

public enum ToastType
{
    Success,
    Error,
    Warning,
    Info,
    Neutral
}
