using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.ObjectiveDimensions;
using Bud.Server.Authorization;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.ObjectiveDimensions;

public sealed class ObjectiveDimensionUseCasesTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));

    private readonly Mock<IObjectiveDimensionRepository> _repository = new();
    private readonly Mock<IApplicationAuthorizationGateway> _authorizationGateway = new();
    private readonly Mock<ITenantProvider> _tenantProvider = new();

    [Fact]
    public async Task RegisterStrategicDimension_WithValidRequest_ReturnsSuccess()
    {
        var organizationId = Guid.NewGuid();

        _tenantProvider.SetupGet(provider => provider.TenantId).Returns(organizationId);
        _authorizationGateway
            .Setup(gateway => gateway.CanAccessTenantOrganizationAsync(User, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repository
            .Setup(repository => repository.IsNameUniqueAsync("Clientes", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new RegisterStrategicDimension(
            _repository.Object,
            _authorizationGateway.Object,
            _tenantProvider.Object);

        var result = await useCase.ExecuteAsync(User, new CreateObjectiveDimensionRequest { Name = "Clientes" });

        result.IsSuccess.Should().BeTrue();
        _repository.Verify(repository => repository.AddAsync(It.IsAny<ObjectiveDimension>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterStrategicDimension_WhenUnauthorized_ReturnsForbidden()
    {
        var tenantId = Guid.NewGuid();

        _tenantProvider.SetupGet(provider => provider.TenantId).Returns(tenantId);
        _authorizationGateway
            .Setup(gateway => gateway.CanAccessTenantOrganizationAsync(User, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new RegisterStrategicDimension(
            _repository.Object,
            _authorizationGateway.Object,
            _tenantProvider.Object);

        var result = await useCase.ExecuteAsync(User, new CreateObjectiveDimensionRequest { Name = "Clientes" });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task RegisterStrategicDimension_WhenDuplicateName_ReturnsConflict()
    {
        var organizationId = Guid.NewGuid();

        _tenantProvider.SetupGet(provider => provider.TenantId).Returns(organizationId);
        _authorizationGateway
            .Setup(gateway => gateway.CanAccessTenantOrganizationAsync(User, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repository
            .Setup(repository => repository.IsNameUniqueAsync("Clientes", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new RegisterStrategicDimension(
            _repository.Object,
            _authorizationGateway.Object,
            _tenantProvider.Object);

        var result = await useCase.ExecuteAsync(User, new CreateObjectiveDimensionRequest { Name = "Clientes" });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task RenameStrategicDimension_WhenNotFound_ReturnsNotFound()
    {
        _repository
            .Setup(repository => repository.GetByIdTrackedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ObjectiveDimension?)null);

        var useCase = new RenameStrategicDimension(
            _repository.Object,
            _authorizationGateway.Object);

        var result = await useCase.ExecuteAsync(User, Guid.NewGuid(), new UpdateObjectiveDimensionRequest { Name = "Novo" });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task RemoveStrategicDimension_WhenHasObjectives_ReturnsConflict()
    {
        var organizationId = Guid.NewGuid();
        var dimension = new ObjectiveDimension { Id = Guid.NewGuid(), Name = "Clientes", OrganizationId = organizationId };

        _repository
            .Setup(repository => repository.GetByIdTrackedAsync(dimension.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dimension);
        _authorizationGateway
            .Setup(gateway => gateway.CanAccessTenantOrganizationAsync(User, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repository
            .Setup(repository => repository.HasObjectivesAsync(dimension.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new RemoveStrategicDimension(
            _repository.Object,
            _authorizationGateway.Object);

        var result = await useCase.ExecuteAsync(User, dimension.Id);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task ViewStrategicDimensionDetails_WithExistingDimension_ReturnsSuccess()
    {
        var id = Guid.NewGuid();

        _repository
            .Setup(repository => repository.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ObjectiveDimension { Id = id, Name = "Clientes", OrganizationId = Guid.NewGuid() });

        var useCase = new ViewStrategicDimensionDetails(_repository.Object);

        var result = await useCase.ExecuteAsync(id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(id);
    }

    [Fact]
    public async Task ListStrategicDimensions_ReturnsPagedResult()
    {
        _repository
            .Setup(repository => repository.GetAllAsync("cli", 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ObjectiveDimension>
            {
                Items = [new ObjectiveDimension { Id = Guid.NewGuid(), Name = "Clientes", OrganizationId = Guid.NewGuid() }],
                Total = 1,
                Page = 1,
                PageSize = 10
            });

        var useCase = new ListStrategicDimensions(_repository.Object);

        var result = await useCase.ExecuteAsync("cli", 1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(1);
    }
}
