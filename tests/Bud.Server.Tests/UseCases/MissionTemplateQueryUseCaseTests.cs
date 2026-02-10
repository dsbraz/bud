using Bud.Server.Application.MissionTemplates;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bud.Server.Tests.UseCases;

public sealed class MissionTemplateQueryUseCaseTests
{
    [Fact]
    public async Task GetByIdAsync_DelegatesToService()
    {
        var templateId = Guid.NewGuid();
        var templateService = new Mock<IMissionTemplateService>();
        templateService
            .Setup(s => s.GetByIdAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<MissionTemplate>.Success(new MissionTemplate
            {
                Id = templateId,
                Name = "Template",
                OrganizationId = Guid.NewGuid()
            }));

        var useCase = new MissionTemplateQueryUseCase(templateService.Object);

        var result = await useCase.GetByIdAsync(templateId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(templateId);
        templateService.Verify(s => s.GetByIdAsync(templateId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_DelegatesToService()
    {
        var templates = new PagedResult<MissionTemplate>
        {
            Items =
            [
                new MissionTemplate { Id = Guid.NewGuid(), Name = "Template 1", OrganizationId = Guid.NewGuid() },
                new MissionTemplate { Id = Guid.NewGuid(), Name = "Template 2", OrganizationId = Guid.NewGuid() }
            ],
            Total = 2
        };

        var templateService = new Mock<IMissionTemplateService>();
        templateService
            .Setup(s => s.GetAllAsync("search", 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<PagedResult<MissionTemplate>>.Success(templates));

        var useCase = new MissionTemplateQueryUseCase(templateService.Object);

        var result = await useCase.GetAllAsync("search", 1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
        templateService.Verify(s => s.GetAllAsync("search", 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }
}
