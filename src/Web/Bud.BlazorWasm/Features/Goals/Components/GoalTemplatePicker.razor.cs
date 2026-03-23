using Bud.Shared.Contracts.Features.Goals;
using Microsoft.AspNetCore.Components;

namespace Bud.BlazorWasm.Features.Goals.Components;

public partial class GoalTemplatePicker
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public List<TemplateResponse> Templates { get; set; } = [];
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback OnCreateFromScratch { get; set; }
    [Parameter] public EventCallback<TemplateResponse> OnSelectTemplate { get; set; }
}
