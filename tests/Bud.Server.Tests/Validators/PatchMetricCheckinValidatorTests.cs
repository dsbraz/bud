using Bud.Server.Validators;
using Bud.Shared.Contracts;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Validators;

public class PatchMetricCheckinValidatorTests
{
    private readonly PatchMetricCheckinValidator _validator = new();

    [Fact]
    public async Task Validate_ValidRequest_Passes()
    {
        var request = new PatchCheckinRequest
        {
            Value = 50m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 4,
            Note = "Atualização"
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_EmptyCheckinDate_Fails()
    {
        var request = new PatchCheckinRequest
        {
            CheckinDate = default,
            ConfidenceLevel = 3
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("CheckinDate"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task Validate_ConfidenceLevelOutOfRange_Fails(int confidenceLevel)
    {
        var request = new PatchCheckinRequest
        {
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = confidenceLevel
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("ConfidenceLevel"));
    }

    [Fact]
    public async Task Validate_NoteTooLong_Fails()
    {
        var request = new PatchCheckinRequest
        {
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3,
            Note = new string('a', 1001)
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Note"));
    }

    [Fact]
    public async Task Validate_TextTooLong_Fails()
    {
        var request = new PatchCheckinRequest
        {
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3,
            Text = new string('a', 1001)
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Text"));
    }
}
