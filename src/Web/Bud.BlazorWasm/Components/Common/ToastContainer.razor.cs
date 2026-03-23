using Bud.BlazorWasm.Services;
using Microsoft.AspNetCore.Components;

namespace Bud.BlazorWasm.Components.Common;

public partial class ToastContainer : IDisposable
{
    [Inject] private ToastService ToastService { get; set; } = default!;

    private readonly List<ToastMessage> toasts = new();

    protected override void OnInitialized()
    {
        ToastService.OnToastAdded += AddToast;
    }

    private void AddToast(ToastMessage toast)
    {
        toasts.Add(toast);
        InvokeAsync(StateHasChanged);
    }

    private void RemoveToast(ToastMessage toast)
    {
        toasts.Remove(toast);
        StateHasChanged();
    }

    public void Dispose()
    {
        ToastService.OnToastAdded -= AddToast;
        GC.SuppressFinalize(this);
    }
}
