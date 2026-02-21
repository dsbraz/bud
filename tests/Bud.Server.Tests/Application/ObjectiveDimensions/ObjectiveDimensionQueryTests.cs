using Bud.Server.Infrastructure.Repositories;
using Bud.Server.Application.Common;
using Bud.Server.Application.ObjectiveDimensions;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.ObjectiveDimensions;

public sealed class ObjectiveDimensionQueryTests
{
    private readonly Mock<IObjectiveDimensionRepository> _repo = new();

    [Fact]
    public async Task GetByIdAsync_WithExistingDimension_ReturnsSuccess()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ObjectiveDimension { Id = id, Name = "Clientes", OrganizationId = Guid.NewGuid() });

        var objectiveDimensionQuery = new ObjectiveDimensionQuery(_repo.Object);
        var result = await objectiveDimensionQuery.GetByIdAsync(id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ReturnsNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ObjectiveDimension?)null);

        var objectiveDimensionQuery = new ObjectiveDimensionQuery(_repo.Object);
        var result = await objectiveDimensionQuery.GetByIdAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResult()
    {
        _repo.Setup(r => r.GetAllAsync("cli", 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ObjectiveDimension>
            {
                Items = [new ObjectiveDimension { Id = Guid.NewGuid(), Name = "Clientes", OrganizationId = Guid.NewGuid() }],
                Total = 1,
                Page = 1,
                PageSize = 10
            });

        var objectiveDimensionQuery = new ObjectiveDimensionQuery(_repo.Object);
        var result = await objectiveDimensionQuery.GetAllAsync("cli", 1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(1);
    }
}
