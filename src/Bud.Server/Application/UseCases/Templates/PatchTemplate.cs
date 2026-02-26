using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.UseCases.Templates;

public sealed partial class PatchTemplate(
    ITemplateRepository templateRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<PatchTemplate> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Template>> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        PatchTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        LogPatchingTemplate(logger, id);

        var template = await templateRepository.GetByIdWithChildrenAsync(id, cancellationToken);
        if (template is null)
        {
            LogTemplatePatchFailed(logger, id, "Not found");
            return Result<Template>.NotFound("Template de missão não encontrado.");
        }

        var canUpdate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, template.OrganizationId, cancellationToken);
        if (!canUpdate)
        {
            LogTemplatePatchFailed(logger, id, "Forbidden");
            return Result<Template>.Forbidden("Você não tem permissão para atualizar templates nesta organização.");
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
                objectiveRequests.Select(objective => new TemplateObjectiveDraft(
                    objective.Id,
                    objective.Name,
                    objective.Description,
                    objective.OrderIndex,
                    objective.Dimension)),
                metricRequests.Select(metric => new TemplateMetricDraft(
                    metric.Name,
                    metric.Type,
                    metric.OrderIndex,
                    metric.TemplateObjectiveId,
                    metric.QuantitativeType,
                    metric.MinValue,
                    metric.MaxValue,
                    metric.Unit,
                    metric.TargetText)));

            await templateRepository.RemoveObjectivesAndMetricsAsync(previousObjectives, previousMetrics, cancellationToken);
            await templateRepository.AddObjectivesAndMetricsAsync(template.Objectives, template.Metrics, cancellationToken);
            await unitOfWork.CommitAsync(templateRepository.SaveChangesAsync, cancellationToken);

            var reloadedTemplate = await templateRepository.GetByIdReadOnlyAsync(id, cancellationToken);
            LogTemplatePatched(logger, id, template.Name);
            return Result<Template>.Success(reloadedTemplate!);
        }
        catch (DomainInvariantException ex)
        {
            LogTemplatePatchFailed(logger, id, ex.Message);
            return Result<Template>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    [LoggerMessage(EventId = 4075, Level = LogLevel.Information, Message = "Patching template {TemplateId}")]
    private static partial void LogPatchingTemplate(ILogger logger, Guid templateId);

    [LoggerMessage(EventId = 4076, Level = LogLevel.Information, Message = "Template patched successfully: {TemplateId} - '{Name}'")]
    private static partial void LogTemplatePatched(ILogger logger, Guid templateId, string name);

    [LoggerMessage(EventId = 4077, Level = LogLevel.Warning, Message = "Template patch failed for {TemplateId}: {Reason}")]
    private static partial void LogTemplatePatchFailed(ILogger logger, Guid templateId, string reason);
}
