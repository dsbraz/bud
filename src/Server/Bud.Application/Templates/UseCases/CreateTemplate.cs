using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Ports;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace Bud.Application.Templates;

public sealed partial class CreateTemplate(
    ITemplateRepository templateRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ITenantProvider tenantProvider,
    ILogger<CreateTemplate> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Template>> ExecuteAsync(
        ClaimsPrincipal user,
        CreateTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        LogCreatingTemplate(logger, request.Name);

        if (tenantProvider.TenantId.HasValue)
        {
            var canCreate = await authorizationGateway.CanAccessTenantOrganizationAsync(
                user,
                tenantProvider.TenantId.Value,
                cancellationToken);
            if (!canCreate)
            {
                LogTemplateCreationFailed(logger, request.Name, "Forbidden");
                return Result<Template>.Forbidden(UserErrorMessages.TemplateCreateForbidden);
            }
        }

        try
        {
            var template = Template.Create(
                Guid.NewGuid(),
                Guid.Empty,
                request.Name,
                request.Description,
                request.GoalNamePattern,
                request.GoalDescriptionPattern);

            template.ReplaceGoalsAndIndicators(
                request.Goals.Select(goal => new TemplateGoalDraft(
                    goal.Id,
                    goal.ParentId,
                    goal.Name,
                    goal.Description,
                    goal.OrderIndex,
                    goal.Dimension)),
                request.Indicators.Select(indicator => new TemplateIndicatorDraft(
                    indicator.Name,
                    indicator.Type,
                    indicator.OrderIndex,
                    indicator.TemplateGoalId,
                    indicator.QuantitativeType,
                    indicator.MinValue,
                    indicator.MaxValue,
                    indicator.Unit,
                    indicator.TargetText)));

            await templateRepository.AddAsync(template, cancellationToken);
            await unitOfWork.CommitAsync(templateRepository.SaveChangesAsync, cancellationToken);

            LogTemplateCreated(logger, template.Id, template.Name);
            return Result<Template>.Success(template);
        }
        catch (DomainInvariantException ex)
        {
            LogTemplateCreationFailed(logger, request.Name, ex.Message);
            return Result<Template>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    [LoggerMessage(EventId = 4070, Level = LogLevel.Information, Message = "Creating template '{Name}'")]
    private static partial void LogCreatingTemplate(ILogger logger, string name);

    [LoggerMessage(EventId = 4071, Level = LogLevel.Information, Message = "Template created successfully: {TemplateId} - '{Name}'")]
    private static partial void LogTemplateCreated(ILogger logger, Guid templateId, string name);

    [LoggerMessage(EventId = 4072, Level = LogLevel.Warning, Message = "Template creation failed for '{Name}': {Reason}")]
    private static partial void LogTemplateCreationFailed(ILogger logger, string name, string reason);
}
