using Bud.Server.Data;
using Bud.Shared.Contracts;
using Bud.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Services;

public sealed class MissionTemplateService(ApplicationDbContext dbContext) : IMissionTemplateService
{
    public async Task<ServiceResult<MissionTemplate>> CreateAsync(CreateMissionTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var template = new MissionTemplate
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            MissionNamePattern = request.MissionNamePattern?.Trim(),
            MissionDescriptionPattern = request.MissionDescriptionPattern?.Trim(),
            IsDefault = false,
            IsActive = true,
            Metrics = request.Metrics.Select(m => new MissionTemplateMetric
            {
                Id = Guid.NewGuid(),
                Name = m.Name.Trim(),
                Type = m.Type,
                OrderIndex = m.OrderIndex,
                QuantitativeType = m.QuantitativeType,
                MinValue = m.MinValue,
                MaxValue = m.MaxValue,
                Unit = m.Unit,
                TargetText = m.TargetText?.Trim()
            }).ToList()
        };

        dbContext.MissionTemplates.Add(template);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<MissionTemplate>.Success(template);
    }

    public async Task<ServiceResult<MissionTemplate>> UpdateAsync(Guid id, UpdateMissionTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var template = await dbContext.MissionTemplates
            .Include(t => t.Metrics)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (template is null)
        {
            return ServiceResult<MissionTemplate>.NotFound("Template de missão não encontrado.");
        }

        template.Name = request.Name.Trim();
        template.Description = request.Description?.Trim();
        template.MissionNamePattern = request.MissionNamePattern?.Trim();
        template.MissionDescriptionPattern = request.MissionDescriptionPattern?.Trim();
        template.IsActive = request.IsActive;

        // Replace entire metrics collection
        dbContext.MissionTemplateMetrics.RemoveRange(template.Metrics);

        var newMetrics = request.Metrics.Select(m => new MissionTemplateMetric
        {
            Id = Guid.NewGuid(),
            MissionTemplateId = template.Id,
            Name = m.Name.Trim(),
            Type = m.Type,
            OrderIndex = m.OrderIndex,
            QuantitativeType = m.QuantitativeType,
            MinValue = m.MinValue,
            MaxValue = m.MaxValue,
            Unit = m.Unit,
            TargetText = m.TargetText?.Trim(),
            OrganizationId = template.OrganizationId
        }).ToList();

        dbContext.MissionTemplateMetrics.AddRange(newMetrics);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Reload to include new metrics in result
        template = await dbContext.MissionTemplates
            .Include(t => t.Metrics.OrderBy(m => m.OrderIndex))
            .FirstAsync(t => t.Id == id, cancellationToken);

        return ServiceResult<MissionTemplate>.Success(template);
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await dbContext.MissionTemplates.FindAsync([id], cancellationToken);

        if (template is null)
        {
            return ServiceResult.NotFound("Template de missão não encontrado.");
        }

        dbContext.MissionTemplates.Remove(template);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<MissionTemplate>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await dbContext.MissionTemplates
            .AsNoTracking()
            .Include(t => t.Metrics.OrderBy(m => m.OrderIndex))
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        return template is null
            ? ServiceResult<MissionTemplate>.NotFound("Template de missão não encontrado.")
            : ServiceResult<MissionTemplate>.Success(template);
    }

    public async Task<ServiceResult<PagedResult<MissionTemplate>>> GetAllAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PaginationNormalizer.Normalize(page, pageSize);

        var query = dbContext.MissionTemplates
            .AsNoTracking()
            .Include(t => t.Metrics.OrderBy(m => m.OrderIndex));

        IQueryable<MissionTemplate> filteredQuery = query;

        if (!string.IsNullOrWhiteSpace(search))
        {
            filteredQuery = dbContext.Database.IsNpgsql()
                ? filteredQuery.Where(t => EF.Functions.ILike(t.Name, $"%{search}%"))
                : filteredQuery.Where(t => t.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var total = await filteredQuery.CountAsync(cancellationToken);
        var items = await filteredQuery
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<MissionTemplate>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return ServiceResult<PagedResult<MissionTemplate>>.Success(result);
    }
}
