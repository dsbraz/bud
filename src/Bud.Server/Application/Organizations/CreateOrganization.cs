using Bud.Server.Application.Common;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Settings;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Options;

namespace Bud.Server.Application.Organizations;

public sealed class CreateOrganization(
    IOrganizationRepository organizationRepository,
    ICollaboratorRepository collaboratorRepository,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Organization>> ExecuteAsync(
        CreateOrganizationRequest request,
        CancellationToken cancellationToken = default)
    {
        var owner = await collaboratorRepository.GetByIdAsync(request.OwnerId, cancellationToken);
        if (owner is null)
        {
            return Result<Organization>.NotFound("O líder selecionado não foi encontrado.");
        }

        if (owner.Role != Bud.Server.Domain.Model.CollaboratorRole.Leader)
        {
            return Result<Organization>.Failure(
                "O proprietário da organização deve ter a função de Líder.",
                ErrorType.Validation);
        }

        try
        {
            var organization = Organization.Create(Guid.NewGuid(), request.Name, request.OwnerId);

            await organizationRepository.AddAsync(organization, cancellationToken);
            await unitOfWork.CommitAsync(organizationRepository.SaveChangesAsync, cancellationToken);

            var result = await organizationRepository.GetByIdWithOwnerAsync(organization.Id, cancellationToken);
            return Result<Organization>.Success(result!);
        }
        catch (DomainInvariantException ex)
        {
            return Result<Organization>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}

