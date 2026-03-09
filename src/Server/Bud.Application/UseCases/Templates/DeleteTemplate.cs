using System.Security.Claims;
using Bud.Application.Common;
using Bud.Application.Ports;
using Bud.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Bud.Application.UseCases.Templates;

public sealed partial class DeleteTemplate(
    ITemplateRepository templateRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<DeleteTemplate> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        LogDeletingTemplate(logger, id);

        var template = await templateRepository.GetByIdAsync(id, cancellationToken);
        if (template is null)
        {
            LogTemplateDeletionFailed(logger, id, "Not found");
            return Result.NotFound(UserErrorMessages.TemplateNotFound);
        }

        var canDelete = await authorizationGateway.CanAccessTenantOrganizationAsync(user, template.OrganizationId, cancellationToken);
        if (!canDelete)
        {
            LogTemplateDeletionFailed(logger, id, "Forbidden");
            return Result.Forbidden(UserErrorMessages.TemplateDeleteForbidden);
        }

        await templateRepository.RemoveAsync(template, cancellationToken);
        await unitOfWork.CommitAsync(templateRepository.SaveChangesAsync, cancellationToken);

        LogTemplateDeleted(logger, id);
        return Result.Success();
    }

    [LoggerMessage(EventId = 4076, Level = LogLevel.Information, Message = "Deleting template {TemplateId}")]
    private static partial void LogDeletingTemplate(ILogger logger, Guid templateId);

    [LoggerMessage(EventId = 4077, Level = LogLevel.Information, Message = "Template deleted successfully: {TemplateId}")]
    private static partial void LogTemplateDeleted(ILogger logger, Guid templateId);

    [LoggerMessage(EventId = 4078, Level = LogLevel.Warning, Message = "Template deletion failed for {TemplateId}: {Reason}")]
    private static partial void LogTemplateDeletionFailed(ILogger logger, Guid templateId, string reason);
}
