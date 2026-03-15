using Bud.Shared.Contracts.Features.Goals;
using Microsoft.AspNetCore.Components;

namespace Bud.BlazorWasm.Features.Goals.Components;

public partial class CheckinModal
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public IndicatorResponse? Indicator { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback<CreateCheckinRequest> OnSubmit { get; set; }

    private CreateCheckinRequest checkinRequest = new() { ConfidenceLevel = 3 };
    private IndicatorResponse? previousIndicator;

    protected override void OnParametersSet()
    {
        if (Indicator != previousIndicator)
        {
            previousIndicator = Indicator;
            checkinRequest = new CreateCheckinRequest { ConfidenceLevel = 3 };
        }
    }

    private async Task HandleSubmit()
    {
        if (Indicator is null)
        {
            return;
        }
        checkinRequest.CheckinDate = DateTime.UtcNow;
        await OnSubmit.InvokeAsync(checkinRequest);
    }
}
