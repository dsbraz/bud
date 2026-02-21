using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.ObjectiveDimensions;

public sealed class RegisterStrategicDimension(
    IObjectiveDimensionRepository dimensionRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ITenantProvider tenantProvider,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<ObjectiveDimension>> ExecuteAsync(
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
            await unitOfWork.CommitAsync(dimensionRepository.SaveChangesAsync, cancellationToken);

            return Result<ObjectiveDimension>.Success(dimension);
        }
        catch (DomainInvariantException ex)
        {
            return Result<ObjectiveDimension>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}

public sealed class RenameStrategicDimension(
    IObjectiveDimensionRepository dimensionRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<ObjectiveDimension>> ExecuteAsync(
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
            await unitOfWork.CommitAsync(dimensionRepository.SaveChangesAsync, cancellationToken);

            return Result<ObjectiveDimension>.Success(dimension);
        }
        catch (DomainInvariantException ex)
        {
            return Result<ObjectiveDimension>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}

public sealed class RemoveStrategicDimension(
    IObjectiveDimensionRepository dimensionRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result> ExecuteAsync(
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
        await unitOfWork.CommitAsync(dimensionRepository.SaveChangesAsync, cancellationToken);

        return Result.Success();
    }
}

public sealed class ViewStrategicDimensionDetails(IObjectiveDimensionRepository dimensionRepository)
{
    public async Task<Result<ObjectiveDimension>> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var dimension = await dimensionRepository.GetByIdAsync(id, cancellationToken);
        return dimension is null
            ? Result<ObjectiveDimension>.NotFound("Dimensão do objetivo não encontrada.")
            : Result<ObjectiveDimension>.Success(dimension);
    }
}

public sealed class ListStrategicDimensions(IObjectiveDimensionRepository dimensionRepository)
{
    public async Task<Result<PagedResult<ObjectiveDimension>>> ExecuteAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);
        var result = await dimensionRepository.GetAllAsync(search, page, pageSize, cancellationToken);
        return Result<PagedResult<ObjectiveDimension>>.Success(result);
    }
}
