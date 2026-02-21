using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.MetricCheckins;

public sealed class RegisterMetricCheckin(
    IMetricCheckinRepository checkinRepository,
    ICollaboratorRepository collaboratorRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ITenantProvider tenantProvider,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<MetricCheckin>> ExecuteAsync(
        ClaimsPrincipal user,
        CreateMetricCheckinRequest request,
        CancellationToken cancellationToken = default)
    {
        var metric = await checkinRepository.GetMetricWithMissionAsync(
            request.MissionMetricId,
            cancellationToken);

        if (metric is null)
        {
            return Result<MetricCheckin>.NotFound("Métrica não encontrada.");
        }

        var hasTenantAccess = await authorizationGateway.CanAccessTenantOrganizationAsync(user, metric.OrganizationId, cancellationToken);
        if (!hasTenantAccess)
        {
            return Result<MetricCheckin>.Forbidden("Você não tem permissão para criar check-ins nesta métrica.");
        }

        var mission = metric.Mission;
        var hasScopeAccess = await authorizationGateway.CanAccessMissionScopeAsync(
            user,
            mission.WorkspaceId,
            mission.TeamId,
            mission.CollaboratorId,
            cancellationToken);
        if (!hasScopeAccess)
        {
            return Result<MetricCheckin>.Forbidden("Você não tem permissão para fazer check-in nesta métrica.");
        }

        var collaboratorId = tenantProvider.CollaboratorId;
        if (!collaboratorId.HasValue)
        {
            return Result<MetricCheckin>.Forbidden("Colaborador não identificado.");
        }

        var collaborator = await collaboratorRepository.GetByIdAsync(collaboratorId.Value, cancellationToken);
        if (collaborator is null)
        {
            return Result<MetricCheckin>.Forbidden("Colaborador não encontrado.");
        }

        if (mission.Status != Bud.Server.Domain.Model.MissionStatus.Active)
        {
            return Result<MetricCheckin>.Failure(
                "Não é possível fazer check-in em métricas de missões que não estão ativas.",
                ErrorType.Validation);
        }

        if (metric.OrganizationId == Guid.Empty)
        {
            return Result<MetricCheckin>.Failure("Métrica sem organização definida.", ErrorType.Validation);
        }

        try
        {
            var checkin = metric.CreateCheckin(
                Guid.NewGuid(),
                collaboratorId.Value,
                request.Value,
                request.Text,
                DateTime.SpecifyKind(request.CheckinDate, DateTimeKind.Utc),
                request.Note,
                request.ConfidenceLevel);

            await checkinRepository.AddAsync(checkin, cancellationToken);
            await unitOfWork.CommitAsync(checkinRepository.SaveChangesAsync, cancellationToken);

            return Result<MetricCheckin>.Success(checkin);
        }
        catch (DomainInvariantException ex)
        {
            return Result<MetricCheckin>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}
