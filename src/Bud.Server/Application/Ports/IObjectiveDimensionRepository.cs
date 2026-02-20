using Bud.Shared.Contracts;
using Bud.Shared.Domain;

namespace Bud.Server.Application.Ports;

public interface IObjectiveDimensionRepository
{
    Task<ObjectiveDimension?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ObjectiveDimension?> GetByIdTrackedAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<ObjectiveDimension>> GetAllAsync(string? search, int page, int pageSize, CancellationToken ct = default);
    Task<bool> IsNameUniqueAsync(string name, Guid? excludeId, CancellationToken ct = default);
    Task<bool> HasObjectivesAsync(Guid id, CancellationToken ct = default);
    Task<bool> HasTemplateObjectivesAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(ObjectiveDimension entity, CancellationToken ct = default);
    Task RemoveAsync(ObjectiveDimension entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
