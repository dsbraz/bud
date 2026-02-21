using Bud.Server.Infrastructure.Repositories;
using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.MissionTemplates;
using Bud.Server.Authorization;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.MissionTemplates;

public sealed class MissionTemplateCommandTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));
    private readonly Mock<IMissionTemplateRepository> _repo = new();
    private readonly Mock<IApplicationAuthorizationGateway> _authGateway = new();
    private readonly Mock<ITenantProvider> _tenantProvider = new();

    private MissionTemplateCommand CreateCommand()
        => new(_repo.Object, _authGateway.Object, _tenantProvider.Object);

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesTemplate()
    {
        var organizationId = Guid.NewGuid();
        _tenantProvider.SetupGet(t => t.TenantId).Returns(organizationId);
        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var missionTemplateCommand = CreateCommand();
        var request = new CreateMissionTemplateRequest
        {
            Name = "Template",
            Metrics =
            [
                new MissionTemplateMetricDto
                {
                    Name = "Metric",
                    Type = MetricType.Qualitative,
                    OrderIndex = 0,
                    TargetText = "Target"
                }
            ]
        };

        var result = await missionTemplateCommand.CreateAsync(User, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Template");
        _repo.Verify(r => r.AddAsync(It.IsAny<MissionTemplate>(), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenNotAuthorized_ReturnsForbidden()
    {
        var tenantId = Guid.NewGuid();
        _tenantProvider.SetupGet(t => t.TenantId).Returns(tenantId);
        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var missionTemplateCommand = CreateCommand();
        var request = new CreateMissionTemplateRequest { Name = "Template" };

        var result = await missionTemplateCommand.CreateAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        _repo.Verify(r => r.AddAsync(It.IsAny<MissionTemplate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithExistingTemplate_UpdatesSuccessfully()
    {
        var template = new MissionTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Original",
            OrganizationId = Guid.NewGuid(),
            Objectives = new List<MissionTemplateObjective>(),
            Metrics = new List<MissionTemplateMetric>()
        };

        _repo.Setup(r => r.GetByIdWithChildrenAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _repo.Setup(r => r.GetByIdReadOnlyAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MissionTemplate { Id = template.Id, Name = "Updated", OrganizationId = template.OrganizationId });
        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, template.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var missionTemplateCommand = CreateCommand();
        var request = new UpdateMissionTemplateRequest { Name = "Updated" };

        var result = await missionTemplateCommand.UpdateAsync(User, template.Id, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Updated");
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNotFound()
    {
        _repo.Setup(r => r.GetByIdWithChildrenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MissionTemplate?)null);

        var missionTemplateCommand = CreateCommand();
        var request = new UpdateMissionTemplateRequest { Name = "Updated" };

        var result = await missionTemplateCommand.UpdateAsync(User, Guid.NewGuid(), request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingTemplate_DeletesSuccessfully()
    {
        var template = new MissionTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Template",
            OrganizationId = Guid.NewGuid()
        };

        _repo.Setup(r => r.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, template.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var missionTemplateCommand = CreateCommand();

        var result = await missionTemplateCommand.DeleteAsync(User, template.Id);

        result.IsSuccess.Should().BeTrue();
        _repo.Verify(r => r.RemoveAsync(template, It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MissionTemplate?)null);

        var missionTemplateCommand = CreateCommand();

        var result = await missionTemplateCommand.DeleteAsync(User, Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }
}
