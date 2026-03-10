using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Ports;
using Bud.Shared.Contracts;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Bud.Application.UnitTests.Application.Templates;

public sealed class TemplateUseCasesTests
{
    private static readonly ClaimsPrincipal User = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test")]));

    private readonly Mock<ITemplateRepository> _repository = new();
    private readonly Mock<IApplicationAuthorizationGateway> _authorizationGateway = new();
    private readonly Mock<ITenantProvider> _tenantProvider = new();

    [Fact]
    public async Task CreateStrategicTemplate_WithValidRequest_CreatesTemplate()
    {
        var organizationId = Guid.NewGuid();
        _tenantProvider.SetupGet(provider => provider.TenantId).Returns(organizationId);
        _authorizationGateway
            .Setup(gateway => gateway.CanAccessTenantOrganizationAsync(User, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new CreateTemplate(
            _repository.Object,
            _authorizationGateway.Object,
            _tenantProvider.Object,
            NullLogger<CreateTemplate>.Instance);

        var request = new CreateTemplateRequest
        {
            Name = "Template",
            Indicators =
            [
                new TemplateIndicatorRequest
                {
                    Name = "Metric",
                    Type = Bud.Shared.Kernel.IndicatorType.Qualitative,
                    OrderIndex = 0,
                    TargetText = "Target"
                }
            ]
        };

        var result = await useCase.ExecuteAsync(User, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Template");
        _repository.Verify(repository => repository.AddAsync(It.IsAny<Template>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateStrategicTemplate_WhenUnauthorized_ReturnsForbidden()
    {
        var tenantId = Guid.NewGuid();
        _tenantProvider.SetupGet(provider => provider.TenantId).Returns(tenantId);
        _authorizationGateway
            .Setup(gateway => gateway.CanAccessTenantOrganizationAsync(User, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new CreateTemplate(
            _repository.Object,
            _authorizationGateway.Object,
            _tenantProvider.Object,
            NullLogger<CreateTemplate>.Instance);

        var result = await useCase.ExecuteAsync(User, new CreateTemplateRequest { Name = "Template" });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Forbidden);
        _repository.Verify(repository => repository.AddAsync(It.IsAny<Template>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReviseStrategicTemplate_WithExistingTemplate_UpdatesSuccessfully()
    {
        var template = new Template
        {
            Id = Guid.NewGuid(),
            Name = "Original",
            OrganizationId = Guid.NewGuid(),
            Goals = new List<TemplateGoal>(),
            Indicators = new List<TemplateIndicator>()
        };

        _repository
            .Setup(repository => repository.GetByIdWithChildrenAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _repository
            .Setup(repository => repository.GetByIdReadOnlyAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Template { Id = template.Id, Name = "Updated", OrganizationId = template.OrganizationId });
        _authorizationGateway
            .Setup(gateway => gateway.CanAccessTenantOrganizationAsync(User, template.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new PatchTemplate(
            _repository.Object,
            _authorizationGateway.Object,
            NullLogger<PatchTemplate>.Instance);

        var result = await useCase.ExecuteAsync(User, template.Id, new PatchTemplateRequest { Name = "Updated" });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Updated");
        _repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReviseStrategicTemplate_WhenNotFound_ReturnsNotFound()
    {
        _repository
            .Setup(repository => repository.GetByIdWithChildrenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Template?)null);

        var useCase = new PatchTemplate(
            _repository.Object,
            _authorizationGateway.Object,
            NullLogger<PatchTemplate>.Instance);

        var result = await useCase.ExecuteAsync(User, Guid.NewGuid(), new PatchTemplateRequest { Name = "Updated" });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task RemoveStrategicTemplate_WithExistingTemplate_DeletesSuccessfully()
    {
        var template = new Template
        {
            Id = Guid.NewGuid(),
            Name = "Template",
            OrganizationId = Guid.NewGuid()
        };

        _repository
            .Setup(repository => repository.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _authorizationGateway
            .Setup(gateway => gateway.CanAccessTenantOrganizationAsync(User, template.OrganizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new DeleteTemplate(
            _repository.Object,
            _authorizationGateway.Object,
            NullLogger<DeleteTemplate>.Instance);

        var result = await useCase.ExecuteAsync(User, template.Id);

        result.IsSuccess.Should().BeTrue();
        _repository.Verify(repository => repository.RemoveAsync(template, It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveStrategicTemplate_WhenNotFound_ReturnsNotFound()
    {
        _repository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Template?)null);

        var useCase = new DeleteTemplate(
            _repository.Object,
            _authorizationGateway.Object,
            NullLogger<DeleteTemplate>.Instance);

        var result = await useCase.ExecuteAsync(User, Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task ViewStrategicTemplate_WithExistingTemplate_ReturnsSuccess()
    {
        var templateId = Guid.NewGuid();
        _repository
            .Setup(repository => repository.GetByIdReadOnlyAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Template { Id = templateId, Name = "Template", OrganizationId = Guid.NewGuid() });

        var useCase = new GetTemplateById(_repository.Object);

        var result = await useCase.ExecuteAsync(templateId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(templateId);
    }

    [Fact]
    public async Task ListTemplates_ReturnsSuccess()
    {
        var pagedResult = new PagedResult<Template>
        {
            Items = [new Template { Id = Guid.NewGuid(), Name = "T1", OrganizationId = Guid.NewGuid() }],
            Total = 1,
            Page = 1,
            PageSize = 10
        };

        _repository
            .Setup(repository => repository.GetAllAsync("search", 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var useCase = new ListTemplates(_repository.Object);

        var result = await useCase.ExecuteAsync("search", 1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(1);
    }
}
