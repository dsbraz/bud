using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.ReadModels;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Domain.ValueObjects;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.UseCases.Collaborators;

public sealed class CreateCollaborator(
    ICollaboratorRepository collaboratorRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ITenantProvider tenantProvider,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Collaborator>> ExecuteAsync(
        ClaimsPrincipal user,
        CreateCollaboratorRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = tenantProvider.TenantId;
        if (!organizationId.HasValue)
        {
            return Result<Collaborator>.Failure("Contexto de organização não encontrado.", ErrorType.Validation);
        }

        var canCreate = await authorizationGateway.IsOrganizationOwnerAsync(user, organizationId.Value, cancellationToken);
        if (!canCreate)
        {
            return Result<Collaborator>.Forbidden("Apenas o proprietário da organização pode criar colaboradores.");
        }

        if (!EmailAddress.TryCreate(request.Email, out var emailAddress))
        {
            return Result<Collaborator>.Failure("E-mail inválido.", ErrorType.Validation);
        }

        if (!PersonName.TryCreate(request.FullName, out var personName))
        {
            return Result<Collaborator>.Failure("O nome do colaborador é obrigatório.", ErrorType.Validation);
        }

        try
        {
            var requestedRole = request.Role.ToDomain();

            var collaborator = Collaborator.Create(
                Guid.NewGuid(),
                organizationId.Value,
                personName.Value,
                emailAddress.Value,
                requestedRole,
                request.LeaderId);

            await collaboratorRepository.AddAsync(collaborator, cancellationToken);
            await unitOfWork.CommitAsync(collaboratorRepository.SaveChangesAsync, cancellationToken);

            return Result<Collaborator>.Success(collaborator);
        }
        catch (DomainInvariantException ex)
        {
            return Result<Collaborator>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}

