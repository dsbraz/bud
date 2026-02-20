using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.ObjectiveDimensions;
using Bud.Server.Application.Ports;
using Bud.Server.Authorization;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.ObjectiveDimensions;

public sealed class ObjectiveDimensionCommandUseCaseTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));
    private readonly Mock<IObjectiveDimensionRepository> _repo = new();
    private readonly Mock<IApplicationAuthorizationGateway> _authGateway = new();
    private readonly Mock<ITenantProvider> _tenantProvider = new();

    private ObjectiveDimensionCommandUseCase CreateUseCase()
        => new(_repo.Object, _authGateway.Object, _tenantProvider.Object);

    [Fact]
    public async Task CreateAsync_WithValidRequest_ReturnsSuccess()
    {
        var organizationId = Guid.NewGuid();
        _tenantProvider.SetupGet(t => t.TenantId).Returns(organizationId);
        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repo.Setup(r => r.IsNameUniqueAsync("Clientes", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = CreateUseCase();
        var result = await useCase.CreateAsync(User, new CreateObjectiveDimensionRequest { Name = "Clientes" });

        result.IsSuccess.Should().BeTrue();
        _repo.Verify(r => r.AddAsync(It.IsAny<ObjectiveDimension>(), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var tenantId = Guid.NewGuid();
        _tenantProvider.SetupGet(t => t.TenantId).Returns(tenantId);
        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = CreateUseCase();
        var result = await useCase.CreateAsync(User, new CreateObjectiveDimensionRequest { Name = "Clientes" });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicateName_ReturnsConflict()
    {
        var organizationId = Guid.NewGuid();
        _tenantProvider.SetupGet(t => t.TenantId).Returns(organizationId);
        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repo.Setup(r => r.IsNameUniqueAsync("Clientes", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = CreateUseCase();
        var result = await useCase.CreateAsync(User, new CreateObjectiveDimensionRequest { Name = "Clientes" });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNotFound()
    {
        _repo.Setup(r => r.GetByIdTrackedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ObjectiveDimension?)null);

        var useCase = CreateUseCase();
        var result = await useCase.UpdateAsync(User, Guid.NewGuid(), new UpdateObjectiveDimensionRequest { Name = "Novo" });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var orgId = Guid.NewGuid();
        var dimension = new ObjectiveDimension { Id = Guid.NewGuid(), Name = "Clientes", OrganizationId = orgId };
        _repo.Setup(r => r.GetByIdTrackedAsync(dimension.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dimension);
        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = CreateUseCase();
        var result = await useCase.UpdateAsync(User, dimension.Id, new UpdateObjectiveDimensionRequest { Name = "Novo" });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task UpdateAsync_WhenDuplicateName_ReturnsConflict()
    {
        var orgId = Guid.NewGuid();
        var dimension = new ObjectiveDimension { Id = Guid.NewGuid(), Name = "Clientes", OrganizationId = orgId };
        _repo.Setup(r => r.GetByIdTrackedAsync(dimension.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dimension);
        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repo.Setup(r => r.IsNameUniqueAsync("Processos", dimension.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = CreateUseCase();
        var result = await useCase.UpdateAsync(User, dimension.Id, new UpdateObjectiveDimensionRequest { Name = "Processos" });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_ReturnsSuccess()
    {
        var orgId = Guid.NewGuid();
        var dimension = new ObjectiveDimension { Id = Guid.NewGuid(), Name = "Clientes", OrganizationId = orgId };
        _repo.Setup(r => r.GetByIdTrackedAsync(dimension.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dimension);
        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repo.Setup(r => r.IsNameUniqueAsync("Processos", dimension.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = CreateUseCase();
        var result = await useCase.UpdateAsync(User, dimension.Id, new UpdateObjectiveDimensionRequest { Name = "Processos" });

        result.IsSuccess.Should().BeTrue();
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsNotFound()
    {
        _repo.Setup(r => r.GetByIdTrackedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ObjectiveDimension?)null);

        var useCase = CreateUseCase();
        var result = await useCase.DeleteAsync(User, Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var orgId = Guid.NewGuid();
        var dimension = new ObjectiveDimension { Id = Guid.NewGuid(), Name = "Clientes", OrganizationId = orgId };
        _repo.Setup(r => r.GetByIdTrackedAsync(dimension.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dimension);
        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = CreateUseCase();
        var result = await useCase.DeleteAsync(User, dimension.Id);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task DeleteAsync_WhenHasObjectives_ReturnsConflict()
    {
        var orgId = Guid.NewGuid();
        var dimension = new ObjectiveDimension { Id = Guid.NewGuid(), Name = "Clientes", OrganizationId = orgId };
        _repo.Setup(r => r.GetByIdTrackedAsync(dimension.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dimension);
        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repo.Setup(r => r.HasObjectivesAsync(dimension.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = CreateUseCase();
        var result = await useCase.DeleteAsync(User, dimension.Id);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task DeleteAsync_WhenHasTemplateObjectives_ReturnsConflict()
    {
        var orgId = Guid.NewGuid();
        var dimension = new ObjectiveDimension { Id = Guid.NewGuid(), Name = "Clientes", OrganizationId = orgId };
        _repo.Setup(r => r.GetByIdTrackedAsync(dimension.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dimension);
        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repo.Setup(r => r.HasObjectivesAsync(dimension.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repo.Setup(r => r.HasTemplateObjectivesAsync(dimension.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = CreateUseCase();
        var result = await useCase.DeleteAsync(User, dimension.Id);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.Error.Should().Contain("objetivos de template");
    }

    [Fact]
    public async Task DeleteAsync_WithValidRequest_ReturnsSuccess()
    {
        var orgId = Guid.NewGuid();
        var dimension = new ObjectiveDimension { Id = Guid.NewGuid(), Name = "Clientes", OrganizationId = orgId };
        _repo.Setup(r => r.GetByIdTrackedAsync(dimension.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dimension);
        _authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repo.Setup(r => r.HasObjectivesAsync(dimension.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repo.Setup(r => r.HasTemplateObjectivesAsync(dimension.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = CreateUseCase();
        var result = await useCase.DeleteAsync(User, dimension.Id);

        result.IsSuccess.Should().BeTrue();
        _repo.Verify(r => r.RemoveAsync(dimension, It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
