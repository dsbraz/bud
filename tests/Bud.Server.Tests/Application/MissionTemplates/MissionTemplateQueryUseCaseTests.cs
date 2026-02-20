using Bud.Server.Application.Common;
using Bud.Server.Application.MissionTemplates;
using Bud.Server.Application.Ports;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.Application.MissionTemplates;

public sealed class MissionTemplateQueryUseCaseTests
{
    private readonly Mock<IMissionTemplateRepository> _repo = new();

    [Fact]
    public async Task GetByIdAsync_WithExistingTemplate_ReturnsSuccess()
    {
        var templateId = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdReadOnlyAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MissionTemplate { Id = templateId, Name = "Template", OrganizationId = Guid.NewGuid() });

        var useCase = new MissionTemplateQueryUseCase(_repo.Object);

        var result = await useCase.GetByIdAsync(templateId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(templateId);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ReturnsNotFound()
    {
        _repo.Setup(r => r.GetByIdReadOnlyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MissionTemplate?)null);

        var useCase = new MissionTemplateQueryUseCase(_repo.Object);

        var result = await useCase.GetByIdAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsSuccess()
    {
        var pagedResult = new PagedResult<MissionTemplate>
        {
            Items = [new MissionTemplate { Id = Guid.NewGuid(), Name = "T1", OrganizationId = Guid.NewGuid() }],
            Total = 1,
            Page = 1,
            PageSize = 10
        };

        _repo.Setup(r => r.GetAllAsync("search", 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var useCase = new MissionTemplateQueryUseCase(_repo.Object);

        var result = await useCase.GetAllAsync("search", 1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(1);
    }
}
