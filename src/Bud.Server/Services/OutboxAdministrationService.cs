using Bud.Server.Data;
using Bud.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Bud.Server.Services;

public sealed class OutboxAdministrationService(ApplicationDbContext dbContext) : IOutboxAdministrationService
{
    public async Task<ServiceResult<PagedResult<OutboxDeadLetterDto>>> GetDeadLettersAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var baseQuery = dbContext.OutboxMessages
            .AsNoTracking()
            .Where(m => m.DeadLetteredOnUtc != null)
            .OrderByDescending(m => m.DeadLetteredOnUtc)
            .ThenByDescending(m => m.OccurredOnUtc);

        var total = await baseQuery.CountAsync(cancellationToken);
        var items = await baseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new OutboxDeadLetterDto
            {
                Id = m.Id,
                OccurredOnUtc = m.OccurredOnUtc,
                EventType = m.EventType,
                RetryCount = m.RetryCount,
                DeadLetteredOnUtc = m.DeadLetteredOnUtc,
                Error = m.Error
            })
            .ToListAsync(cancellationToken);

        return ServiceResult<PagedResult<OutboxDeadLetterDto>>.Success(new PagedResult<OutboxDeadLetterDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<ServiceResult> ReprocessDeadLetterAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await dbContext.OutboxMessages
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (message is null)
        {
            return ServiceResult.NotFound("Mensagem de outbox não encontrada.");
        }

        if (message.DeadLetteredOnUtc is null)
        {
            return ServiceResult.Failure("A mensagem informada não está em dead-letter.");
        }

        message.RetryCount = 0;
        message.DeadLetteredOnUtc = null;
        message.ProcessedOnUtc = null;
        message.Error = null;
        message.NextAttemptOnUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<int>> ReprocessDeadLettersAsync(ReprocessDeadLettersRequest request, CancellationToken cancellationToken = default)
    {
        if (request.MaxItems is < 1 or > 500)
        {
            return ServiceResult<int>.Failure("O parâmetro 'maxItems' deve estar entre 1 e 500.");
        }

        if (request.DeadLetteredFromUtc.HasValue && request.DeadLetteredToUtc.HasValue &&
            request.DeadLetteredFromUtc > request.DeadLetteredToUtc)
        {
            return ServiceResult<int>.Failure("O período informado é inválido.");
        }

        var query = dbContext.OutboxMessages
            .Where(m => m.DeadLetteredOnUtc != null);

        if (!string.IsNullOrWhiteSpace(request.EventType))
        {
            var normalizedEventType = request.EventType.Trim();
            query = query.Where(m => m.EventType.Contains(normalizedEventType));
        }

        if (request.DeadLetteredFromUtc.HasValue)
        {
            query = query.Where(m => m.DeadLetteredOnUtc >= request.DeadLetteredFromUtc.Value);
        }

        if (request.DeadLetteredToUtc.HasValue)
        {
            query = query.Where(m => m.DeadLetteredOnUtc <= request.DeadLetteredToUtc.Value);
        }

        var messages = await query
            .OrderBy(m => m.DeadLetteredOnUtc)
            .Take(request.MaxItems)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            message.RetryCount = 0;
            message.DeadLetteredOnUtc = null;
            message.ProcessedOnUtc = null;
            message.Error = null;
            message.NextAttemptOnUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult<int>.Success(messages.Count);
    }
}
