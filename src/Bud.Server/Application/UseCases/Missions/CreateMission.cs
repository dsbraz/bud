using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Authorization;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.Application.Ports;
using Bud.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace Bud.Server.Application.UseCases.Missions;

public sealed partial class CreateMission(
    IMissionRepository missionRepository,
    IMissionScopeResolver missionScopeResolver,
    IApplicationAuthorizationGateway authorizationGateway,
    ILogger<CreateMission> logger,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<Mission>> ExecuteAsync(
        ClaimsPrincipal user,
        CreateMissionRequest request,
        CancellationToken cancellationToken = default)
    {
        LogCreatingMission(logger, request.Name, request.ScopeType);

        var scopeType = request.ScopeType;
        var status = request.Status;

        var scopeResolution = await missionScopeResolver.ResolveScopeOrganizationIdAsync(
            scopeType,
            request.ScopeId,
            ignoreQueryFilters: true,
            cancellationToken: cancellationToken);

        if (!scopeResolution.IsSuccess)
        {
            var error = scopeResolution.Error ?? "Escopo não encontrado.";
            LogMissionCreationFailed(logger, request.Name, error);
            return Result<Mission>.NotFound(error);
        }

        var canCreate = await authorizationGateway.CanAccessTenantOrganizationAsync(user, scopeResolution.Value, cancellationToken);
        if (!canCreate)
        {
            LogMissionCreationFailed(logger, request.Name, "Forbidden");
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

            LogMissionCreated(logger, mission.Id, mission.Name);
            return Result<Mission>.Success(mission);
        }
        catch (DomainInvariantException ex)
        {
            LogMissionCreationFailed(logger, request.Name, ex.Message);
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

    [LoggerMessage(EventId = 4000, Level = LogLevel.Information, Message = "Creating mission '{Name}' with scope type '{ScopeType}'")]
    private static partial void LogCreatingMission(ILogger logger, string name, MissionScopeType scopeType);

    [LoggerMessage(EventId = 4001, Level = LogLevel.Information, Message = "Mission created successfully: {MissionId} - '{Name}'")]
    private static partial void LogMissionCreated(ILogger logger, Guid missionId, string name);

    [LoggerMessage(EventId = 4002, Level = LogLevel.Warning, Message = "Mission creation failed for '{Name}': {Reason}")]
    private static partial void LogMissionCreationFailed(ILogger logger, string name, string reason);
}
