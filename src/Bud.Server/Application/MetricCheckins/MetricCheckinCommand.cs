using Bud.Server.Infrastructure.Repositories;
using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Application.Notifications;
using Bud.Server.Authorization;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.MetricCheckins;

public sealed class MetricCheckinCommand(
    IMetricCheckinRepository checkinRepository,
    ICollaboratorRepository collaboratorRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ITenantProvider tenantProvider,
    NotificationOrchestrator notificationOrchestrator)
{
    public async Task<Result<MetricCheckin>> CreateAsync(
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

        if (mission.Status != MissionStatus.Active)
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
            await checkinRepository.SaveChangesAsync(cancellationToken);

            await notificationOrchestrator.NotifyMetricCheckinCreatedAsync(
                checkin.Id,
                checkin.MissionMetricId,
                checkin.OrganizationId,
                checkin.CollaboratorId,
                cancellationToken);

            return Result<MetricCheckin>.Success(checkin);
        }
        catch (DomainInvariantException ex)
        {
            return Result<MetricCheckin>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    public async Task<Result<MetricCheckin>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateMetricCheckinRequest request,
        CancellationToken cancellationToken = default)
    {
        var checkin = await checkinRepository.GetByIdAsync(id, cancellationToken);

        if (checkin is null)
        {
            return Result<MetricCheckin>.NotFound("Check-in não encontrado.");
        }

        var hasTenantAccess = await authorizationGateway.CanAccessTenantOrganizationAsync(user, checkin.OrganizationId, cancellationToken);
        if (!hasTenantAccess)
        {
            return Result<MetricCheckin>.Forbidden("Você não tem permissão para atualizar este check-in.");
        }

        if (!tenantProvider.IsGlobalAdmin && tenantProvider.CollaboratorId != checkin.CollaboratorId)
        {
            return Result<MetricCheckin>.Forbidden("Apenas o autor pode editar este check-in.");
        }

        var metric = await checkinRepository.GetMetricByIdAsync(checkin.MissionMetricId, cancellationToken);
        if (metric is null)
        {
            return Result<MetricCheckin>.NotFound("Métrica não encontrada.");
        }

        try
        {
            metric.UpdateCheckin(
                checkin,
                request.Value,
                request.Text,
                DateTime.SpecifyKind(request.CheckinDate, DateTimeKind.Utc),
                request.Note,
                request.ConfidenceLevel);

            await checkinRepository.SaveChangesAsync(cancellationToken);

            return Result<MetricCheckin>.Success(checkin);
        }
        catch (DomainInvariantException ex)
        {
            return Result<MetricCheckin>.Failure(ex.Message, ErrorType.Validation);
        }
    }

    public async Task<Result> DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var checkin = await checkinRepository.GetByIdAsync(id, cancellationToken);

        if (checkin is null)
        {
            return Result.NotFound("Check-in não encontrado.");
        }

        var hasTenantAccess = await authorizationGateway.CanAccessTenantOrganizationAsync(user, checkin.OrganizationId, cancellationToken);
        if (!hasTenantAccess)
        {
            return Result.Forbidden("Você não tem permissão para excluir este check-in.");
        }

        if (!tenantProvider.IsGlobalAdmin && tenantProvider.CollaboratorId != checkin.CollaboratorId)
        {
            return Result.Forbidden("Apenas o autor pode excluir este check-in.");
        }

        await checkinRepository.RemoveAsync(checkin, cancellationToken);
        await checkinRepository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
