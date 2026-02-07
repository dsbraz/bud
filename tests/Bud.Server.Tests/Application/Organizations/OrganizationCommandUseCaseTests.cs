using Bud.Server.Application.Organizations;
using Bud.Server.Services;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.Organizations;

public sealed class OrganizationCommandUseCaseTests
{
    [Fact]
    public async Task CreateAsync_DelegatesToService()
    {
        // Arrange
        var request = new CreateOrganizationRequest { Name = "Bud" };
        var organizationService = new Mock<IOrganizationService>();
        organizationService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<Organization>.Success(new Organization { Id = Guid.NewGuid(), Name = request.Name }));

        var useCase = new OrganizationCommandUseCase(organizationService.Object);

        // Act
        var result = await useCase.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        organizationService.Verify(s => s.CreateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_DelegatesToService()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organizationService = new Mock<IOrganizationService>();
        organizationService
            .Setup(s => s.DeleteAsync(organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Success());

        var useCase = new OrganizationCommandUseCase(organizationService.Object);

        // Act
        var result = await useCase.DeleteAsync(organizationId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        organizationService.Verify(s => s.DeleteAsync(organizationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenCreated_DispatchesDomainEvent()
    {
        // Arrange
        var organization = new Organization { Id = Guid.NewGuid(), Name = "Bud" };
        var request = new CreateOrganizationRequest { Name = "Bud" };
        var organizationService = new Mock<IOrganizationService>();
        organizationService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<Organization>.Success(organization));

        var dispatcher = new Mock<Bud.Server.Application.Common.Events.IDomainEventDispatcher>();
        dispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<Bud.Server.Domain.Common.Events.IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new OrganizationCommandUseCase(
            organizationService.Object,
            null,
            dispatcher.Object);

        // Act
        var result = await useCase.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        dispatcher.Verify(d => d.DispatchAsync(
            It.IsAny<Bud.Server.Domain.Organizations.Events.OrganizationCreatedDomainEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenUpdated_DispatchesDomainEvent()
    {
        var organizationId = Guid.NewGuid();
        var request = new UpdateOrganizationRequest { Name = "Bud 2" };
        var organizationService = new Mock<IOrganizationService>();
        organizationService
            .Setup(s => s.UpdateAsync(organizationId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<Organization>.Success(new Organization { Id = organizationId, Name = request.Name }));

        var dispatcher = new Mock<Bud.Server.Application.Common.Events.IDomainEventDispatcher>();
        dispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<Bud.Server.Domain.Common.Events.IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new OrganizationCommandUseCase(organizationService.Object, null, dispatcher.Object);

        var result = await useCase.UpdateAsync(organizationId, request);

        result.IsSuccess.Should().BeTrue();
        dispatcher.Verify(d => d.DispatchAsync(
            It.IsAny<Bud.Server.Domain.Organizations.Events.OrganizationUpdatedDomainEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenDeleted_DispatchesDomainEvent()
    {
        var organizationId = Guid.NewGuid();
        var organizationService = new Mock<IOrganizationService>();
        organizationService
            .Setup(s => s.DeleteAsync(organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult.Success());

        var dispatcher = new Mock<Bud.Server.Application.Common.Events.IDomainEventDispatcher>();
        dispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<Bud.Server.Domain.Common.Events.IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new OrganizationCommandUseCase(organizationService.Object, null, dispatcher.Object);

        var result = await useCase.DeleteAsync(organizationId);

        result.IsSuccess.Should().BeTrue();
        dispatcher.Verify(d => d.DispatchAsync(
            It.IsAny<Bud.Server.Domain.Organizations.Events.OrganizationDeletedDomainEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
