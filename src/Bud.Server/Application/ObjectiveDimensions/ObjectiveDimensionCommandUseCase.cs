using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Ports;
using Bud.Server.Authorization;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.ObjectiveDimensions;

public sealed class ObjectiveDimensionCommandUseCase(
    IObjectiveDimensionRepository dimensionRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ITenantProvider tenantProvider) : IObjectiveDimensionCommandUseCase
{
    public async Task<Result<ObjectiveDimension>> CreateAsync(
        ClaimsPrincipal user,
        CreateObjectiveDimensionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (tenantProvider.TenantId.HasValue)
        {
            var canCreate = await authorizationGateway.CanAccessTenantOrganizationAsync(
                user,
                tenantProvider.TenantId.Value,
                cancellationToken);

            if (!canCreate)
            {
                return Result<ObjectiveDimension>.Forbidden(
                    "Você não tem permissão para criar dimensões nesta organização.");
            }
        }

        var normalizedName = request.Name.Trim();
        var isUnique = await dimensionRepository.IsNameUniqueAsync(normalizedName, null, cancellationToken);
        if (!isUnique)
        {
            return Result<ObjectiveDimension>.Failure(
                "Já existe uma dimensão com este nome.",
                ErrorType.Conflict);
        }

        try
        {
            var dimension = ObjectiveDimension.Create(Guid.NewGuid(), Guid.Empty, request.Name);
            await dimensionRepository.AddAsync(dimension, cancellationToken);
            await dimensionRepository.SaveChangesAsync(cancellationToken);

            return Result<ObjectiveDimension>.Success(dimension);
        }
        catch (DomainInvariantException ex)
        {
            return Result<ObjectiveDimension>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    public async Task<Result<ObjectiveDimension>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateObjectiveDimensionRequest request,
        CancellationToken cancellationToken = default)
    {
        var dimension = await dimensionRepository.GetByIdTrackedAsync(id, cancellationToken);
        if (dimension is null)
        {
            return Result<ObjectiveDimension>.NotFound("Dimensão do objetivo não encontrada.");
        }

        var canUpdate = await authorizationGateway.CanAccessTenantOrganizationAsync(
            user,
            dimension.OrganizationId,
            cancellationToken);
        if (!canUpdate)
        {
            return Result<ObjectiveDimension>.Forbidden(
                "Você não tem permissão para atualizar dimensões nesta organização.");
        }

        var normalizedName = request.Name.Trim();
        var isUnique = await dimensionRepository.IsNameUniqueAsync(normalizedName, id, cancellationToken);
        if (!isUnique)
        {
            return Result<ObjectiveDimension>.Failure(
                "Já existe uma dimensão com este nome.",
                ErrorType.Conflict);
        }

        try
        {
            dimension.Rename(request.Name);
            await dimensionRepository.SaveChangesAsync(cancellationToken);

            return Result<ObjectiveDimension>.Success(dimension);
        }
        catch (DomainInvariantException ex)
        {
            return Result<ObjectiveDimension>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    public async Task<Result> DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var dimension = await dimensionRepository.GetByIdTrackedAsync(id, cancellationToken);
        if (dimension is null)
        {
            return Result.NotFound("Dimensão do objetivo não encontrada.");
        }

        var canDelete = await authorizationGateway.CanAccessTenantOrganizationAsync(
            user,
            dimension.OrganizationId,
            cancellationToken);
        if (!canDelete)
        {
            return Result.Forbidden(
                "Você não tem permissão para excluir dimensões nesta organização.");
        }

        if (await dimensionRepository.HasObjectivesAsync(id, cancellationToken))
        {
            return Result.Failure(
                "Não é possível excluir esta dimensão porque existem objetivos vinculados a ela.",
                ErrorType.Conflict);
        }

        if (await dimensionRepository.HasTemplateObjectivesAsync(id, cancellationToken))
        {
            return Result.Failure(
                "Não é possível excluir esta dimensão porque existem objetivos de template vinculados a ela.",
                ErrorType.Conflict);
        }

        await dimensionRepository.RemoveAsync(dimension, cancellationToken);
        await dimensionRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
