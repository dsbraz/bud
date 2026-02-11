using System.Security.Claims;
using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.MissionTemplates;

public interface IMissionTemplateCommandUseCase
{
    Task<ServiceResult<MissionTemplate>> CreateAsync(
        ClaimsPrincipal user,
        CreateMissionTemplateRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<MissionTemplate>> UpdateAsync(
        ClaimsPrincipal user,
        Guid id,
        UpdateMissionTemplateRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> DeleteAsync(
        ClaimsPrincipal user,
        Guid id,
        CancellationToken cancellationToken = default);
}
