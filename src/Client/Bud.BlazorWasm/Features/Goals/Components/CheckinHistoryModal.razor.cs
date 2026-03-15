using Bud.BlazorWasm.Services;
using Bud.Shared.Contracts.Features.Goals;
using Microsoft.AspNetCore.Components;

namespace Bud.BlazorWasm.Features.Goals.Components;

public partial class CheckinHistoryModal
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public IndicatorResponse? Indicator { get; set; }
    [Parameter] public List<CheckinResponse> Checkins { get; set; } = [];
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback OnNewCheckin { get; set; }

    private static string GetConfidenceStarsDisplay(int level) =>
        new string('\u2605', level) + new string('\u2606', 5 - level);

    private string GetCheckinValueDisplay(CheckinResponse checkin)
    {
        if (checkin.Value.HasValue)
        {
            var unit = Indicator?.Unit.HasValue == true ? IndicatorDisplayHelper.GetUnitLabel(Indicator.Unit!.Value) : "";
            return $"{checkin.Value.Value:G} {unit}".Trim();
        }
        if (!string.IsNullOrWhiteSpace(checkin.Text))
        {
            return checkin.Text;
        }
        return "\u2014";
    }
}
