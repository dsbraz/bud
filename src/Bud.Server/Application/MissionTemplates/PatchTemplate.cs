using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.MissionTemplates;

public sealed class PatchTemplate(
    IMissionTemplateRepository templateRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<MissionTemplate>> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        PatchMissionTemplateRequest request,
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
                    objective.Dimension)),
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

