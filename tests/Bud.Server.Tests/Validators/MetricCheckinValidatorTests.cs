using Bud.Server.Validators;
using Bud.Shared.Contracts;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Validators;

public class CreateMetricCheckinValidatorTests
{
    private readonly CreateMetricCheckinValidator _validator = new();

    [Fact]
    public async Task Validate_ValidRequest_Passes()
    {
        var request = new CreateCheckinRequest
        {
            MetricId = Guid.NewGuid(),
            Value = 42m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3,
            Note = "Progresso bom"
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_EmptyMissionMetricId_Fails()
    {
        var request = new CreateCheckinRequest
        {
            MetricId = Guid.Empty,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MetricId");
    }

    [Fact]
    public async Task Validate_EmptyCheckinDate_Fails()
    {
        var request = new CreateCheckinRequest
        {
            MetricId = Guid.NewGuid(),
            CheckinDate = default,
            ConfidenceLevel = 3
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CheckinDate");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(10)]
    public async Task Validate_ConfidenceLevelOutOfRange_Fails(int confidenceLevel)
    {
        var request = new CreateCheckinRequest
        {
            MetricId = Guid.NewGuid(),
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = confidenceLevel
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "ConfidenceLevel" &&
            e.ErrorMessage.Contains("entre 1 e 5"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task Validate_ConfidenceLevelInRange_Passes(int confidenceLevel)
    {
        var request = new CreateCheckinRequest
        {
            MetricId = Guid.NewGuid(),
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = confidenceLevel
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_NoteTooLong_Fails()
    {
        var request = new CreateCheckinRequest
        {
            MetricId = Guid.NewGuid(),
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3,
            Note = new string('a', 1001)
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Note" &&
            e.ErrorMessage.Contains("1000"));
    }

    [Fact]
    public async Task Validate_TextTooLong_Fails()
    {
        var request = new CreateCheckinRequest
        {
            MetricId = Guid.NewGuid(),
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3,
            Text = new string('a', 1001)
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Text" &&
            e.ErrorMessage.Contains("1000"));
    }

    [Fact]
    public async Task Validate_NullNoteAndText_Passes()
    {
        var request = new CreateCheckinRequest
        {
            MetricId = Guid.NewGuid(),
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 2,
            Note = null,
            Text = null
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }
}

public class UpdateMetricCheckinValidatorTests
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
        result.Errors.Should().Contain(e => e.PropertyName == "CheckinDate");
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
        result.Errors.Should().Contain(e => e.PropertyName == "ConfidenceLevel");
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
        result.Errors.Should().Contain(e => e.PropertyName == "Note");
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
        result.Errors.Should().Contain(e => e.PropertyName == "Text");
    }
}
