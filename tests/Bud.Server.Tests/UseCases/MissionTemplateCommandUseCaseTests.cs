using System.Security.Claims;
using Bud.Server.Authorization;
using Bud.Server.Application.MissionTemplates;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.UseCases;

public sealed class MissionTemplateCommandUseCaseTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));

    [Fact]
    public async Task CreateAsync_DelegatesToService()
    {
        var organizationId = Guid.NewGuid();
        var createdTemplate = new MissionTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Template",
            OrganizationId = organizationId
        };

        var templateService = new Mock<IMissionTemplateService>();
        templateService
            .Setup(s => s.CreateAsync(It.IsAny<CreateMissionTemplateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<MissionTemplate>.Success(createdTemplate));

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(t => t.TenantId).Returns(organizationId);

        var useCase = new MissionTemplateCommandUseCase(
            templateService.Object,
            authorizationGateway.Object,
            tenantProvider.Object);

        var request = new CreateMissionTemplateRequest { Name = "Template" };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(createdTemplate.Id);
        templateService.Verify(s => s.CreateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenNotAuthorized_ReturnsForbidden()
    {
        var tenantId = Guid.NewGuid();
        var templateService = new Mock<IMissionTemplateService>(MockBehavior.Strict);
        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(t => t.TenantId).Returns(tenantId);

        var useCase = new MissionTemplateCommandUseCase(
            templateService.Object,
            authorizationGateway.Object,
            tenantProvider.Object);

        var request = new CreateMissionTemplateRequest { Name = "Template" };

        var result = await useCase.CreateAsync(User, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Forbidden);
        result.Error.Should().Be("Você não tem permissão para criar templates nesta organização.");
        templateService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_DelegatesToService()
    {
        var template = new MissionTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Template",
            OrganizationId = Guid.NewGuid()
        };

        var request = new UpdateMissionTemplateRequest { Name = "Template 2" };

        var templateService = new Mock<IMissionTemplateService>();
        templateService
            .Setup(s => s.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<MissionTemplate>.Success(template));
        templateService
            .Setup(s => s.UpdateAsync(template.Id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<MissionTemplate>.Success(new MissionTemplate
            {
                Id = template.Id,
                Name = request.Name,
                OrganizationId = template.OrganizationId
            }));

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, template.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var tenantProvider = new Mock<ITenantProvider>();

        var useCase = new MissionTemplateCommandUseCase(
            templateService.Object,
            authorizationGateway.Object,
            tenantProvider.Object);

        var result = await useCase.UpdateAsync(User, template.Id, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Template 2");
        templateService.Verify(s => s.UpdateAsync(template.Id, request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNotFound()
    {
        var templateService = new Mock<IMissionTemplateService>();
        templateService
            .Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<MissionTemplate>.NotFound("Template de missão não encontrado."));

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>(MockBehavior.Strict);
        var tenantProvider = new Mock<ITenantProvider>(MockBehavior.Strict);

        var useCase = new MissionTemplateCommandUseCase(
            templateService.Object,
            authorizationGateway.Object,
            tenantProvider.Object);

        var request = new UpdateMissionTemplateRequest { Name = "Template 2" };

        var result = await useCase.UpdateAsync(User, Guid.NewGuid(), request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Template de missão não encontrado.");
        authorizationGateway.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAsync_DelegatesToService()
    {
        var template = new MissionTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Template",
            OrganizationId = Guid.NewGuid()
        };

        var templateService = new Mock<IMissionTemplateService>();
        templateService
            .Setup(s => s.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<MissionTemplate>.Success(template));
        templateService
            .Setup(s => s.DeleteAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Success());

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>();
        authorizationGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, template.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var tenantProvider = new Mock<ITenantProvider>();

        var useCase = new MissionTemplateCommandUseCase(
            templateService.Object,
            authorizationGateway.Object,
            tenantProvider.Object);

        var result = await useCase.DeleteAsync(User, template.Id);

        result.IsSuccess.Should().BeTrue();
        templateService.Verify(s => s.DeleteAsync(template.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsNotFound()
    {
        var templateService = new Mock<IMissionTemplateService>();
        templateService
            .Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<MissionTemplate>.NotFound("Template de missão não encontrado."));

        var authorizationGateway = new Mock<IApplicationAuthorizationGateway>(MockBehavior.Strict);
        var tenantProvider = new Mock<ITenantProvider>(MockBehavior.Strict);

        var useCase = new MissionTemplateCommandUseCase(
            templateService.Object,
            authorizationGateway.Object,
            tenantProvider.Object);

        var result = await useCase.DeleteAsync(User, Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.NotFound);
        result.Error.Should().Be("Template de missão não encontrado.");
        authorizationGateway.VerifyNoOtherCalls();
    }
}
