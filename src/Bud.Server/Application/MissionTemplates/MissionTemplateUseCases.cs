using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.MissionTemplates;

public sealed class CreateStrategicMissionTemplate(
    IMissionTemplateRepository templateRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ITenantProvider tenantProvider,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<MissionTemplate>> ExecuteAsync(
        ClaimsPrincipal user,
        CreateMissionTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (tenantProvider.TenantId.HasValue)
        {
            var canCreate = await authorizationGateway.CanAccessTenantOrganizationAsync(
                user,
                tenantProvider.TenantId.Value,
                cancellationToken);
            if (!canCreate)
            {
                return Result<MissionTemplate>.Forbidden("Você não tem permissão para criar templates nesta organização.");
            }
        }

        try
        {
            var template = MissionTemplate.Create(
                Guid.NewGuid(),
                Guid.Empty,
                request.Name,
                request.Description,
                request.MissionNamePattern,
                request.MissionDescriptionPattern);

            template.ReplaceObjectivesAndMetrics(
                request.Objectives.Select(objective => new MissionTemplateObjectiveDraft(
                    objective.Id,
                    objective.Name,
                    objective.Description,
                    objective.OrderIndex,
                    objective.ObjectiveDimensionId)),
                request.Metrics.Select(metric => new MissionTemplateMetricDraft(
                    metric.Name,
                    metric.Type.ToDomain(),
                    metric.OrderIndex,
                    metric.MissionTemplateObjectiveId,
                    metric.QuantitativeType.ToDomain(),
                    metric.MinValue,
                    metric.MaxValue,
                    metric.Unit.ToDomain(),
                    metric.TargetText)));

            await templateRepository.AddAsync(template, cancellationToken);
            await unitOfWork.CommitAsync(templateRepository.SaveChangesAsync, cancellationToken);

            return Result<MissionTemplate>.Success(template);
        }
        catch (DomainInvariantException ex)
        {
            return Result<MissionTemplate>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}

public sealed class ReviseStrategicMissionTemplate(
    IMissionTemplateRepository templateRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<MissionTemplate>> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateMissionTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var template = await templateRepository.GetByIdWithChildrenAsync(id, cancellationToken);
        if (template is null)
        {
            return Result<MissionTemplate>.NotFound("Template de missão não encontrado.");
        }

        var canUpdate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, template.OrganizationId, cancellationToken);
        if (!canUpdate)
        {
            return Result<MissionTemplate>.Forbidden("Você não tem permissão para atualizar templates nesta organização.");
        }

        try
        {
            template.UpdateBasics(
                request.Name,
                request.Description,
                request.MissionNamePattern,
                request.MissionDescriptionPattern);

            var previousMetrics = template.Metrics.ToList();
            var previousObjectives = template.Objectives.ToList();

            template.ReplaceObjectivesAndMetrics(
                request.Objectives.Select(objective => new MissionTemplateObjectiveDraft(
                    objective.Id,
                    objective.Name,
                    objective.Description,
                    objective.OrderIndex,
                    objective.ObjectiveDimensionId)),
                request.Metrics.Select(metric => new MissionTemplateMetricDraft(
                    metric.Name,
                    metric.Type.ToDomain(),
                    metric.OrderIndex,
                    metric.MissionTemplateObjectiveId,
                    metric.QuantitativeType.ToDomain(),
                    metric.MinValue,
                    metric.MaxValue,
                    metric.Unit.ToDomain(),
                    metric.TargetText)));

            await templateRepository.RemoveObjectivesAndMetrics(previousObjectives, previousMetrics, cancellationToken);
            await templateRepository.AddObjectivesAndMetrics(template.Objectives, template.Metrics, cancellationToken);
            await unitOfWork.CommitAsync(templateRepository.SaveChangesAsync, cancellationToken);

            var reloadedTemplate = await templateRepository.GetByIdReadOnlyAsync(id, cancellationToken);
            return Result<MissionTemplate>.Success(reloadedTemplate!);
        }
        catch (DomainInvariantException ex)
        {
            return Result<MissionTemplate>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}

public sealed class RemoveStrategicMissionTemplate(
    IMissionTemplateRepository templateRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var template = await templateRepository.GetByIdAsync(id, cancellationToken);
        if (template is null)
        {
            return Result.NotFound("Template de missão não encontrado.");
        }

        var canDelete = await authorizationGateway.CanAccessTenantOrganizationAsync(user, template.OrganizationId, cancellationToken);
        if (!canDelete)
        {
            return Result.Forbidden("Você não tem permissão para excluir templates nesta organização.");
        }

        await templateRepository.RemoveAsync(template, cancellationToken);
        await unitOfWork.CommitAsync(templateRepository.SaveChangesAsync, cancellationToken);

        return Result.Success();
    }
}

public sealed class ViewStrategicMissionTemplate(IMissionTemplateRepository templateRepository)
{
    public async Task<Result<MissionTemplate>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await templateRepository.GetByIdReadOnlyAsync(id, cancellationToken);
        return template is null
            ? Result<MissionTemplate>.NotFound("Template de missão não encontrado.")
            : Result<MissionTemplate>.Success(template);
    }
}

public sealed class ListMissionTemplates(IMissionTemplateRepository templateRepository)
{
    public async Task<Result<PagedResult<MissionTemplate>>> ExecuteAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);
        var result = await templateRepository.GetAllAsync(search, page, pageSize, cancellationToken);
        return Result<PagedResult<MissionTemplate>>.Success(result);
    }
}
