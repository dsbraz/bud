using System.Security.Claims;
using Bud.Server.Application.ObjectiveDimensions;
using Bud.Server.Authorization;
using Bud.Server.MultiTenancy;
using Bud.Server.Services;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.UseCases;

public sealed class ObjectiveDimensionCommandUseCaseTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));

    [Fact]
    public async Task CreateAsync_DelegatesToService()
    {
        var organizationId = Guid.NewGuid();
        var service = new Mock<IObjectiveDimensionService>();
        service
            .Setup(s => s.CreateAsync(It.IsAny<CreateObjectiveDimensionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<ObjectiveDimension>.Success(new ObjectiveDimension
            {
                Id = Guid.NewGuid(),
                Name = "Clientes",
                OrganizationId = organizationId
            }));

        var authGateway = new Mock<IApplicationAuthorizationGateway>();
        authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(t => t.TenantId).Returns(organizationId);

        var useCase = new ObjectiveDimensionCommandUseCase(service.Object, authGateway.Object, tenantProvider.Object);
        var result = await useCase.CreateAsync(User, new CreateObjectiveDimensionRequest { Name = "Clientes" });

        result.IsSuccess.Should().BeTrue();
        service.Verify(s => s.CreateAsync(It.IsAny<CreateObjectiveDimensionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var tenantId = Guid.NewGuid();
        var service = new Mock<IObjectiveDimensionService>(MockBehavior.Strict);
        var authGateway = new Mock<IApplicationAuthorizationGateway>();
        authGateway
            .Setup(g => g.CanAccessTenantOrganizationAsync(User, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(t => t.TenantId).Returns(tenantId);

        var useCase = new ObjectiveDimensionCommandUseCase(service.Object, authGateway.Object, tenantProvider.Object);
        var result = await useCase.CreateAsync(User, new CreateObjectiveDimensionRequest { Name = "Clientes" });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Forbidden);
    }
}
