using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Ports;
using Bud.Server.Authorization;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.MissionTemplates;

public sealed class MissionTemplateCommandUseCase(
    IMissionTemplateRepository templateRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ITenantProvider tenantProvider) : IMissionTemplateCommandUseCase
{
    public async Task<Result<MissionTemplate>> CreateAsync(
        ClaimsPrincipal user,
        CreateMissionTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (tenantProvider.TenantId.HasValue)
        {
            var canCreate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, tenantProvider.TenantId.Value, cancellationToken);
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
                request.Objectives.Select(o => new MissionTemplateObjectiveDraft(
                    o.Id,
                    o.Name,
                    o.Description,
                    o.OrderIndex,
                    o.ObjectiveDimensionId)),
                request.Metrics.Select(m => new MissionTemplateMetricDraft(
                    m.Name,
                    m.Type,
                    m.OrderIndex,
                    m.MissionTemplateObjectiveId,
                    m.QuantitativeType,
                    m.MinValue,
                    m.MaxValue,
                    m.Unit,
                    m.TargetText)));

            await templateRepository.AddAsync(template, cancellationToken);
            await templateRepository.SaveChangesAsync(cancellationToken);

            return Result<MissionTemplate>.Success(template);
        }
        catch (DomainInvariantException ex)
        {
            return Result<MissionTemplate>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    public async Task<Result<MissionTemplate>> UpdateAsync(
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
                request.Objectives.Select(o => new MissionTemplateObjectiveDraft(
                    o.Id,
                    o.Name,
                    o.Description,
                    o.OrderIndex,
                    o.ObjectiveDimensionId)),
                request.Metrics.Select(m => new MissionTemplateMetricDraft(
                    m.Name,
                    m.Type,
                    m.OrderIndex,
                    m.MissionTemplateObjectiveId,
                    m.QuantitativeType,
                    m.MinValue,
                    m.MaxValue,
                    m.Unit,
                    m.TargetText)));

            await templateRepository.RemoveObjectivesAndMetrics(previousObjectives, previousMetrics, cancellationToken);
            await templateRepository.AddObjectivesAndMetrics(template.Objectives, template.Metrics, cancellationToken);
            await templateRepository.SaveChangesAsync(cancellationToken);

            // Reload to include new metrics/objectives ordered
            var reloaded = await templateRepository.GetByIdReadOnlyAsync(id, cancellationToken);
            return Result<MissionTemplate>.Success(reloaded!);
        }
        catch (DomainInvariantException ex)
        {
            return Result<MissionTemplate>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    public async Task<Result> DeleteAsync(
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
        await templateRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
