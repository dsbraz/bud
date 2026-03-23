using Microsoft.AspNetCore.Components;

namespace Bud.BlazorWasm.Components.Common;

public partial class PagedTableSection
{
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public bool IsEmpty { get; set; }
    [Parameter] public int Total { get; set; }
    [Parameter] public string LoadingText { get; set; } = "Carregando...";
    [Parameter] public string EmptyText { get; set; } = "Nenhum registro encontrado.";
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
