using Bud.Server.Domain.Model;
using SharedContracts = Bud.Shared.Contracts;

namespace Bud.Server.Application.Common;

internal static class EnumConversions
{
    public static CollaboratorRole ToDomain(this SharedContracts.CollaboratorRole value)
        => (CollaboratorRole)(int)value;

    public static SharedContracts.CollaboratorRole ToShared(this CollaboratorRole value)
        => (SharedContracts.CollaboratorRole)(int)value;

    public static MissionStatus ToDomain(this SharedContracts.MissionStatus value)
        => (MissionStatus)(int)value;

    public static MissionScopeType ToDomain(this SharedContracts.MissionScopeType value)
        => (MissionScopeType)(int)value;

    public static MetricType ToDomain(this SharedContracts.MetricType value)
        => (MetricType)(int)value;

    public static QuantitativeMetricType? ToDomain(this SharedContracts.QuantitativeMetricType? value)
        => value.HasValue ? (QuantitativeMetricType?)(int)value.Value : null;

    public static MetricUnit? ToDomain(this SharedContracts.MetricUnit? value)
        => value.HasValue ? (MetricUnit?)(int)value.Value : null;
}
