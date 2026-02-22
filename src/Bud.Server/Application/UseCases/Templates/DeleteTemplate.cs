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

public sealed class DeleteTemplate(
    ITemplateRepository templateRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
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
        await unitOfWork.CommitAsync(templateRepository.SaveChangesAsync, cancellationToken);

        return Result.Success();
    }
}

