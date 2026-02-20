using Bud.Server.Domain.Specifications;
using Bud.Server.Infrastructure.Persistence;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Infrastructure.Repositories;

public sealed class ObjectiveDimensionRepository(ApplicationDbContext dbContext) : IObjectiveDimensionRepository
{
    public async Task<ObjectiveDimension?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await dbContext.ObjectiveDimensions.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<ObjectiveDimension?> GetByIdTrackedAsync(Guid id, CancellationToken ct = default)
        => await dbContext.ObjectiveDimensions.FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<PagedResult<ObjectiveDimension>> GetAllAsync(
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        IQueryable<ObjectiveDimension> query = dbContext.ObjectiveDimensions.AsNoTracking();

        query = new ObjectiveDimensionSearchSpecification(search, dbContext.Database.IsNpgsql()).Apply(query);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(d => d.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<ObjectiveDimension>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<bool> IsNameUniqueAsync(string name, Guid? excludeId, CancellationToken ct = default)
    {
        var query = dbContext.ObjectiveDimensions
            .AsNoTracking()
            .Where(d => excludeId == null || d.Id != excludeId.Value);

        if (dbContext.Database.IsNpgsql())
        {
            return !await query.AnyAsync(d => EF.Functions.ILike(d.Name, name), ct);
        }

        var names = await query.Select(d => d.Name).ToListAsync(ct);
        return !names.Any(existingName => string.Equals(existingName, name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> HasObjectivesAsync(Guid id, CancellationToken ct = default)
        => await dbContext.MissionObjectives.AsNoTracking().AnyAsync(o => o.ObjectiveDimensionId == id, ct);

    public async Task<bool> HasTemplateObjectivesAsync(Guid id, CancellationToken ct = default)
        => await dbContext.MissionTemplateObjectives.AsNoTracking().AnyAsync(mto => mto.ObjectiveDimensionId == id, ct);

    public async Task AddAsync(ObjectiveDimension entity, CancellationToken ct = default)
        => await dbContext.ObjectiveDimensions.AddAsync(entity, ct);

    public async Task RemoveAsync(ObjectiveDimension entity, CancellationToken ct = default)
        => await Task.FromResult(dbContext.ObjectiveDimensions.Remove(entity));

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await dbContext.SaveChangesAsync(ct);
}
