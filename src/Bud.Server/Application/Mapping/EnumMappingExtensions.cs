using Bud.Server.Domain.Model;
using SharedContracts = Bud.Shared.Contracts;
using Bud.Shared.Contracts.Common;

namespace Bud.Server.Application.Mapping;

internal static class EnumMappingExtensions
{
    public static CollaboratorRole ToDomain(this SharedContracts.CollaboratorRole value)
        => (CollaboratorRole)(int)value;

    public static SharedContracts.CollaboratorRole ToShared(this CollaboratorRole value)
        => (SharedContracts.CollaboratorRole)(int)value;

    public static MissionStatus ToDomain(this SharedContracts.MissionStatus value)
        => (MissionStatus)(int)value;

    public static MissionStatus? ToDomain(this Optional<SharedContracts.MissionStatus> value)
        => value.HasValue ? (MissionStatus?)(int)value.Value : null;

    public static MissionScopeType ToDomain(this SharedContracts.MissionScopeType value)
        => (MissionScopeType)(int)value;

    public static MissionScopeType? ToDomain(this Optional<SharedContracts.MissionScopeType> value)
        => value.HasValue ? (MissionScopeType?)(int)value.Value : null;

    public static MetricType ToDomain(this SharedContracts.MetricType value)
        => (MetricType)(int)value;

    public static MetricType? ToDomain(this Optional<SharedContracts.MetricType> value)
        => value.HasValue ? (MetricType?)(int)value.Value : null;

    public static QuantitativeMetricType? ToDomain(this SharedContracts.QuantitativeMetricType? value)
        => value.HasValue ? (QuantitativeMetricType?)(int)value.Value : null;

    public static QuantitativeMetricType? ToDomain(this Optional<SharedContracts.QuantitativeMetricType?> value)
        => value.HasValue && value.Value.HasValue ? (QuantitativeMetricType?)(int)value.Value.Value : null;

    public static MetricUnit? ToDomain(this SharedContracts.MetricUnit? value)
        => value.HasValue ? (MetricUnit?)(int)value.Value : null;

    public static MetricUnit? ToDomain(this Optional<SharedContracts.MetricUnit?> value)
        => value.HasValue && value.Value.HasValue ? (MetricUnit?)(int)value.Value.Value : null;
}
