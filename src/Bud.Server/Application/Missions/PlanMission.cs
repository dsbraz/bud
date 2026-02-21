using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Infrastructure.Services;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.Missions;

public sealed class PlanMission(
    IMissionRepository missionRepository,
    IMissionScopeResolver missionScopeResolver,
    IApplicationAuthorizationGateway authorizationGateway,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Mission>> ExecuteAsync(
        ClaimsPrincipal user,
        CreateMissionRequest request,
        CancellationToken cancellationToken = default)
    {
        var scopeType = request.ScopeType.ToDomain();
        var status = request.Status.ToDomain();

        var scopeResolution = await missionScopeResolver.ResolveScopeOrganizationIdAsync(
            scopeType,
            request.ScopeId,
            ignoreQueryFilters: true,
            cancellationToken: cancellationToken);

        if (!scopeResolution.IsSuccess)
        {
            return Result<Mission>.NotFound(scopeResolution.Error ?? "Escopo não encontrado.");
        }

        var canCreate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, scopeResolution.Value, cancellationToken);
        if (!canCreate)
        {
            return Result<Mission>.Forbidden("Você não tem permissão para criar missões nesta organização.");
        }

        try
        {
            var missionScope = MissionScope.Create(scopeType, request.ScopeId);

            var mission = Mission.Create(
                Guid.NewGuid(),
                scopeResolution.Value,
                request.Name,
                request.Description,
                NormalizeToUtc(request.StartDate),
                NormalizeToUtc(request.EndDate),
                status);

            mission.SetScope(missionScope);

            await missionRepository.AddAsync(mission, cancellationToken);
            await unitOfWork.CommitAsync(missionRepository.SaveChangesAsync, cancellationToken);

            return Result<Mission>.Success(mission);
        }
        catch (DomainInvariantException ex)
        {
            return Result<Mission>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    private static DateTime NormalizeToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value.ToUniversalTime()
        };
    }
}
