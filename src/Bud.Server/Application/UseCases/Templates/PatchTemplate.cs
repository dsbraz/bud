using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.UseCases.Templates;

public sealed class PatchTemplate(
    ITemplateRepository templateRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<MissionTemplate>> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        PatchTemplateRequest request,
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
                request.Name.HasValue ? (request.Name.Value ?? template.Name) : template.Name,
                request.Description.HasValue ? request.Description.Value : template.Description,
                request.MissionNamePattern.HasValue ? request.MissionNamePattern.Value : template.MissionNamePattern,
                request.MissionDescriptionPattern.HasValue ? request.MissionDescriptionPattern.Value : template.MissionDescriptionPattern);

            var previousMetrics = template.Metrics.ToList();
            var previousObjectives = template.Objectives.ToList();
            var objectiveRequests = request.Objectives.AsEnumerable().ToList();
            var metricRequests = request.Metrics.AsEnumerable().ToList();

            template.ReplaceObjectivesAndMetrics(
                objectiveRequests.Select(objective => new MissionTemplateObjectiveDraft(
                    objective.Id,
                    objective.Name,
                    objective.Description,
                    objective.OrderIndex,
                    objective.Dimension)),
                metricRequests.Select(metric => new MissionTemplateMetricDraft(
                    metric.Name,
                    metric.Type.ToDomain(),
                    metric.OrderIndex,
                    metric.TemplateObjectiveId,
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
