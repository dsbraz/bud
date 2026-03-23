using Microsoft.AspNetCore.Components;

namespace Bud.BlazorWasm.Components.Common;

public partial class CrudRowActions
{
    [Parameter] public bool StopPropagation { get; set; } = true;

    [Parameter] public bool EditDisabled { get; set; }
    [Parameter] public string EditTitle { get; set; } = "Editar";
    [Parameter] public EventCallback OnEdit { get; set; }

    [Parameter] public bool IsDeleteConfirming { get; set; }
    [Parameter] public bool DeleteDisabled { get; set; }
    [Parameter] public string DeleteTitle { get; set; } = "Excluir";
    [Parameter] public string DeleteConfirmTitle { get; set; } = "Clique novamente para confirmar";
    [Parameter] public string DeleteConfirmText { get; set; } = "Confirmar?";
    [Parameter] public EventCallback OnDelete { get; set; }
}
