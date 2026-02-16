using Bud.Server.Application.ObjectiveDimensions;
using Bud.Server.Services;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.UseCases;

public sealed class ObjectiveDimensionQueryUseCaseTests
{
    [Fact]
    public async Task GetByIdAsync_DelegatesToService()
    {
        var id = Guid.NewGuid();
        var service = new Mock<IObjectiveDimensionService>();
        service
            .Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<ObjectiveDimension>.Success(new ObjectiveDimension
            {
                Id = id,
                Name = "Clientes",
                OrganizationId = Guid.NewGuid()
            }));

        var useCase = new ObjectiveDimensionQueryUseCase(service.Object);
        var result = await useCase.GetByIdAsync(id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetAllAsync_DelegatesToService()
    {
        var service = new Mock<IObjectiveDimensionService>();
        service
            .Setup(s => s.GetAllAsync("cli", 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<PagedResult<ObjectiveDimension>>.Success(new PagedResult<ObjectiveDimension>
            {
                Items = [new ObjectiveDimension { Id = Guid.NewGuid(), Name = "Clientes", OrganizationId = Guid.NewGuid() }],
                Total = 1
            }));

        var useCase = new ObjectiveDimensionQueryUseCase(service.Object);
        var result = await useCase.GetAllAsync("cli", 1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(1);
    }
}
