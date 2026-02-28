using Bud.Client.Services;
using Bud.Shared.Contracts;
using FluentAssertions;
using Xunit;

namespace Bud.Client.Tests.Services;

public sealed class IndicatorDisplayHelperTests
{
    [Fact]
    public void GetIndicatorTypeLabel_ShouldReturnPortugueseLabels()
    {
        IndicatorDisplayHelper.GetIndicatorTypeLabel(IndicatorType.Qualitative).Should().Be("Qualitativa");
        IndicatorDisplayHelper.GetIndicatorTypeLabel(IndicatorType.Quantitative).Should().Be("Quantitativa");
    }

    [Fact]
    public void GetTargetLabel_WhenQuantitative_ShouldComposeTargetWithUnit()
    {
        var indicator = new IndicatorResponse
        {
            Type = IndicatorType.Quantitative,
            QuantitativeType = QuantitativeIndicatorType.KeepBetween,
            MinValue = 10,
            MaxValue = 20,
            Unit = IndicatorUnit.Percentage
        };

        var label = IndicatorDisplayHelper.GetTargetLabel(indicator);

        label.Should().Be("Manter entre 10 e 20 Percentual");
    }

    [Fact]
    public void GetCheckinTargetHint_WhenAbbreviatedUnits_ShouldUseShortUnit()
    {
        var indicator = new IndicatorResponse
        {
            Type = IndicatorType.Quantitative,
            QuantitativeType = QuantitativeIndicatorType.Achieve,
            MaxValue = 8,
            Unit = IndicatorUnit.Hours
        };

        var hint = IndicatorDisplayHelper.GetCheckinTargetHint(indicator, useAbbreviatedUnit: true);

        hint.Should().Be("(atingir 8 h)");
    }
}
