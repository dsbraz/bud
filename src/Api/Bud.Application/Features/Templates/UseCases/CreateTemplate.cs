using Bud.Application.Common;
using Bud.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Bud.Application.Features.Templates.UseCases;

public sealed record CreateTemplateCommand(
    string Name,
    string? Description,
    string? GoalNamePattern,
    string? GoalDescriptionPattern,
    IReadOnlyList<TemplateGoalDraft> Goals,
    IReadOnlyList<TemplateIndicatorDraft> Indicators);

public sealed partial class CreateTemplate(
    ITemplateRepository templateRepository,
    ILogger<CreateTemplate> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Template>> ExecuteAsync(
        CreateTemplateCommand command,
        CancellationToken cancellationToken = default)
    {
        LogCreatingTemplate(logger, command.Name);

        try
        {
            var template = Template.Create(
                Guid.NewGuid(),
                Guid.Empty,
                command.Name,
                command.Description,
                command.GoalNamePattern,
                command.GoalDescriptionPattern);

            template.ReplaceGoalsAndIndicators(command.Goals, command.Indicators);

            await templateRepository.AddAsync(template, cancellationToken);
            await unitOfWork.CommitAsync(templateRepository.SaveChangesAsync, cancellationToken);

            LogTemplateCreated(logger, template.Id, template.Name);
            return Result<Template>.Success(template);
        }
        catch (DomainInvariantException ex)
        {
            LogTemplateCreationFailed(logger, command.Name, ex.Message);
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
