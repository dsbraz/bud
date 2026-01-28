namespace Bud.Shared.Models;

public enum MissionStatus
{
    Planned = 0,
    Active = 1,
    Completed = 2,
    Cancelled = 3
}

public enum MissionScopeType
{
    Organization = 0,
    Workspace = 1,
    Team = 2,
    Collaborator = 3
}

public enum MetricType
{
    Qualitative = 0,
    Quantitative = 1
}

public enum MetricUnit
{
    Integer = 0,
    Decimal = 1,
    Percentage = 2,
    Hours = 3,
    Points = 4
}
