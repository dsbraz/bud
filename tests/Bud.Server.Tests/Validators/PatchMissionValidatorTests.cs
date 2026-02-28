using Bud.Server.Validators;
using Bud.Shared.Contracts;
using FluentAssertions;
using Xunit;

namespace Bud.Server.Tests.Validators;

public sealed class PatchMissionValidatorTests
{
    private readonly PatchGoalValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidRequest_Passes()
    {
        var request = new PatchGoalRequest
        {
            Name = "Missão Atualizada",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = GoalStatus.Active
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_WithNoFieldsSet_Passes()
    {
        // PatchGoalValidator uses .When guards on all fields,
        // so a default PatchGoalRequest with no fields set passes validation.
        var request = new PatchGoalRequest();

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    #region Name Validation

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Validate_WithEmptyName_Fails(string? name)
    {
        var request = new PatchGoalRequest
        {
            Name = name!
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Name"));
    }

    [Fact]
    public async Task Validate_WithNameExceeding200Characters_Fails()
    {
        var request = new PatchGoalRequest
        {
            Name = new string('A', 201)
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName.Contains("Name") &&
            e.ErrorMessage.Contains("200"));
    }

    #endregion

    #region Status Validation

    [Fact]
    public async Task Validate_WithInvalidStatus_Fails()
    {
        var request = new PatchGoalRequest
        {
            Status = (GoalStatus)999
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName.Contains("Status") &&
            e.ErrorMessage.Contains("Status inválido"));
    }

    #endregion

    #region EndDate Validation

    [Fact]
    public async Task Validate_WithEmptyEndDate_Fails()
    {
        var request = new PatchGoalRequest
        {
            EndDate = default(DateTime)
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("EndDate"));
    }

    #endregion

    #region Date Range Validation

    [Fact]
    public async Task Validate_WithEndDateBeforeStartDate_Fails()
    {
        var request = new PatchGoalRequest
        {
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage.Contains("Data de término deve ser igual ou posterior à data de início"));
    }

    [Fact]
    public async Task Validate_WithEndDateEqualToStartDate_Passes()
    {
        var date = DateTime.UtcNow;
        var request = new PatchGoalRequest
        {
            StartDate = date,
            EndDate = date
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithOnlyStartDate_Passes()
    {
        var request = new PatchGoalRequest
        {
            StartDate = DateTime.UtcNow
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithOnlyEndDate_Passes()
    {
        // All fields use .When guards, so setting only EndDate is valid.
        var request = new PatchGoalRequest
        {
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    #endregion
}
