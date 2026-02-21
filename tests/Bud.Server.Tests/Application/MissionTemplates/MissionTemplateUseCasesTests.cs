using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.MissionTemplates;
using Bud.Server.Authorization;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.MissionTemplates;

public sealed class MissionTemplateUseCasesTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));

    private readonly Mock<IMissionTemplateRepository> _repository = new();
    private readonly Mock<IApplicationAuthorizationGateway> _authorizationGateway = new();
    private readonly Mock<ITenantProvider> _tenantProvider = new();

    [Fact]
    public async Task CreateStrategicMissionTemplate_WithValidRequest_CreatesTemplate()
    {
        var organizationId = Guid.NewGuid();
        _tenantProvider.SetupGet(provider => provider.TenantId).Returns(organizationId);
        _authorizationGateway
            .Setup(gateway => gateway.CanAccessTenantOrganizationAsync(User, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new CreateStrategicMissionTemplate(
            _repository.Object,
            _authorizationGateway.Object,
            _tenantProvider.Object);

        var request = new CreateMissionTemplateRequest
        {
            Name = "Template",
            Metrics =
            [
                new MissionTemplateMetricDto
                {
                    Name = "Metric",
                    Type = Bud.Shared.Contracts.MetricType.Qualitative,
                    OrderIndex = 0,
                    TargetText = "Target"
                }
            ]
        };

        var result = await useCase.ExecuteAsync(User, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Template");
        _repository.Verify(repository => repository.AddAsync(It.IsAny<MissionTemplate>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateStrategicMissionTemplate_WhenUnauthorized_ReturnsForbidden()
    {
        var tenantId = Guid.NewGuid();
        _tenantProvider.SetupGet(provider => provider.TenantId).Returns(tenantId);
        _authorizationGateway
            .Setup(gateway => gateway.CanAccessTenantOrganizationAsync(User, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new CreateStrategicMissionTemplate(
            _repository.Object,
            _authorizationGateway.Object,
            _tenantProvider.Object);

        var result = await useCase.ExecuteAsync(User, new CreateMissionTemplateRequest { Name = "Template" });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        _repository.Verify(repository => repository.AddAsync(It.IsAny<MissionTemplate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReviseStrategicMissionTemplate_WithExistingTemplate_UpdatesSuccessfully()
    {
        var template = new MissionTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Original",
            OrganizationId = Guid.NewGuid(),
            Objectives = new List<MissionTemplateObjective>(),
            Metrics = new List<MissionTemplateMetric>()
        };

        _repository
            .Setup(repository => repository.GetByIdWithChildrenAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _repository
            .Setup(repository => repository.GetByIdReadOnlyAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MissionTemplate { Id = template.Id, Name = "Updated", OrganizationId = template.OrganizationId });
        _authorizationGateway
            .Setup(gateway => gateway.CanAccessTenantOrganizationAsync(User, template.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new ReviseStrategicMissionTemplate(
            _repository.Object,
            _authorizationGateway.Object);

        var result = await useCase.ExecuteAsync(User, template.Id, new UpdateMissionTemplateRequest { Name = "Updated" });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Updated");
        _repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReviseStrategicMissionTemplate_WhenNotFound_ReturnsNotFound()
    {
        _repository
            .Setup(repository => repository.GetByIdWithChildrenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MissionTemplate?)null);

        var useCase = new ReviseStrategicMissionTemplate(
            _repository.Object,
            _authorizationGateway.Object);

        var result = await useCase.ExecuteAsync(User, Guid.NewGuid(), new UpdateMissionTemplateRequest { Name = "Updated" });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task RemoveStrategicMissionTemplate_WithExistingTemplate_DeletesSuccessfully()
    {
        var template = new MissionTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Template",
            OrganizationId = Guid.NewGuid()
        };

        _repository
            .Setup(repository => repository.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _authorizationGateway
            .Setup(gateway => gateway.CanAccessTenantOrganizationAsync(User, template.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new RemoveStrategicMissionTemplate(
            _repository.Object,
            _authorizationGateway.Object);

        var result = await useCase.ExecuteAsync(User, template.Id);

        result.IsSuccess.Should().BeTrue();
        _repository.Verify(repository => repository.RemoveAsync(template, It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveStrategicMissionTemplate_WhenNotFound_ReturnsNotFound()
    {
        _repository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MissionTemplate?)null);

        var useCase = new RemoveStrategicMissionTemplate(
            _repository.Object,
            _authorizationGateway.Object);

        var result = await useCase.ExecuteAsync(User, Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task ViewStrategicMissionTemplate_WithExistingTemplate_ReturnsSuccess()
    {
        var templateId = Guid.NewGuid();
        _repository
            .Setup(repository => repository.GetByIdReadOnlyAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MissionTemplate { Id = templateId, Name = "Template", OrganizationId = Guid.NewGuid() });

        var useCase = new ViewStrategicMissionTemplate(_repository.Object);

        var result = await useCase.ExecuteAsync(templateId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(templateId);
    }

    [Fact]
    public async Task ListMissionTemplates_ReturnsSuccess()
    {
        var pagedResult = new PagedResult<MissionTemplate>
        {
            Items = [new MissionTemplate { Id = Guid.NewGuid(), Name = "T1", OrganizationId = Guid.NewGuid() }],
            Total = 1,
            Page = 1,
            PageSize = 10
        };

        _repository
            .Setup(repository => repository.GetAllAsync("search", 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var useCase = new ListMissionTemplates(_repository.Object);

        var result = await useCase.ExecuteAsync("search", 1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(1);
    }
}
