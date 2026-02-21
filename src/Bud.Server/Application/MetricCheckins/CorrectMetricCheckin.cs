using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.MetricCheckins;

public sealed class CorrectMetricCheckin(
    IMetricCheckinRepository checkinRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ITenantProvider tenantProvider,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<MetricCheckin>> ExecuteAsync(
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

            await unitOfWork.CommitAsync(checkinRepository.SaveChangesAsync, cancellationToken);

            return Result<MetricCheckin>.Success(checkin);
        }
        catch (DomainInvariantException ex)
        {
            return Result<MetricCheckin>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}
