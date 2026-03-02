using System.Net;
using System.Net.Http.Json;
using Bud.Server.Infrastructure.Persistence;
using Bud.Shared.Contracts;
using Bud.Server.Domain.Model;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bud.Server.IntegrationTests.Endpoints;

public class TemplatesEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public TemplatesEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateGlobalAdminClient();

        // Global admin needs X-Tenant-Id to allow the interceptor to set OrganizationId on new entities
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var orgId = db.Organizations.IgnoreQueryFilters()
            .Where(o => o.Name == "getbud.co")
            .Select(o => o.Id)
            .First();
        _client.DefaultRequestHeaders.Add("X-Tenant-Id", orgId.ToString());
    }

    private async Task<Template> CreateTestTemplate()
    {
        var request = new CreateTemplateRequest
        {
            Name = $"Template {Guid.NewGuid():N}",
            Description = "Test template",
            Indicators = new List<TemplateIndicatorRequest>
            {
                new()
                {
                    Name = "Metric 1",
                    Type = Bud.Shared.Contracts.IndicatorType.Quantitative,
                    OrderIndex = 0,
                    QuantitativeType = Bud.Shared.Contracts.QuantitativeIndicatorType.Achieve,
                    MaxValue = 100,
                    Unit = Bud.Shared.Contracts.IndicatorUnit.Percentage
                }
            }
        };
        var response = await _client.PostAsJsonAsync("/api/templates", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Template>())!;
    }

    #region Create Tests

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new CreateTemplateRequest
        {
            Name = "Valid Template",
            Description = "A valid mission template",
            GoalNamePattern = "Mission - {name}",
            GoalDescriptionPattern = "Description for {name}",
            Indicators = new List<TemplateIndicatorRequest>
            {
                new()
                {
                    Name = "Revenue Target",
                    Type = Bud.Shared.Contracts.IndicatorType.Quantitative,
                    OrderIndex = 0,
                    QuantitativeType = Bud.Shared.Contracts.QuantitativeIndicatorType.Achieve,
                    MaxValue = 1000,
                    Unit = Bud.Shared.Contracts.IndicatorUnit.Integer
                },
                new()
                {
                    Name = "Quality Check",
                    Type = Bud.Shared.Contracts.IndicatorType.Qualitative,
                    OrderIndex = 1,
                    TargetText = "Ensure all deliverables meet standards"
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/templates", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var template = await response.Content.ReadFromJsonAsync<Template>();
        template.Should().NotBeNull();
        template!.Name.Should().Be("Valid Template");
        template.Description.Should().Be("A valid mission template");
        template.GoalNamePattern.Should().Be("Mission - {name}");
        template.GoalDescriptionPattern.Should().Be("Description for {name}");
        template.Indicators.Should().HaveCount(2);
    }

    [Fact]
    public async Task Create_WithObjectivesAndObjectiveMetric_ReturnsCreatedWithObjectiveLink()
    {
        // Arrange
        var goalId = Guid.NewGuid();
        var request = new CreateTemplateRequest
        {
            Name = "Template com objetivos",
            Goals =
            [
                new TemplateGoalRequest
                {
                    Id = goalId,
                    Name = "Objetivo estratégico",
                    OrderIndex = 0
                }
            ],
            Indicators =
            [
                new TemplateIndicatorRequest
                {
                    Name = "Métrica vinculada",
                    Type = Bud.Shared.Contracts.IndicatorType.Quantitative,
                    OrderIndex = 0,
                    TemplateGoalId = goalId,
                    QuantitativeType = Bud.Shared.Contracts.QuantitativeIndicatorType.Achieve,
                    MaxValue = 100,
                    Unit = Bud.Shared.Contracts.IndicatorUnit.Percentage
                }
            ]
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/templates", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var template = await response.Content.ReadFromJsonAsync<Template>();
        template.Should().NotBeNull();
        template!.Goals.Should().ContainSingle();
        template.Indicators.Should().ContainSingle();
        template.Indicators.First().TemplateGoalId.Should().Be(template.Goals.First().Id);
    }

    [Fact]
    public async Task Create_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateTemplateRequest
        {
            Name = "",
            Description = "Template with empty name"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/templates", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_ReturnsTemplate()
    {
        // Arrange
        var created = await CreateTestTemplate();

        // Act
        var response = await _client.GetAsync($"/api/templates/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var template = await response.Content.ReadFromJsonAsync<Template>();
        template.Should().NotBeNull();
        template!.Id.Should().Be(created.Id);
        template.Name.Should().Be(created.Name);
        template.Description.Should().Be("Test template");
        template.Indicators.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/templates/{nonExistingId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsPagedResults()
    {
        // Arrange - create a few templates to ensure data exists
        await CreateTestTemplate();
        await CreateTestTemplate();

        // Act
        var response = await _client.GetAsync("/api/templates?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<Template>>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.Items.Should().HaveCountGreaterOrEqualTo(2);
        result.Items.Should().HaveCountLessOrEqualTo(10);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var created = await CreateTestTemplate();

        var updateRequest = new PatchTemplateRequest
        {
            Name = "Updated Template Name",
            Description = "Updated description",
            GoalNamePattern = "Updated - {name}",
            GoalDescriptionPattern = "Updated desc for {name}",
            Indicators = new List<TemplateIndicatorRequest>
            {
                new()
                {
                    Name = "Updated Metric",
                    Type = Bud.Shared.Contracts.IndicatorType.Quantitative,
                    OrderIndex = 0,
                    QuantitativeType = Bud.Shared.Contracts.QuantitativeIndicatorType.Reduce,
                    MinValue = 0,
                    MaxValue = 50,
                    Unit = Bud.Shared.Contracts.IndicatorUnit.Percentage
                }
            }
        };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/templates/{created.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<Template>();
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Template Name");
        updated.Description.Should().Be("Updated description");
        updated.GoalNamePattern.Should().Be("Updated - {name}");
        updated.GoalDescriptionPattern.Should().Be("Updated desc for {name}");
        updated.Indicators.Should().HaveCount(1);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        // Arrange
        var created = await CreateTestTemplate();

        // Act
        var response = await _client.DeleteAsync($"/api/templates/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's deleted
        var getResponse = await _client.GetAsync($"/api/templates/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var unauthenticatedClient = _factory.CreateClient();

        // Act
        var response = await unauthenticatedClient.GetAsync("/api/templates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}
