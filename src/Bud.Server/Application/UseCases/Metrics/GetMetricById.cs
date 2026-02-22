using Bud.Server.Application.Common;
using Bud.Server.Application.Mapping;
using Bud.Server.Domain.Model;
using Bud.Server.Domain.Repositories;

namespace Bud.Server.Application.UseCases.Metrics;

public sealed class GetMetricById(IMetricRepository metricRepository)
{
    public async Task<Result<Metric>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var metric = await metricRepository.GetByIdAsync(id, cancellationToken);
        return metric is null
            ? Result<Metric>.NotFound("Métrica da missão não encontrada.")
            : Result<Metric>.Success(metric);
    }
}

