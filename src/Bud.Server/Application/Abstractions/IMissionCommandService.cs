using Bud.Shared.Contracts;
using Bud.Shared.Models;

namespace Bud.Server.Application.Abstractions;

public interface IMissionCommandService
{
    Task<ServiceResult<Mission>> CreateAsync(CreateMissionRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<Mission>> UpdateAsync(Guid id, UpdateMissionRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
