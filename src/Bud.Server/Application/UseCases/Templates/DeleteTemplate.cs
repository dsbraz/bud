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
            return Result.NotFound("Template de missão não encontrado.");
        }

        var canDelete = await authorizationGateway.CanAccessTenantOrganizationAsync(user, template.OrganizationId, cancellationToken);
        if (!canDelete)
        {
            LogTemplateDeletionFailed(logger, id, "Forbidden");
            return Result.Forbidden("Você não tem permissão para excluir templates nesta organização.");
        }

        await templateRepository.RemoveAsync(template, cancellationToken);
        await unitOfWork.CommitAsync(templateRepository.SaveChangesAsync, cancellationToken);

        LogTemplateDeleted(logger, id);
        return Result.Success();
    }

    [LoggerMessage(EventId = 4078, Level = LogLevel.Information, Message = "Deleting template {TemplateId}")]
    private static partial void LogDeletingTemplate(ILogger logger, Guid templateId);

    [LoggerMessage(EventId = 4079, Level = LogLevel.Information, Message = "Template deleted successfully: {TemplateId}")]
    private static partial void LogTemplateDeleted(ILogger logger, Guid templateId);

    [LoggerMessage(EventId = 4080, Level = LogLevel.Warning, Message = "Template deletion failed for {TemplateId}: {Reason}")]
    private static partial void LogTemplateDeletionFailed(ILogger logger, Guid templateId, string reason);
}
