using Bud.Application.Common;
using Bud.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Bud.Application.Features.Templates.UseCases;

public sealed record PatchTemplateCommand(
    Optional<string> Name,
    Optional<string?> Description,
    Optional<string?> GoalNamePattern,
    Optional<string?> GoalDescriptionPattern,
    IReadOnlyList<TemplateGoalDraft> Goals,
    IReadOnlyList<TemplateIndicatorDraft> Indicators);

public sealed partial class PatchTemplate(
    ITemplateRepository templateRepository,
    ILogger<PatchTemplate> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Template>> ExecuteAsync(
        Guid id,
        PatchTemplateCommand command,
        CancellationToken cancellationToken = default)
    {
        LogPatchingTemplate(logger, id);

        var template = await templateRepository.GetByIdWithChildrenAsync(id, cancellationToken);
        if (template is null)
        {
            LogTemplatePatchFailed(logger, id, "Not found");
            return Result<Template>.NotFound(UserErrorMessages.TemplateNotFound);
        }

        try
        {
            template.UpdateBasics(
                command.Name.HasValue ? (command.Name.Value ?? template.Name) : template.Name,
                command.Description.HasValue ? command.Description.Value : template.Description,
                command.GoalNamePattern.HasValue ? command.GoalNamePattern.Value : template.GoalNamePattern,
                command.GoalDescriptionPattern.HasValue ? command.GoalDescriptionPattern.Value : template.GoalDescriptionPattern);

            var previousIndicators = template.Indicators.ToList();
            var previousGoals = template.Goals.ToList();

            template.ReplaceGoalsAndIndicators(command.Goals, command.Indicators);

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

    [LoggerMessage(EventId = 4073, Level = LogLevel.Information, Message = "Patching template {TemplateId}")]
    private static partial void LogPatchingTemplate(ILogger logger, Guid templateId);

    [LoggerMessage(EventId = 4074, Level = LogLevel.Information, Message = "Template patched successfully: {TemplateId} - '{Name}'")]
    private static partial void LogTemplatePatched(ILogger logger, Guid templateId, string name);

    [LoggerMessage(EventId = 4075, Level = LogLevel.Warning, Message = "Template patch failed for {TemplateId}: {Reason}")]
    private static partial void LogTemplatePatchFailed(ILogger logger, Guid templateId, string reason);
}
