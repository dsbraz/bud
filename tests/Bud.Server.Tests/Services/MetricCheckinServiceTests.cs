using Bud.Server.Data;
using Bud.Server.Services;
using Bud.Server.Tests.Helpers;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bud.Server.Tests.Services;

public class MetricCheckinServiceTests
{
    private readonly TestTenantProvider _tenantProvider = new() { IsGlobalAdmin = true };

    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, _tenantProvider);
    }

    private static async Task<(Organization org, Mission mission)> CreateTestMission(
        ApplicationDbContext context,
        MissionStatus status = MissionStatus.Active)
    {
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        context.Organizations.Add(org);

        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Test Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = status,
            OrganizationId = org.Id
        };

        context.Missions.Add(mission);
        await context.SaveChangesAsync();

        return (org, mission);
    }

    private static async Task<MissionMetric> CreateTestMetric(
        ApplicationDbContext context,
        Guid missionId,
        Guid organizationId,
        MetricType type,
        string name = "Test Metric")
    {
        var metric = new MissionMetric
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            MissionId = missionId,
            Name = name,
            Type = type,
            TargetText = type == MetricType.Qualitative ? "Target text" : null,
            QuantitativeType = type == MetricType.Quantitative ? QuantitativeMetricType.KeepAbove : null,
            MinValue = type == MetricType.Quantitative ? 10m : null,
            Unit = type == MetricType.Quantitative ? MetricUnit.Integer : null
        };

        context.MissionMetrics.Add(metric);
        await context.SaveChangesAsync();

        return metric;
    }

    private static async Task<Collaborator> CreateTestCollaborator(ApplicationDbContext context, Guid organizationId)
    {
        var collaborator = new Collaborator
        {
            Id = Guid.NewGuid(),
            FullName = "Test Collaborator",
            Email = "test@example.com",
            OrganizationId = organizationId
        };

        context.Collaborators.Add(collaborator);
        await context.SaveChangesAsync();

        return collaborator;
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithQuantitativeMetricAndValue_ReturnsSuccess()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricCheckinService(context);
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id, MetricType.Quantitative);
        var collaborator = await CreateTestCollaborator(context, org.Id);

        var request = new CreateMetricCheckinRequest
        {
            MissionMetricId = metric.Id,
            Value = 42.5m,
            CheckinDate = DateTime.UtcNow,
            Note = "Good progress",
            ConfidenceLevel = 3
        };

        // Act
        var result = await service.CreateAsync(request, collaborator.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().NotBeEmpty();
        result.Value!.MissionMetricId.Should().Be(metric.Id);
        result.Value!.CollaboratorId.Should().Be(collaborator.Id);
        result.Value!.OrganizationId.Should().Be(org.Id);
        result.Value!.Value.Should().Be(42.5m);
        result.Value!.Note.Should().Be("Good progress");
        result.Value!.ConfidenceLevel.Should().Be(3);
    }

    [Fact]
    public async Task CreateAsync_WithQualitativeMetricAndText_ReturnsSuccess()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricCheckinService(context);
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id, MetricType.Qualitative);
        var collaborator = await CreateTestCollaborator(context, org.Id);

        var request = new CreateMetricCheckinRequest
        {
            MissionMetricId = metric.Id,
            Text = "Quality is improving steadily",
            CheckinDate = DateTime.UtcNow,
            Note = "Team feedback is positive",
            ConfidenceLevel = 4
        };

        // Act
        var result = await service.CreateAsync(request, collaborator.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().NotBeEmpty();
        result.Value!.MissionMetricId.Should().Be(metric.Id);
        result.Value!.CollaboratorId.Should().Be(collaborator.Id);
        result.Value!.OrganizationId.Should().Be(org.Id);
        result.Value!.Text.Should().Be("Quality is improving steadily");
        result.Value!.Note.Should().Be("Team feedback is positive");
        result.Value!.ConfidenceLevel.Should().Be(4);
    }

    [Fact]
    public async Task CreateAsync_WithQuantitativeMetricWithoutValue_ReturnsValidationError()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricCheckinService(context);
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id, MetricType.Quantitative);
        var collaborator = await CreateTestCollaborator(context, org.Id);

        var request = new CreateMetricCheckinRequest
        {
            MissionMetricId = metric.Id,
            Value = null,
            Text = "Some text",
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        // Act
        var result = await service.CreateAsync(request, collaborator.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Be("Valor é obrigatório para métricas quantitativas.");
    }

    [Fact]
    public async Task CreateAsync_WithQualitativeMetricWithoutText_ReturnsValidationError()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricCheckinService(context);
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id, MetricType.Qualitative);
        var collaborator = await CreateTestCollaborator(context, org.Id);

        var request = new CreateMetricCheckinRequest
        {
            MissionMetricId = metric.Id,
            Value = 100m,
            Text = null,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 2
        };

        // Act
        var result = await service.CreateAsync(request, collaborator.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Be("Texto é obrigatório para métricas qualitativas.");
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentMetric_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricCheckinService(context);
        var collaboratorId = Guid.NewGuid();

        var request = new CreateMetricCheckinRequest
        {
            MissionMetricId = Guid.NewGuid(),
            Value = 10m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        // Act
        var result = await service.CreateAsync(request, collaboratorId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Métrica não encontrada.");
    }

    [Fact]
    public async Task CreateAsync_WithInvalidConfidenceLevel_ReturnsValidationError()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricCheckinService(context);
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id, MetricType.Quantitative);
        var collaborator = await CreateTestCollaborator(context, org.Id);

        var request = new CreateMetricCheckinRequest
        {
            MissionMetricId = metric.Id,
            Value = 10m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 0
        };

        // Act
        var result = await service.CreateAsync(request, collaborator.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Be("Nível de confiança deve estar entre 1 e 5.");
    }

    [Fact]
    public async Task CreateAsync_TrimsTextAndNote()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricCheckinService(context);
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id, MetricType.Qualitative);
        var collaborator = await CreateTestCollaborator(context, org.Id);

        var request = new CreateMetricCheckinRequest
        {
            MissionMetricId = metric.Id,
            Text = "  Some text with spaces  ",
            CheckinDate = DateTime.UtcNow,
            Note = "  Note with spaces  ",
            ConfidenceLevel = 3
        };

        // Act
        var result = await service.CreateAsync(request, collaborator.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Text.Should().Be("Some text with spaces");
        result.Value!.Note.Should().Be("Note with spaces");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricCheckinService(context);
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id, MetricType.Quantitative);
        var collaborator = await CreateTestCollaborator(context, org.Id);

        var createRequest = new CreateMetricCheckinRequest
        {
            MissionMetricId = metric.Id,
            Value = 10m,
            CheckinDate = DateTime.UtcNow,
            Note = "Initial note",
            ConfidenceLevel = 2
        };

        var createResult = await service.CreateAsync(createRequest, collaborator.Id);
        var checkinId = createResult.Value!.Id;

        var updateRequest = new UpdateMetricCheckinRequest
        {
            Value = 25m,
            CheckinDate = DateTime.UtcNow.AddDays(1),
            Note = "Updated note",
            ConfidenceLevel = 4
        };

        // Act
        var result = await service.UpdateAsync(checkinId, updateRequest);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Value.Should().Be(25m);
        result.Value!.Note.Should().Be("Updated note");
        result.Value!.ConfidenceLevel.Should().Be(4);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentCheckin_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricCheckinService(context);

        var updateRequest = new UpdateMetricCheckinRequest
        {
            Value = 50m,
            CheckinDate = DateTime.UtcNow,
            Note = "Some note",
            ConfidenceLevel = 3
        };

        // Act
        var result = await service.UpdateAsync(Guid.NewGuid(), updateRequest);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Check-in não encontrado.");
    }

    [Fact]
    public async Task UpdateAsync_WhenMetricWasRemoved_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricCheckinService(context);
        var (org, _) = await CreateTestMission(context);
        var collaborator = await CreateTestCollaborator(context, org.Id);
        var checkin = MetricCheckin.Create(
            Guid.NewGuid(),
            org.Id,
            Guid.NewGuid(),
            collaborator.Id,
            10m,
            null,
            DateTime.UtcNow,
            null,
            3);
        context.MetricCheckins.Add(checkin);
        await context.SaveChangesAsync();

        var updateRequest = new UpdateMetricCheckinRequest
        {
            Value = 11m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        // Act
        var result = await service.UpdateAsync(checkin.Id, updateRequest);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Métrica não encontrada.");
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidConfidenceLevel_ReturnsValidationError()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricCheckinService(context);
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id, MetricType.Quantitative);
        var collaborator = await CreateTestCollaborator(context, org.Id);

        var createRequest = new CreateMetricCheckinRequest
        {
            MissionMetricId = metric.Id,
            Value = 10m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };
        var createResult = await service.CreateAsync(createRequest, collaborator.Id);
        var checkinId = createResult.Value!.Id;

        var updateRequest = new UpdateMetricCheckinRequest
        {
            Value = 11m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 6
        };

        // Act
        var result = await service.UpdateAsync(checkinId, updateRequest);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Be("Nível de confiança deve estar entre 1 e 5.");
    }

    [Fact]
    public async Task UpdateAsync_TrimsTextAndNote()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricCheckinService(context);
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id, MetricType.Qualitative);
        var collaborator = await CreateTestCollaborator(context, org.Id);

        var createRequest = new CreateMetricCheckinRequest
        {
            MissionMetricId = metric.Id,
            Text = "Original text",
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        var createResult = await service.CreateAsync(createRequest, collaborator.Id);
        var checkinId = createResult.Value!.Id;

        var updateRequest = new UpdateMetricCheckinRequest
        {
            Text = "  Updated text  ",
            CheckinDate = DateTime.UtcNow,
            Note = "  Updated note  ",
            ConfidenceLevel = 4
        };

        // Act
        var result = await service.UpdateAsync(checkinId, updateRequest);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Text.Should().Be("Updated text");
        result.Value!.Note.Should().Be("Updated note");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingCheckin_ReturnsSuccess()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricCheckinService(context);
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id, MetricType.Quantitative);
        var collaborator = await CreateTestCollaborator(context, org.Id);

        var createRequest = new CreateMetricCheckinRequest
        {
            MissionMetricId = metric.Id,
            Value = 10m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        var createResult = await service.CreateAsync(createRequest, collaborator.Id);
        var checkinId = createResult.Value!.Id;

        // Act
        var result = await service.DeleteAsync(checkinId);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify it was actually removed
        var getResult = await service.GetByIdAsync(checkinId);
        getResult.IsSuccess.Should().BeFalse();
        getResult.ErrorType.Should().Be(ServiceErrorType.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentCheckin_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricCheckinService(context);

        // Act
        var result = await service.DeleteAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Check-in não encontrado.");
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingCheckin_ReturnsSuccess()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricCheckinService(context);
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id, MetricType.Quantitative);
        var collaborator = await CreateTestCollaborator(context, org.Id);

        var createRequest = new CreateMetricCheckinRequest
        {
            MissionMetricId = metric.Id,
            Value = 77m,
            CheckinDate = DateTime.UtcNow,
            Note = "Weekly check-in",
            ConfidenceLevel = 5
        };

        var createResult = await service.CreateAsync(createRequest, collaborator.Id);
        var checkinId = createResult.Value!.Id;

        // Act
        var result = await service.GetByIdAsync(checkinId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(checkinId);
        result.Value!.MissionMetricId.Should().Be(metric.Id);
        result.Value!.CollaboratorId.Should().Be(collaborator.Id);
        result.Value!.Value.Should().Be(77m);
        result.Value!.Note.Should().Be("Weekly check-in");
        result.Value!.ConfidenceLevel.Should().Be(5);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentCheckin_ReturnsNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricCheckinService(context);

        // Act
        var result = await service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Check-in não encontrado.");
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithMissionMetricIdFilter_ReturnsOnlyMatchingCheckins()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricCheckinService(context);
        var (org, mission) = await CreateTestMission(context);
        var metric1 = await CreateTestMetric(context, mission.Id, org.Id, MetricType.Quantitative, "Metric 1");
        var metric2 = await CreateTestMetric(context, mission.Id, org.Id, MetricType.Quantitative, "Metric 2");
        var collaborator = await CreateTestCollaborator(context, org.Id);

        // Create check-ins for metric1
        await service.CreateAsync(new CreateMetricCheckinRequest
        {
            MissionMetricId = metric1.Id,
            Value = 10m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        }, collaborator.Id);

        await service.CreateAsync(new CreateMetricCheckinRequest
        {
            MissionMetricId = metric1.Id,
            Value = 20m,
            CheckinDate = DateTime.UtcNow.AddDays(1),
            ConfidenceLevel = 4
        }, collaborator.Id);

        // Create check-in for metric2
        await service.CreateAsync(new CreateMetricCheckinRequest
        {
            MissionMetricId = metric2.Id,
            Value = 50m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 2
        }, collaborator.Id);

        // Act
        var result = await service.GetAllAsync(missionMetricId: metric1.Id, missionId: null, page: 1, pageSize: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Total.Should().Be(2);
        result.Value!.Items.Should().HaveCount(2);
        result.Value!.Items.Should().AllSatisfy(c => c.MissionMetricId.Should().Be(metric1.Id));
    }

    [Fact]
    public async Task GetAllAsync_WithMissionIdFilter_ReturnsOnlyMatchingCheckins()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricCheckinService(context);
        var (org, mission1) = await CreateTestMission(context);

        var mission2 = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Another Mission",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = MissionStatus.Planned,
            OrganizationId = org.Id
        };
        context.Missions.Add(mission2);
        await context.SaveChangesAsync();

        var metric1 = await CreateTestMetric(context, mission1.Id, org.Id, MetricType.Quantitative, "Metric M1");
        var metric2 = await CreateTestMetric(context, mission2.Id, org.Id, MetricType.Quantitative, "Metric M2");
        var collaborator = await CreateTestCollaborator(context, org.Id);

        // Create check-in for mission1's metric
        await service.CreateAsync(new CreateMetricCheckinRequest
        {
            MissionMetricId = metric1.Id,
            Value = 10m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        }, collaborator.Id);

        // Create check-in for mission2's metric
        await service.CreateAsync(new CreateMetricCheckinRequest
        {
            MissionMetricId = metric2.Id,
            Value = 20m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 4
        }, collaborator.Id);

        // Act
        var result = await service.GetAllAsync(missionMetricId: null, missionId: mission1.Id, page: 1, pageSize: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Total.Should().Be(1);
        result.Value!.Items.Should().HaveCount(1);
        result.Value!.Items[0].MissionMetricId.Should().Be(metric1.Id);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOrderedByCheckinDateDescending()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricCheckinService(context);
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id, MetricType.Quantitative);
        var collaborator = await CreateTestCollaborator(context, org.Id);

        var date1 = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var date2 = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var date3 = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        // Create check-ins in non-chronological order
        await service.CreateAsync(new CreateMetricCheckinRequest
        {
            MissionMetricId = metric.Id,
            Value = 10m,
            CheckinDate = date2,
            ConfidenceLevel = 3
        }, collaborator.Id);

        await service.CreateAsync(new CreateMetricCheckinRequest
        {
            MissionMetricId = metric.Id,
            Value = 30m,
            CheckinDate = date1,
            ConfidenceLevel = 3
        }, collaborator.Id);

        await service.CreateAsync(new CreateMetricCheckinRequest
        {
            MissionMetricId = metric.Id,
            Value = 20m,
            CheckinDate = date3,
            ConfidenceLevel = 3
        }, collaborator.Id);

        // Act
        var result = await service.GetAllAsync(missionMetricId: metric.Id, missionId: null, page: 1, pageSize: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(3);
        result.Value!.Items[0].CheckinDate.Should().Be(date3);
        result.Value!.Items[1].CheckinDate.Should().Be(date2);
        result.Value!.Items[2].CheckinDate.Should().Be(date1);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedResults()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricCheckinService(context);
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id, MetricType.Quantitative);
        var collaborator = await CreateTestCollaborator(context, org.Id);

        // Create 5 check-ins
        for (var i = 0; i < 5; i++)
        {
            await service.CreateAsync(new CreateMetricCheckinRequest
            {
                MissionMetricId = metric.Id,
                Value = i * 10m,
                CheckinDate = DateTime.UtcNow.AddDays(i),
                ConfidenceLevel = 3
            }, collaborator.Id);
        }

        // Act - get page 1 with page size 2
        var result = await service.GetAllAsync(missionMetricId: null, missionId: null, page: 1, pageSize: 2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(5);
        result.Value!.Items.Should().HaveCount(2);
        result.Value!.Page.Should().Be(1);
        result.Value!.PageSize.Should().Be(2);
    }

    #endregion

    #region Mission Status Validation Tests

    [Fact]
    public async Task CreateAsync_WithActiveMission_ReturnsSuccess()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricCheckinService(context);
        var (org, mission) = await CreateTestMission(context, MissionStatus.Active);
        var metric = await CreateTestMetric(context, mission.Id, org.Id, MetricType.Quantitative);
        var collaborator = await CreateTestCollaborator(context, org.Id);

        var request = new CreateMetricCheckinRequest
        {
            MissionMetricId = metric.Id,
            Value = 42.5m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        // Act
        var result = await service.CreateAsync(request, collaborator.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_WithPlannedMission_ReturnsValidationError()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricCheckinService(context);
        var (org, mission) = await CreateTestMission(context, MissionStatus.Planned);
        var metric = await CreateTestMetric(context, mission.Id, org.Id, MetricType.Quantitative);
        var collaborator = await CreateTestCollaborator(context, org.Id);

        var request = new CreateMetricCheckinRequest
        {
            MissionMetricId = metric.Id,
            Value = 42.5m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        // Act
        var result = await service.CreateAsync(request, collaborator.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Be("Não é possível fazer check-in em métricas de missões que não estão ativas.");
    }

    [Fact]
    public async Task CreateAsync_WithCompletedMission_ReturnsValidationError()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricCheckinService(context);
        var (org, mission) = await CreateTestMission(context, MissionStatus.Completed);
        var metric = await CreateTestMetric(context, mission.Id, org.Id, MetricType.Quantitative);
        var collaborator = await CreateTestCollaborator(context, org.Id);

        var request = new CreateMetricCheckinRequest
        {
            MissionMetricId = metric.Id,
            Value = 42.5m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        // Act
        var result = await service.CreateAsync(request, collaborator.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Be("Não é possível fazer check-in em métricas de missões que não estão ativas.");
    }

    [Fact]
    public async Task CreateAsync_WithCancelledMission_ReturnsValidationError()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricCheckinService(context);
        var (org, mission) = await CreateTestMission(context, MissionStatus.Cancelled);
        var metric = await CreateTestMetric(context, mission.Id, org.Id, MetricType.Quantitative);
        var collaborator = await CreateTestCollaborator(context, org.Id);

        var request = new CreateMetricCheckinRequest
        {
            MissionMetricId = metric.Id,
            Value = 42.5m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        // Act
        var result = await service.CreateAsync(request, collaborator.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Validation);
        result.Error.Should().Be("Não é possível fazer check-in em métricas de missões que não estão ativas.");
    }

    #endregion

    #region Error Message Tests (pt-BR)

    [Fact]
    public async Task CreateAsync_WithNonExistentMetric_ReturnsErrorInPortuguese()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricCheckinService(context);

        var request = new CreateMetricCheckinRequest
        {
            MissionMetricId = Guid.NewGuid(),
            Value = 10m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        // Act
        var result = await service.CreateAsync(request, Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Métrica não encontrada.");
    }

    [Fact]
    public async Task CreateAsync_QuantitativeWithoutValue_ReturnsErrorInPortuguese()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricCheckinService(context);
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id, MetricType.Quantitative);

        var request = new CreateMetricCheckinRequest
        {
            MissionMetricId = metric.Id,
            Value = null,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        // Act
        var result = await service.CreateAsync(request, Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Valor é obrigatório para métricas quantitativas.");
    }

    [Fact]
    public async Task CreateAsync_QualitativeWithoutText_ReturnsErrorInPortuguese()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricCheckinService(context);
        var (org, mission) = await CreateTestMission(context);
        var metric = await CreateTestMetric(context, mission.Id, org.Id, MetricType.Qualitative);

        var request = new CreateMetricCheckinRequest
        {
            MissionMetricId = metric.Id,
            Text = null,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        // Act
        var result = await service.CreateAsync(request, Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Texto é obrigatório para métricas qualitativas.");
    }

    [Fact]
    public async Task UpdateAsync_NonExistent_ReturnsErrorInPortuguese()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricCheckinService(context);

        var request = new UpdateMetricCheckinRequest
        {
            Value = 10m,
            CheckinDate = DateTime.UtcNow,
            ConfidenceLevel = 3
        };

        // Act
        var result = await service.UpdateAsync(Guid.NewGuid(), request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Check-in não encontrado.");
    }

    [Fact]
    public async Task DeleteAsync_NonExistent_ReturnsErrorInPortuguese()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricCheckinService(context);

        // Act
        var result = await service.DeleteAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Check-in não encontrado.");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistent_ReturnsErrorInPortuguese()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = new MetricCheckinService(context);

        // Act
        var result = await service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Check-in não encontrado.");
    }

    #endregion
}
