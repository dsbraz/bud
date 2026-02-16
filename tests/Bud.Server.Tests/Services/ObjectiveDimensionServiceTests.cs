using Bud.Server.Data;
using Bud.Server.MultiTenancy;
using Bud.Server.Services;
using Bud.Server.Tests.Helpers;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bud.Server.Tests.Services;

public sealed class ObjectiveDimensionServiceTests
{
    private readonly TestTenantProvider _tenantProvider = new() { IsGlobalAdmin = true };

    private ApplicationDbContext CreateInMemoryContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .AddInterceptors(new TenantSaveChangesInterceptor(_tenantProvider))
            .Options;

        return new ApplicationDbContext(options, _tenantProvider);
    }

    private static async Task<Organization> CreateTestOrganization(ApplicationDbContext context, string name = "Org Teste")
    {
        var organization = Organization.Create(Guid.NewGuid(), name, Guid.NewGuid());
        context.Organizations.Add(organization);
        await context.SaveChangesAsync();
        return organization;
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesDimension()
    {
        using var context = CreateInMemoryContext();
        var organization = await CreateTestOrganization(context);
        _tenantProvider.TenantId = organization.Id;
        var service = new ObjectiveDimensionService(context);

        var result = await service.CreateAsync(new CreateObjectiveDimensionRequest
        {
            Name = "  Clientes  "
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Clientes");
        result.Value.OrganizationId.Should().Be(organization.Id);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateNameIgnoringCase_ReturnsConflict()
    {
        using var context = CreateInMemoryContext();
        var organization = await CreateTestOrganization(context);
        _tenantProvider.TenantId = organization.Id;
        var service = new ObjectiveDimensionService(context);

        _ = await service.CreateAsync(new CreateObjectiveDimensionRequest { Name = "Clientes" });

        var result = await service.CreateAsync(new CreateObjectiveDimensionRequest { Name = "clientes" });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Conflict);
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_UpdatesDimension()
    {
        using var context = CreateInMemoryContext();
        var organization = await CreateTestOrganization(context);
        _tenantProvider.TenantId = organization.Id;
        var service = new ObjectiveDimensionService(context);

        var created = await service.CreateAsync(new CreateObjectiveDimensionRequest { Name = "Clientes" });

        var result = await service.UpdateAsync(created.Value!.Id, new UpdateObjectiveDimensionRequest
        {
            Name = "Processos"
        });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Processos");
    }

    [Fact]
    public async Task DeleteAsync_WhenDimensionInUse_ReturnsConflict()
    {
        using var context = CreateInMemoryContext();
        var organization = await CreateTestOrganization(context);
        _tenantProvider.TenantId = organization.Id;
        var service = new ObjectiveDimensionService(context);

        var dimension = (await service.CreateAsync(new CreateObjectiveDimensionRequest
        {
            Name = "Clientes"
        })).Value!;

        var mission = Mission.Create(
            Guid.NewGuid(),
            organization.Id,
            "Missão Teste",
            null,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(7),
            MissionStatus.Active);
        context.Missions.Add(mission);
        await context.SaveChangesAsync();

        var objective = MissionObjective.Create(
            Guid.NewGuid(),
            organization.Id,
            mission.Id,
            "Objetivo",
            null,
            dimension.Id);
        context.MissionObjectives.Add(objective);
        await context.SaveChangesAsync();

        var result = await service.DeleteAsync(dimension.Id);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Conflict);
    }

    [Fact]
    public async Task DeleteAsync_WhenUsedByTemplateObjectives_ReturnsConflict()
    {
        using var context = CreateInMemoryContext();
        var organization = await CreateTestOrganization(context);
        _tenantProvider.TenantId = organization.Id;
        var service = new ObjectiveDimensionService(context);

        var dimension = (await service.CreateAsync(new CreateObjectiveDimensionRequest
        {
            Name = "Processos"
        })).Value!;

        var template = new MissionTemplate
        {
            Id = Guid.NewGuid(),
            Name = "Template Teste",
            OrganizationId = organization.Id,
            IsDefault = false,
            IsActive = true
        };
        context.MissionTemplates.Add(template);
        await context.SaveChangesAsync();

        var templateObjective = new MissionTemplateObjective
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            MissionTemplateId = template.Id,
            Name = "Objetivo Template",
            OrderIndex = 0,
            ObjectiveDimensionId = dimension.Id
        };
        context.MissionTemplateObjectives.Add(templateObjective);
        await context.SaveChangesAsync();

        var result = await service.DeleteAsync(dimension.Id);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ServiceErrorType.Conflict);
        result.Error.Should().Contain("objetivos de template");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedDimensions()
    {
        using var context = CreateInMemoryContext();
        var organization = await CreateTestOrganization(context);
        _tenantProvider.TenantId = organization.Id;
        var service = new ObjectiveDimensionService(context);

        _ = await service.CreateAsync(new CreateObjectiveDimensionRequest { Name = "Clientes" });
        _ = await service.CreateAsync(new CreateObjectiveDimensionRequest { Name = "Processos" });

        var result = await service.GetAllAsync(null, 1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
    }
}
