using Bud.BlazorWasm.Services;
using Microsoft.AspNetCore.Components;

namespace Bud.BlazorWasm.Components.Common;

public partial class Toast : IDisposable
{
    [Parameter] public string Title { get; set; } = string.Empty;
    [Parameter] public string? Message { get; set; }
    [Parameter] public ToastType Type { get; set; } = ToastType.Info;
    [Parameter] public bool ShowCloseButton { get; set; } = true;
    [Parameter] public int DurationMs { get; set; } = 5000;
    [Parameter] public EventCallback OnClosed { get; set; }

    private bool IsVisible = true;
    private bool IsClosing;
    private System.Threading.Timer? autoCloseTimer;

    protected override void OnInitialized()
    {
        if (DurationMs > 0)
        {
            autoCloseTimer = new System.Threading.Timer(_ => InvokeAsync(Close), null, DurationMs, Timeout.Infinite);
        }
    }

    private async Task Close()
    {
        IsClosing = true;
        StateHasChanged();

        await Task.Delay(300); // Wait for animation

        IsVisible = false;
        autoCloseTimer?.Dispose();
        await OnClosed.InvokeAsync();
    }

    public void Dispose()
    {
        autoCloseTimer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
