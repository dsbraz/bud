using System.Net;
using System.Net.Http.Json;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using Xunit;

namespace Bud.Server.IntegrationTests.Endpoints;

public class MissionTemplatesEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public MissionTemplatesEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateGlobalAdminClient();
    }

    private async Task<MissionTemplate> CreateTestTemplate()
    {
        var request = new CreateMissionTemplateRequest
        {
            Name = $"Template {Guid.NewGuid():N}",
            Description = "Test template",
            Metrics = new List<MissionTemplateMetricDto>
            {
                new()
                {
                    Name = "Metric 1",
                    Type = MetricType.Quantitative,
                    OrderIndex = 0,
                    QuantitativeType = QuantitativeMetricType.Achieve,
                    MaxValue = 100,
                    Unit = MetricUnit.Percentage
                }
            }
        };
        var response = await _client.PostAsJsonAsync("/api/mission-templates", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MissionTemplate>())!;
    }

    #region Create Tests

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new CreateMissionTemplateRequest
        {
            Name = "Valid Template",
            Description = "A valid mission template",
            MissionNamePattern = "Mission - {name}",
            MissionDescriptionPattern = "Description for {name}",
            Metrics = new List<MissionTemplateMetricDto>
            {
                new()
                {
                    Name = "Revenue Target",
                    Type = MetricType.Quantitative,
                    OrderIndex = 0,
                    QuantitativeType = QuantitativeMetricType.Achieve,
                    MaxValue = 1000,
                    Unit = MetricUnit.Integer
                },
                new()
                {
                    Name = "Quality Check",
                    Type = MetricType.Qualitative,
                    OrderIndex = 1,
                    TargetText = "Ensure all deliverables meet standards"
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/mission-templates", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var template = await response.Content.ReadFromJsonAsync<MissionTemplate>();
        template.Should().NotBeNull();
        template!.Name.Should().Be("Valid Template");
        template.Description.Should().Be("A valid mission template");
        template.MissionNamePattern.Should().Be("Mission - {name}");
        template.MissionDescriptionPattern.Should().Be("Description for {name}");
        template.IsActive.Should().BeTrue();
        template.Metrics.Should().HaveCount(2);
    }

    [Fact]
    public async Task Create_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateMissionTemplateRequest
        {
            Name = "",
            Description = "Template with empty name"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/mission-templates", request);

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
        var response = await _client.GetAsync($"/api/mission-templates/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var template = await response.Content.ReadFromJsonAsync<MissionTemplate>();
        template.Should().NotBeNull();
        template!.Id.Should().Be(created.Id);
        template.Name.Should().Be(created.Name);
        template.Description.Should().Be("Test template");
        template.Metrics.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/mission-templates/{nonExistingId}");

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
        var response = await _client.GetAsync("/api/mission-templates?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<MissionTemplate>>();
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

        var updateRequest = new UpdateMissionTemplateRequest
        {
            Name = "Updated Template Name",
            Description = "Updated description",
            MissionNamePattern = "Updated - {name}",
            MissionDescriptionPattern = "Updated desc for {name}",
            IsActive = true,
            Metrics = new List<MissionTemplateMetricDto>
            {
                new()
                {
                    Name = "Updated Metric",
                    Type = MetricType.Quantitative,
                    OrderIndex = 0,
                    QuantitativeType = QuantitativeMetricType.Reduce,
                    MinValue = 0,
                    MaxValue = 50,
                    Unit = MetricUnit.Percentage
                }
            }
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/mission-templates/{created.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<MissionTemplate>();
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Template Name");
        updated.Description.Should().Be("Updated description");
        updated.MissionNamePattern.Should().Be("Updated - {name}");
        updated.MissionDescriptionPattern.Should().Be("Updated desc for {name}");
        updated.IsActive.Should().BeTrue();
        updated.Metrics.Should().HaveCount(1);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        // Arrange
        var created = await CreateTestTemplate();

        // Act
        var response = await _client.DeleteAsync($"/api/mission-templates/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's deleted
        var getResponse = await _client.GetAsync($"/api/mission-templates/{created.Id}");
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
        var response = await unauthenticatedClient.GetAsync("/api/mission-templates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}
