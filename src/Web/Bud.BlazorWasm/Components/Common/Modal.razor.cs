using Microsoft.AspNetCore.Components;

namespace Bud.BlazorWasm.Components.Common;

public partial class Modal
{
    /// <summary>
    /// Modal title displayed in the header
    /// </summary>
    [Parameter]
    public string Title { get; set; } = "";

    /// <summary>
    /// Optional breadcrumb text to show before the title
    /// </summary>
    [Parameter]
    public string? Breadcrumb { get; set; }

    /// <summary>
    /// Optional navigable breadcrumb content (RenderFragment). When provided, renders instead of the plain Breadcrumb string.
    /// </summary>
    [Parameter]
    public RenderFragment? BreadcrumbContent { get; set; }

    /// <summary>
    /// Modal size: sm (400px), md (540px), lg (720px), xl (960px)
    /// </summary>
    [Parameter]
    public string Size { get; set; } = "md";

    /// <summary>
    /// Modal body content
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Optional footer content (for custom action buttons)
    /// </summary>
    [Parameter]
    public RenderFragment? Footer { get; set; }

    /// <summary>
    /// Event callback fired when modal is closed
    /// </summary>
    [Parameter]
    public EventCallback OnClose { get; set; }

    /// <summary>
    /// Whether clicking the overlay should close the modal (default: true)
    /// </summary>
    [Parameter]
    public bool CloseOnOverlayClick { get; set; } = true;

    /// <summary>
    /// Whether modal starts expanded to fullscreen (default: false)
    /// </summary>
    [Parameter]
    public bool InitiallyExpanded { get; set; }

    /// <summary>
    /// Internal state to track if modal is expanded
    /// </summary>
    private bool isExpanded;
    private bool _initialized;

    /// <summary>
    /// Close the modal
    /// </summary>
    private async Task Close()
    {
        await OnClose.InvokeAsync();
    }

    /// <summary>
    /// Toggle expanded/collapsed state
    /// </summary>
    private void ToggleExpand()
    {
        isExpanded = !isExpanded;
    }

    private static void OnDialogClick()
    {
        // Prevents event propagation to backdrop
    }

    protected override void OnParametersSet()
    {
        if (!_initialized)
        {
            isExpanded = InitiallyExpanded;
            _initialized = true;
        }
    }
}
