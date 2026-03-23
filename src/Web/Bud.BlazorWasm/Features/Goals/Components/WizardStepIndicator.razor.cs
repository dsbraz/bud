using Bud.Shared.Contracts.Features.Goals;
using Microsoft.AspNetCore.Components;

namespace Bud.BlazorWasm.Features.Goals.Components;

public partial class WizardStepIndicator
{
    [Parameter] public WizardStep CurrentStep { get; set; }
    [Parameter] public bool Hidden { get; set; }
    [Parameter] public EventCallback<WizardStep> OnStepClick { get; set; }

    private static readonly (WizardStep Value, string Label)[] Steps =
    [
        (WizardStep.ChooseTemplate, "Escolher template"),
        (WizardStep.BuildGoal, "Construir meta"),
        (WizardStep.Review, "Revisão")
    ];
}
