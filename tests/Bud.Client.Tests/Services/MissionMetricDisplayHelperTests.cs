using Bud.Client.Services;
using Bud.Shared.Models;
using FluentAssertions;
using Xunit;

namespace Bud.Client.Tests.Services;

public sealed class MissionMetricDisplayHelperTests
{
    [Fact]
    public void GetMetricTypeLabel_ShouldReturnPortugueseLabels()
    {
        MissionMetricDisplayHelper.GetMetricTypeLabel(MetricType.Qualitative).Should().Be("Qualitativa");
        MissionMetricDisplayHelper.GetMetricTypeLabel(MetricType.Quantitative).Should().Be("Quantitativa");
    }

    [Fact]
    public void GetTargetLabel_WhenQuantitative_ShouldComposeTargetWithUnit()
    {
        var metric = new MissionMetric
        {
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.KeepBetween,
            MinValue = 10,
            MaxValue = 20,
            Unit = MetricUnit.Percentage
        };

        var label = MissionMetricDisplayHelper.GetTargetLabel(metric);

        label.Should().Be("Manter entre 10 e 20 Percentual");
    }

    [Fact]
    public void GetCheckinTargetHint_WhenAbbreviatedUnits_ShouldUseShortUnit()
    {
        var metric = new MissionMetric
        {
            Type = MetricType.Quantitative,
            QuantitativeType = QuantitativeMetricType.Achieve,
            MaxValue = 8,
            Unit = MetricUnit.Hours
        };

        var hint = MissionMetricDisplayHelper.GetCheckinTargetHint(metric, useAbbreviatedUnit: true);

        hint.Should().Be("(atingir 8 h)");
    }
}
