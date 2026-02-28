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
            return Result<Template>.NotFound("Template de meta não encontrado.");
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
                request.GoalNamePattern.HasValue ? request.GoalNamePattern.Value : template.GoalNamePattern,
                request.GoalDescriptionPattern.HasValue ? request.GoalDescriptionPattern.Value : template.GoalDescriptionPattern);

            var previousIndicators = template.Indicators.ToList();
            var previousGoals = template.Goals.ToList();
            var goalRequests = request.Goals.AsEnumerable().ToList();
            var indicatorRequests = request.Indicators.AsEnumerable().ToList();

            template.ReplaceGoalsAndIndicators(
                goalRequests.Select(goal => new TemplateGoalDraft(
                    goal.Id,
                    goal.ParentId,
                    goal.Name,
                    goal.Description,
                    goal.OrderIndex,
                    goal.Dimension)),
                indicatorRequests.Select(indicator => new TemplateIndicatorDraft(
                    indicator.Name,
                    indicator.Type,
                    indicator.OrderIndex,
                    indicator.TemplateGoalId,
                    indicator.QuantitativeType,
                    indicator.MinValue,
                    indicator.MaxValue,
                    indicator.Unit,
                    indicator.TargetText)));

            await templateRepository.RemoveGoalsAndIndicatorsAsync(previousGoals, previousIndicators, cancellationToken);
            await templateRepository.AddGoalsAndIndicatorsAsync(template.Goals, template.Indicators, cancellationToken);
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
