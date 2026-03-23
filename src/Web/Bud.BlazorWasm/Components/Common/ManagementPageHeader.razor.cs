using Microsoft.AspNetCore.Components;

namespace Bud.BlazorWasm.Components.Common;

public partial class ManagementPageHeader
{
    [Parameter] public string Kicker { get; set; } = "Gestão";
    [Parameter] public string Title { get; set; } = string.Empty;
    [Parameter] public string? Subtitle { get; set; }
    [Parameter] public string PrimaryActionText { get; set; } = "Novo";
    [Parameter] public bool ShowPrimaryAction { get; set; } = true;
    [Parameter] public EventCallback OnPrimaryAction { get; set; }
}
