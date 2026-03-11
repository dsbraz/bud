using Microsoft.AspNetCore.Components;

namespace Bud.BlazorWasm.Components.Common;

public partial class StatCard
{
    [Parameter, EditorRequired] public string Value { get; set; } = string.Empty;
    [Parameter, EditorRequired] public string Label { get; set; } = string.Empty;
    [Parameter] public bool ShowProgress { get; set; }
    [Parameter] public int Progress { get; set; }
    [Parameter] public int ExpectedProgress { get; set; }
}
