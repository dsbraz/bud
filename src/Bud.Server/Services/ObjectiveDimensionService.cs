using Bud.Server.Data;
using Bud.Shared.Contracts;
using Bud.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Services;

public sealed class ObjectiveDimensionService(ApplicationDbContext dbContext) : IObjectiveDimensionService
{
    public async Task<ServiceResult<ObjectiveDimension>> CreateAsync(
        CreateObjectiveDimensionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedName = request.Name.Trim();
            var alreadyExists = await ExistsWithNameAsync(normalizedName, null, cancellationToken);
            if (alreadyExists)
            {
                return ServiceResult<ObjectiveDimension>.Failure(
                    "Já existe uma dimensão com este nome.",
                    ServiceErrorType.Conflict);
            }

            // OrganizationId será preenchido por TenantSaveChangesInterceptor quando necessário.
            var dimension = ObjectiveDimension.Create(Guid.NewGuid(), Guid.Empty, request.Name);
            dbContext.ObjectiveDimensions.Add(dimension);
            await dbContext.SaveChangesAsync(cancellationToken);

            return ServiceResult<ObjectiveDimension>.Success(dimension);
        }
        catch (DomainInvariantException ex)
        {
            return ServiceResult<ObjectiveDimension>.Failure(ex.Message, ServiceErrorType.Validation);
        }
    }

    public async Task<ServiceResult<ObjectiveDimension>> UpdateAsync(
        Guid id,
        UpdateObjectiveDimensionRequest request,
        CancellationToken cancellationToken = default)
    {
        var dimension = await dbContext.ObjectiveDimensions.FindAsync([id], cancellationToken);
        if (dimension is null)
        {
            return ServiceResult<ObjectiveDimension>.NotFound("Dimensão do objetivo não encontrada.");
        }

        try
        {
            var normalizedName = request.Name.Trim();
            var alreadyExists = await ExistsWithNameAsync(normalizedName, id, cancellationToken);
            if (alreadyExists)
            {
                return ServiceResult<ObjectiveDimension>.Failure(
                    "Já existe uma dimensão com este nome.",
                    ServiceErrorType.Conflict);
            }

            dimension.Rename(request.Name);
            await dbContext.SaveChangesAsync(cancellationToken);

            return ServiceResult<ObjectiveDimension>.Success(dimension);
        }
        catch (DomainInvariantException ex)
        {
            return ServiceResult<ObjectiveDimension>.Failure(ex.Message, ServiceErrorType.Validation);
        }
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dimension = await dbContext.ObjectiveDimensions.FindAsync([id], cancellationToken);
        if (dimension is null)
        {
            return ServiceResult.NotFound("Dimensão do objetivo não encontrada.");
        }

        var isInUse = await dbContext.MissionObjectives
            .AsNoTracking()
            .AnyAsync(o => o.ObjectiveDimensionId == id, cancellationToken);

        if (isInUse)
        {
            return ServiceResult.Failure(
                "Não é possível excluir esta dimensão porque existem objetivos vinculados a ela.",
                ServiceErrorType.Conflict);
        }

        var isUsedByTemplates = await dbContext.MissionTemplateObjectives
            .AsNoTracking()
            .AnyAsync(mto => mto.ObjectiveDimensionId == id, cancellationToken);

        if (isUsedByTemplates)
        {
            return ServiceResult.Failure(
                "Não é possível excluir esta dimensão porque existem objetivos de template vinculados a ela.",
                ServiceErrorType.Conflict);
        }

        dbContext.ObjectiveDimensions.Remove(dimension);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<ObjectiveDimension>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dimension = await dbContext.ObjectiveDimensions
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        return dimension is null
            ? ServiceResult<ObjectiveDimension>.NotFound("Dimensão do objetivo não encontrada.")
            : ServiceResult<ObjectiveDimension>.Success(dimension);
    }

    public async Task<ServiceResult<PagedResult<ObjectiveDimension>>> GetAllAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        IQueryable<ObjectiveDimension> query = dbContext.ObjectiveDimensions.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = dbContext.Database.IsNpgsql()
                ? query.Where(d => EF.Functions.ILike(d.Name, $"%{search}%"))
                : query.Where(d => d.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(d => d.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return ServiceResult<PagedResult<ObjectiveDimension>>.Success(new PagedResult<ObjectiveDimension>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        });
    }

    private async Task<bool> ExistsWithNameAsync(string name, Guid? excludingId, CancellationToken cancellationToken)
    {
        var query = dbContext.ObjectiveDimensions
            .AsNoTracking()
            .Where(d => excludingId == null || d.Id != excludingId.Value);

        if (dbContext.Database.IsNpgsql())
        {
            return await query.AnyAsync(d => EF.Functions.ILike(d.Name, name), cancellationToken);
        }

        var names = await query.Select(d => d.Name).ToListAsync(cancellationToken);
        return names.Any(existingName => string.Equals(existingName, name, StringComparison.OrdinalIgnoreCase));
    }
}
