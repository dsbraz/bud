
namespace Bud.Application.Features.Templates;

public interface ITemplateRepository
{
    Task<Template?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Template?> GetByIdWithChildrenAsync(Guid id, CancellationToken ct = default);
    Task<Template?> GetByIdReadOnlyAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<Template>> GetAllAsync(string? search, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Template entity, CancellationToken ct = default);
    Task RemoveAsync(Template entity, CancellationToken ct = default);
    Task RemoveGoalsAndIndicatorsAsync(IEnumerable<TemplateGoal> goals, IEnumerable<TemplateIndicator> indicators, CancellationToken ct = default);
    Task AddGoalsAndIndicatorsAsync(IEnumerable<TemplateGoal> goals, IEnumerable<TemplateIndicator> indicators, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
