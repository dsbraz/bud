using System.Security.Claims;
using Bud.Server.Application.Common;
using Bud.Server.Authorization;
using Bud.Server.Domain.Abstractions;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;
using Bud.Server.MultiTenancy;
using Bud.Shared.Contracts;

namespace Bud.Server.Application.MissionTemplates;

public sealed class CreateTemplate(
    IMissionTemplateRepository templateRepository,
    IApplicationAuthorizationGateway authorizationGateway,
    ITenantProvider tenantProvider,
    IUnitOfWork? unitOfWork = null)
{
    public async Task<Result<MissionTemplate>> ExecuteAsync(
        ClaimsPrincipal user,
        CreateMissionTemplateRequest request,
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
                return Result<MissionTemplate>.Forbidden("Você não tem permissão para criar templates nesta organização.");
            }
        }

        try
        {
            var template = MissionTemplate.Create(
                Guid.NewGuid(),
                Guid.Empty,
                request.Name,
                request.Description,
                request.MissionNamePattern,
                request.MissionDescriptionPattern);

            template.ReplaceObjectivesAndMetrics(
                request.Objectives.Select(objective => new MissionTemplateObjectiveDraft(
                    objective.Id,
                    objective.Name,
                    objective.Description,
                    objective.OrderIndex,
                    objective.Dimension)),
                request.Metrics.Select(metric => new MissionTemplateMetricDraft(
                    metric.Name,
                    metric.Type.ToDomain(),
                    metric.OrderIndex,
                    metric.MissionTemplateObjectiveId,
                    metric.QuantitativeType.ToDomain(),
                    metric.MinValue,
                    metric.MaxValue,
                    metric.Unit.ToDomain(),
                    metric.TargetText)));

            await templateRepository.AddAsync(template, cancellationToken);
            await unitOfWork.CommitAsync(templateRepository.SaveChangesAsync, cancellationToken);

            return Result<MissionTemplate>.Success(template);
        }
        catch (DomainInvariantException ex)
        {
            return Result<MissionTemplate>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}

